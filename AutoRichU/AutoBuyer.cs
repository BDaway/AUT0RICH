using System;
using System.Drawing; // 需要引用 System.Drawing.Common NuGet 包（.NET Core/5+）或添加项目引用
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging; // For ImageFormat

/// <summary>
/// 实现自动购买的核心逻辑。
/// </summary>
public class AutoBuyer : IDisposable // Implement IDisposable
{
    private readonly AppSettings settings; // 引用应用程序设置
    private volatile bool running = false; // 控制购买循环的运行状态 (volatile 确保线程可见性)
    private CancellationTokenSource cts;   // 用于取消后台任务
    private Task buyTask;                  // 后台购买任务的引用
    private int successfulPurchases = 0;   // 成功购买次数计数
    private readonly Random random = new Random(); // Use readonly
    private volatile bool _stopping = false; // Add a flag to prevent race conditions/multiple stops

    // 共享的 HttpClient 实例，提高性能并减少资源占用
    // ConfigureAwait(false) can be important if this runs in a context with a SynchronizationContext (like WinForms old style async)
    // but with Task.Run it's less critical. Still good practice.
    private static readonly HttpClient httpClient = new HttpClient();

    /// <summary>
    /// 获取一个值，该值指示自动购买任务当前是否正在运行。
    /// </summary>
    public bool IsRunning => running && buyTask != null && !buyTask.IsCompleted;

    /// <summary>
    /// 当自动购买过程停止时（无论是手动、达到次数还是出错）触发。
    /// </summary>
    public event Action Stopped;

    /// <summary>
    /// 初始化 AutoBuyer。
    /// </summary>
    /// <param name="appSettings">应用程序的配置设置。</param>
    public AutoBuyer(AppSettings appSettings)
    {
        this.settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        httpClient.Timeout = TimeSpan.FromSeconds(settings.OcrTimeoutSeconds);
        KeyboardHook.StopAutoBuyer += HandleStopRequest;
    }

    /// <summary>
    /// 启动自动购买流程。
    /// </summary>
    /// <exception cref="InvalidOperationException">如果罗技驱动未初始化。</exception>
    public void Start()
    {
        if (IsRunning)
        {
            Console.WriteLine("自动购买已经在运行中。");
            return;
        }
        if (!LogitechMouse.IsInitialized)
        {
            Console.WriteLine("错误：罗技驱动未初始化，无法启动购买。");
            throw new InvalidOperationException("罗技驱动未初始化，无法启动。");
        }

        _stopping = false; // 重置 stopping 标志

        Console.WriteLine("开始启动自动购买任务...");
        successfulPurchases = 0;
        running = true;
        cts = new CancellationTokenSource();
        // Pass the token to the loop correctly
        CancellationToken token = cts.Token;
        buyTask = Task.Run(async () => await BuyLoop(token), token); // 使用 async lambda
        Console.WriteLine("自动购买任务已启动。");
    }

    /// <summary>
    /// 请求停止自动购买流程。
    /// </summary>
    public void Stop()
    {
        // Check running first, but allow stopping even if already _stopping to ensure event fires?
        // Let OnStopped handle the logic
        if (!running && !_stopping) // If not running and not already stopping, do nothing.
        {
            Console.WriteLine("Stop 请求，但 AutoBuyer 未运行且未在停止中。");
            return;
        }
        Console.WriteLine("Stop: 外部请求停止...");
        OnStopped(); // Trigger the unified stop logic
    }

    // Helper to trigger Stopped event and perform cleanup (idempotent)
    private void OnStopped()
    {
        // Use lock or Interlocked for thread safety if needed, but simple flag might suffice here
        if (_stopping) return; // Already processing stop
        _stopping = true; // Signal that stop processing has started

        Console.WriteLine("OnStopped: 开始执行停止流程...");

        running = false; // Ensure running state is false

        var currentCts = cts; // Use local variable
        if (currentCts != null)
        {
            try
            {
                if (!currentCts.IsCancellationRequested)
                {
                    Console.WriteLine("OnStopped: 请求取消 CancellationTokenSource...");
                    currentCts.Cancel();
                }
                Console.WriteLine("OnStopped: 释放 CancellationTokenSource...");
                currentCts.Dispose();
            }
            catch (ObjectDisposedException) { Console.WriteLine("OnStopped: CancellationTokenSource 已被释放。"); }
            catch (Exception ex) { Console.WriteLine($"OnStopped: 清理 CancellationTokenSource 时出错: {ex.Message}"); }
            finally { cts = null; } // Mark as disposed/nullified
        }
        else { Console.WriteLine("OnStopped: CancellationTokenSource 为 null 或已被释放。"); }

        buyTask = null; // Clear task reference

        try
        {
            Console.WriteLine("OnStopped: 触发 Stopped 事件...");
            Stopped?.Invoke(); // Notify listeners (like MainForm)
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: 调用 Stopped 事件处理程序时出错: {ex.Message}");
        }
        finally
        {
            // _stopping = false; // Don't reset here, reset in Start()
            Console.WriteLine("OnStopped: 停止流程完成。");
        }
    }

    // Handles stop request from KeyboardHook
    private void HandleStopRequest()
    {
        if (running && !_stopping) // Only process if running and not already stopping
        {
            Console.WriteLine("收到来自键盘钩子的停止请求。");
            Stop(); // Use the standard stop method
        }
    }

    // --- Methods to update settings dynamically ---
    public void SetPriceThreshold(int newThreshold)
    {
        settings.PriceThreshold = Math.Max(0, newThreshold); // Ensure non-negative
        Console.WriteLine($"最高单价已更新为: {settings.PriceThreshold}");
    }

    public void SetPurchaseQuantity(int quantity)
    {
        // Validate quantity if needed, e.g., only allow 1, 25, 100, 200
        settings.PurchaseQuantity = quantity;
        Console.WriteLine($"购买数量已更新为: {settings.PurchaseQuantity}");
    }

    public void SetStopAfterPurchases(int count)
    {
        settings.StopAfterPurchases = Math.Max(0, count); // Ensure non-negative
        Console.WriteLine($"停止购买次数已更新为: {settings.StopAfterPurchases} (0 表示无限)");
    }


    /// <summary>
    /// 核心购买循环，在后台线程运行。
    /// </summary>
    private async Task BuyLoop(CancellationToken token)
    {
        Console.WriteLine("购买循环线程启动...");
        try
        {
            while (running && !token.IsCancellationRequested)
            {
                bool purchasedOrFailedThisRound = false;
                bool errorOccurred = false;

                try
                {
                    token.ThrowIfCancellationRequested();
                    EnterItemPage();
                    await Task.Delay(random.Next(settings.DelayShortMin, settings.DelayShortMax), token);

                    token.ThrowIfCancellationRequested();
                    int price = await CheckPrice(token);
                    token.ThrowIfCancellationRequested();

                    if (price == -1)
                    {
                        Console.WriteLine("未能识别初始价格，将刷新页面重试...");
                        errorOccurred = true;
                    }
                    else if (price <= settings.PriceThreshold)
                    {
                        Console.WriteLine($"检测到低单价 {price} <= {settings.PriceThreshold}，点击购买数量 {settings.PurchaseQuantity}...");
                        ClickQuantityButton();
                        await Task.Delay(random.Next(settings.DelayClickMin, settings.DelayClickMax), token);
                        token.ThrowIfCancellationRequested();

                        int averagePrice = await CheckPrice(token);
                        token.ThrowIfCancellationRequested();

                        if (averagePrice == -1)
                        {
                            Console.WriteLine("点击数量后未能识别平均价格，将刷新页面重试...");
                            errorOccurred = true;
                        }
                        else if (averagePrice <= settings.PriceThreshold)
                        {
                            Console.WriteLine($"点击数量后，平均价格 {averagePrice} <= {settings.PriceThreshold}，执行购买...");
                            ClickAt(settings.BuyButtonPos.X, settings.BuyButtonPos.Y, false);
                            await Task.Delay(random.Next(settings.DelayLongMin, settings.DelayLongMax), token);
                            token.ThrowIfCancellationRequested();

                            bool success = await CheckPurchaseNotification(token);
                            purchasedOrFailedThisRound = true;

                            if (success)
                            {
                                successfulPurchases++;
                                Console.WriteLine($"购买成功! (当前成功 {successfulPurchases} 次 / 目标 {(settings.StopAfterPurchases == 0 ? "无限" : settings.StopAfterPurchases.ToString())})");
                                if (settings.StopAfterPurchases > 0 && successfulPurchases >= settings.StopAfterPurchases)
                                {
                                    Console.WriteLine($"已达到目标购买次数 {settings.StopAfterPurchases}，停止自动购买...");
                                    Stop(); // Request stop through the standard mechanism
                                    break; // Exit the loop cleanly after requesting stop
                                }
                            }
                            else
                            {
                                Console.WriteLine("购买失败或未识别到成功提示，将刷新...");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"点击数量后，平均价格 {averagePrice} > {settings.PriceThreshold}，价格不合适，刷新...");
                            purchasedOrFailedThisRound = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"初始单价 {price} > {settings.PriceThreshold}，价格不合适，刷新...");
                        purchasedOrFailedThisRound = true;
                    }
                }
                catch (OperationCanceledException) { throw; } // Re-throw cancellation
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"错误: OCR API 请求失败: {httpEx.Message}"); errorOccurred = true;
                    await Task.Delay(random.Next(settings.DelayMediumMin, settings.DelayMediumMax), CancellationToken.None); // Wait before retry
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"错误: OCR API 响应解析失败: {jsonEx.Message}"); errorOccurred = true;
                    await Task.Delay(random.Next(settings.DelayMediumMin, settings.DelayMediumMax), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: 购买循环中发生意外错误: {ex.Message}\n{ex.StackTrace}");
                    errorOccurred = true;
                    await Task.Delay(random.Next(settings.DelayLongMin, settings.DelayLongMax), CancellationToken.None);
                }

                // --- Refresh Logic ---
                if (running && !token.IsCancellationRequested && (purchasedOrFailedThisRound || errorOccurred))
                {
                    RefreshPrice();
                    await Task.Delay(random.Next(settings.DelayMediumMin, settings.DelayMediumMax), token);
                }
                else if (running && !token.IsCancellationRequested)
                {
                    // If nothing happened (e.g., error during first price check before refresh decision)
                    // Add a small delay to prevent tight loop on unexpected state
                    await Task.Delay(random.Next(settings.DelayShortMin, settings.DelayShortMax), token);
                }
            } // End While

            // Check if loop exited due to cancellation request after the loop condition check
            token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("购买循环被取消。");
            // OnStopped() will be called in finally
        }
        catch (Exception ex) // Catch unexpected exceptions from the loop itself
        {
            Console.WriteLine($"严重错误: BuyLoop 意外终止: {ex.Message}\n{ex.StackTrace}");
            // OnStopped() will be called in finally
        }
        finally
        {
            Console.WriteLine("BuyLoop 任务结束 (finally 块)。");
            // Ensure OnStopped is called if the loop terminates for any reason
            OnStopped();
        }
    }

    // --- Helper Methods ---

    private void ClickQuantityButton()
    {
        Point pos;
        switch (settings.PurchaseQuantity)
        {
            case 1: pos = settings.MinQtyButtonPos; break;
            case 25: pos = settings.SmallQtyButtonPos; break;
            case 100: pos = settings.MediumQtyButtonPos; break;
            case 200:
            default: pos = settings.MaxQtyButtonPos; break;
        }
        ClickAt(pos.X, pos.Y, true);
    }

    private void EnterItemPage()
    {
        Console.WriteLine("动作: 进入商品页面...");
        ClickAt(settings.ItemClickPos.X, settings.ItemClickPos.Y, false);
    }

    private void RefreshPrice()
    {
        Console.WriteLine("动作: 返回并重新进入以刷新价格...");
        ClickAt(settings.BackButtonPos.X, settings.BackButtonPos.Y, false);
        Thread.Sleep(random.Next(settings.DelayShortMin, settings.DelayShortMax));
    }

    private void ClickAt(int x, int y, bool preciseEnd)
    {
        if (!running || (cts?.IsCancellationRequested ?? true)) return;
        LogitechMouse.MoveTo(x, y, preciseEnd);
        Thread.Sleep(random.Next(settings.DelayClickMin, settings.DelayClickMax));

        if (!running || (cts?.IsCancellationRequested ?? true)) return;
        LogitechMouse.Click(1);
        Thread.Sleep(random.Next(settings.DelayClickMin, settings.DelayClickMax));
    }

    private async Task<int> CheckPrice(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 0; attempt < settings.OcrRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string text1 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token).ConfigureAwait(false);
            lastReadText = text1;
            token.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(text1) && int.TryParse(text1.Trim(), out int price1))
            {
                await Task.Delay(settings.DelayOcrCheck, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                string text2 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token).ConfigureAwait(false);
                lastReadText = text2;
                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(text2) && int.TryParse(text2.Trim(), out int price2))
                {
                    if (price1 == price2)
                    {
                        Console.WriteLine($"价格确认: {price1}");
                        return price1;
                    }
                    else { Console.WriteLine($"警告: 价格 OCR 两次读数不一致 ({price1} vs {price2})，重试..."); }
                }
            }
            await Task.Delay(random.Next(settings.DelayRetryWaitMin, settings.DelayRetryWaitMax), token).ConfigureAwait(false);
        }
        Console.WriteLine($"错误: 检查价格在 {settings.OcrRetryAttempts} 次尝试后失败。最后识别文本: '{lastReadText}'");
        return -1;
    }

    private async Task<bool> CheckPurchaseNotification(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 0; attempt < settings.NotificationRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string text = await RecognizeScreenArea(settings.NotificationPos, settings.NotificationSize, false, token).ConfigureAwait(false);
            lastReadText = text?.Trim() ?? "";
            token.ThrowIfCancellationRequested();

            // Use IndexOf with OrdinalIgnoreCase for robust, case-insensitive check
            foreach (string keyword in settings.SuccessKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine($"识别到购买成功关键字: '{keyword}'");
                    return true;
                }
            }
            foreach (string keyword in settings.FailureKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine($"识别到购买失败关键字: '{keyword}'");
                    return false;
                }
            }
            await Task.Delay(random.Next(settings.DelayRetryWaitMin, settings.DelayRetryWaitMax), token).ConfigureAwait(false);
        }
        Console.WriteLine($"警告: 检查购买通知在 {settings.NotificationRetryAttempts} 次尝试后未识别到明确结果。最后识别文本: '{lastReadText}'。按购买失败处理。");
        return false;
    }

    private async Task<string> RecognizeScreenArea(Point pos, Size size, bool numbersOnly, CancellationToken token)
    {
        byte[] imageBytes = null;
        try
        {
            using (Bitmap bmp = CaptureScreen(pos.X, pos.Y, size.Width, size.Height))
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                imageBytes = ms.ToArray();
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                Console.WriteLine("错误: 截图后未能获取图像数据。"); return "";
            }
        }
        catch (Exception ex) { Console.WriteLine($"错误: 截图或图像转换失败 ({pos.X},{pos.Y}): {ex.Message}"); return ""; }

        try
        {
            using (var content = new MultipartFormDataContent())
            using (var imageContent = new ByteArrayContent(imageBytes))
            {
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                content.Add(imageContent, "image", "screenshot.png");
                content.Add(new StringContent(numbersOnly.ToString().ToLower()), "numbersOnly");

                // Use ConfigureAwait(false) to avoid capturing context in library-like code
                HttpResponseMessage response = await httpClient.PostAsync(settings.OcrApiUrl, content, token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"错误: OCR API 请求失败。状态码: {response.StatusCode}, URL: {settings.OcrApiUrl}, 响应: {errorResponse}");
                    return "";
                }

                string jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("text", out JsonElement textElement))
                    {
                        return textElement.GetString()?.Trim() ?? "";
                    }
                    else if (root.TryGetProperty("error", out JsonElement errorElement))
                    {
                        Console.WriteLine($"错误: OCR API 返回错误信息: {errorElement.GetString()}"); return "";
                    }
                    else { Console.WriteLine($"错误: OCR API 响应格式未知: {jsonResponse}"); return ""; }
                }
            }
        }
        catch (HttpRequestException httpEx) { Console.WriteLine($"错误: OCR API 网络请求失败: {httpEx.Message}。URL: {settings.OcrApiUrl}"); throw; }
        catch (TaskCanceledException) // Don't need the variable name if unused
        {
            if (token.IsCancellationRequested) { Console.WriteLine("OCR 请求被用户取消。"); throw new OperationCanceledException(token); }
            else { Console.WriteLine($"错误: OCR API 请求超时 ({settings.OcrTimeoutSeconds} 秒)。URL: {settings.OcrApiUrl}。"); throw; }
        }
        catch (JsonException jsonEx) { Console.WriteLine($"错误: 解析 OCR API 响应失败: {jsonEx.Message}。"); throw; }
        catch (Exception ex) { Console.WriteLine($"错误: 调用 OCR API 时发生未知错误: {ex.Message}"); throw; }
    }

    private Bitmap CaptureScreen(int x, int y, int width, int height)
    {
        width = Math.Max(1, width); height = Math.Max(1, height);
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: 截图失败 ({x},{y} - {width}x{height}): {ex.Message}");
            // Dispose potentially corrupted bitmap and return a small blank one
            bmp?.Dispose();
            return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        }
        return bmp;
    }

    // --- IDisposable Implementation ---
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                Console.WriteLine("Disposing AutoBuyer managed resources...");
                KeyboardHook.StopAutoBuyer -= HandleStopRequest; // Unsubscribe event

                // Ensure stop logic is called during disposal if still running/partially stopped
                OnStopped();

                // HttpClient is static, don't dispose here
            }
            // Free unmanaged resources (unmanaged objects) and override finalizer
            // (None in this class directly, but good practice to include the pattern)
            disposedValue = true;
        }
    }

    // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~AutoBuyer()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
