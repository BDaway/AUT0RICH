// AutoBuyer.cs
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Windows.Forms; // 确保 using

public class AutoBuyer : IDisposable
{
    private readonly AppSettings settings;
    private volatile bool running = false; // 是否期望运行
    private CancellationTokenSource cts;
    private Task buyTask;
    private int successfulPurchases = 0;
    private readonly Random random = new Random();
    private volatile bool _stopping = false; // 是否正在执行停止流程
    private static readonly HttpClient httpClient = new HttpClient(); // 静态HttpClient以重用

    public bool IsRunning => running && buyTask != null && !buyTask.IsCompleted && !_stopping;
    public event Action Stopped;

    public AutoBuyer(AppSettings appSettings)
    {
        this.settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        httpClient.Timeout = TimeSpan.FromSeconds(settings.OcrTimeoutSeconds);
        KeyboardHook.StopAutoBuyer += HandleStopRequest;
        Console.WriteLine("AutoBuyer 初始化并订阅了 KeyboardHook。");
    }

    public void Start()
    {
        if (IsRunning) { Console.WriteLine("自动购买已经在运行中。"); return; }
        if (_stopping) { Console.WriteLine("自动购买正在停止中，无法启动。"); return; }

        Console.WriteLine("尝试启动自动购买任务...");
        lock (this)
        {
            if (IsRunning) { Console.WriteLine("自动购买已被其他线程并发启动，取消本次启动请求。"); return; }
            _stopping = false;
            successfulPurchases = 0;
            running = true;
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            buyTask = Task.Run(async () => await BuyLoop(token), token);
            Console.WriteLine("自动购买任务已成功启动。");
        }
    }

    public void Stop()
    {
        if (!running && !_stopping) { Console.WriteLine("Stop 请求，但 AutoBuyer 未运行且未在停止中。"); return; }
        if (_stopping) { Console.WriteLine("停止操作已在进行中。"); return; }
        Console.WriteLine("Stop: 收到外部停止请求。");
        _stopping = true;
        CancelAndCleanup();
    }

    private void CancelAndCleanup()
    {
        Console.WriteLine("CancelAndCleanup: 开始执行取消和清理...");
        running = false;
        var currentCts = cts;
        if (currentCts != null)
        {
            try
            {
                if (!currentCts.IsCancellationRequested)
                {
                    Console.WriteLine("CancelAndCleanup: 请求取消 CancellationTokenSource...");
                    currentCts.Cancel();
                }
                else { Console.WriteLine("CancelAndCleanup: 取消已被请求。"); }
            }
            catch (ObjectDisposedException) { Console.WriteLine("CancelAndCleanup: CancellationTokenSource 在尝试取消时已被释放。"); }
            catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: 取消 CancellationTokenSource 时出错: {ex.Message}"); }
            finally
            {
                try
                {
                    if (currentCts != null)
                    {
                        Console.WriteLine("CancelAndCleanup: 尝试释放 CancellationTokenSource...");
                        currentCts.Dispose();
                    }
                }
                catch (ObjectDisposedException) { Console.WriteLine("CancelAndCleanup: CTS 已经释放 (捕获到 ObjectDisposedException)。"); }
                catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: 释放 CancellationTokenSource 时出错: {ex.Message}"); }
                finally
                {
                    if (ReferenceEquals(cts, currentCts)) { cts = null; }
                }
            }
        }
        else { Console.WriteLine("CancelAndCleanup: CancellationTokenSource 已为 null。"); }

        var currentTask = buyTask;
        if (currentTask != null && !currentTask.IsCompleted)
        {
            Console.WriteLine("CancelAndCleanup: 等待 BuyLoop 任务完成...");
            try
            {
                bool completed = currentTask.Wait(TimeSpan.FromSeconds(2));
                if (!completed) { Console.WriteLine("CancelAndCleanup: 警告 - BuyLoop 任务未在超时时间内完成。"); }
                else { Console.WriteLine("CancelAndCleanup: BuyLoop 任务已完成。"); }
            }
            catch (AggregateException ae) { ae.Handle(ex => ex is OperationCanceledException); Console.WriteLine("CancelAndCleanup: BuyLoop 任务按预期取消。"); }
            catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: 等待 BuyLoop 任务时出错: {ex.Message}"); }
        }
        buyTask = null;

        try
        {
            Console.WriteLine("CancelAndCleanup: 调用 Stopped 事件...");
            Stopped?.Invoke();
        }
        catch (Exception ex) { Console.WriteLine($"错误: 调用 Stopped 事件时出错: {ex.Message}"); }
        finally
        {
            _stopping = false;
            Console.WriteLine("CancelAndCleanup: 停止流程完成。");
        }
    }

    private void HandleStopRequest()
    {
        if (running && !_stopping)
        {
            Console.WriteLine("AutoBuyer 收到来自 KeyboardHook 的停止请求。");
            Stop();
        }
        else { Console.WriteLine("收到 KeyboardHook 停止请求，但 AutoBuyer 当前无法停止（未运行或已在停止中）。"); }
    }

    public void SetPriceThreshold(int newThreshold) { settings.PriceThreshold = Math.Max(0, newThreshold); Console.WriteLine($"最高单价更新为: {settings.PriceThreshold}"); }
    public void SetPurchaseQuantity(int quantity) { settings.PurchaseQuantity = quantity; Console.WriteLine($"购买数量更新为: {settings.PurchaseQuantity}"); }
    public void SetStopAfterPurchases(int count) { settings.StopAfterPurchases = Math.Max(0, count); Console.WriteLine($"停止购买次数更新为: {settings.StopAfterPurchases} (0=无限)"); }

    private async Task BuyLoop(CancellationToken token)
    {
        Console.WriteLine("购买循环线程启动...");
        int consecutivePriceCheckFailures = 0; // <--- 添加计数器
        try
        {
            while (running && !token.IsCancellationRequested)
            {
                bool purchasedOrFailedThisRound = false;
                bool errorOccurred = false;
                try
                {
                    token.ThrowIfCancellationRequested();

                    Console.WriteLine("BuyLoop: 进入商品页面...");
                    EnterItemPage();
                    await DelayBeforeCheck(token);
                    await DelayAction(token);
                    token.ThrowIfCancellationRequested();
                    Console.WriteLine("BuyLoop: 检查初始价格...");
                    int price = await CheckPrice(token);

                    if (price == -1)
                    {
                        consecutivePriceCheckFailures++; // <--- 失败时递增
                        Console.WriteLine($"BuyLoop: 未能识别初始价格 (连续失败: {consecutivePriceCheckFailures})，检查是否需要刷新...");

                        if (consecutivePriceCheckFailures >= 5) // <--- 检查阈值
                        {
                            Console.WriteLine($"BuyLoop: 连续 {consecutivePriceCheckFailures} 次价格识别失败，执行刷新操作...");
                            RefreshPrice(); // 执行刷新
                            await DelayAction(token); // 刷新后延迟
                            consecutivePriceCheckFailures = 0; // <--- 刷新后重置计数器
                            continue; // <--- 跳到下一次循环
                        }
                        else
                        {
                            // 失败次数未达阈值，直接进入下一次循环
                            Console.WriteLine("BuyLoop: 连续失败次数未达阈值，开始下一轮循环...");
                            continue;
                        }
                    }
                    else // 价格检查成功
                    {
                        consecutivePriceCheckFailures = 0; // <--- 成功时重置计数器
                        // ... (继续原有逻辑) ...
                        if (price <= settings.PriceThreshold)
                        {
                            Console.WriteLine($"BuyLoop: 检测到低单价 {price} <= {settings.PriceThreshold}，点击购买数量 {settings.PurchaseQuantity}...");
                            ClickQuantityButton();
                            await DelayBeforeCheckR(token);
                            await DelayAction(token);
                            token.ThrowIfCancellationRequested();
                            Console.WriteLine("BuyLoop: 点击数量后检查平均价格...");
                            int averagePrice = await CheckPrice(token);

                            if (averagePrice == -1)
                            {
                                consecutivePriceCheckFailures++; // <--- 失败时递增
                                Console.WriteLine($"BuyLoop: 点击数量后未能识别平均价格 (连续失败: {consecutivePriceCheckFailures})，检查是否需要刷新...");

                                if (consecutivePriceCheckFailures >= 5) // <--- 检查阈值
                                {
                                    Console.WriteLine($"BuyLoop: 连续 {consecutivePriceCheckFailures} 次价格识别失败，执行刷新操作...");
                                    RefreshPrice(); // 执行刷新
                                    await DelayAction(token); // 刷新后延迟
                                    consecutivePriceCheckFailures = 0; // <--- 刷新后重置计数器
                                    continue; // <--- 跳到下一次循环
                                }
                                else
                                {
                                    // 失败次数未达阈值，直接进入下一次循环
                                    Console.WriteLine("BuyLoop: 连续失败次数未达阈值，开始下一轮循环...");
                                    continue;
                                }
                            }
                            else // 平均价格检查成功
                            {
                                consecutivePriceCheckFailures = 0; // <--- 成功时重置计数器
                                // ... (继续原有逻辑) ...
                                if (averagePrice <= settings.PriceThreshold)
                                {
                                    Console.WriteLine($"BuyLoop: 平均价格确认 {averagePrice} <= {settings.PriceThreshold}，执行购买...");
                                    ClickAt(settings.BuyButtonPos.X, settings.BuyButtonPos.Y, false);
                                    await Task.Delay(random.Next(settings.NotificationDelayMin, settings.NotificationDelayMax + 1), token);
                                    token.ThrowIfCancellationRequested();
                                    Console.WriteLine("BuyLoop: 检查购买通知...");
                                    bool success = await CheckPurchaseNotification(token);
                                    purchasedOrFailedThisRound = true;

                                    if (success)
                                    {
                                        successfulPurchases++;
                                        Console.WriteLine($"BuyLoop: 购买成功! (当前成功 {successfulPurchases}/{settings.StopAfterPurchases})");
                                        if (settings.StopAfterPurchases > 0 && successfulPurchases >= settings.StopAfterPurchases)
                                        {
                                            Console.WriteLine($"BuyLoop: 达到目标购买次数 {settings.StopAfterPurchases}，停止...");
                                            running = false;
                                            break;
                                        }
                                    }
                                    else { Console.WriteLine("BuyLoop: 购买失败或未识别成功提示，将刷新..."); }
                                }
                                else { Console.WriteLine($"BuyLoop: 平均价格 {averagePrice} > {settings.PriceThreshold}，刷新..."); purchasedOrFailedThisRound = true; }
                            }
                        }
                        else { Console.WriteLine($"BuyLoop: 初始单价 {price} > {settings.PriceThreshold}，刷新..."); purchasedOrFailedThisRound = true; }
                    }
                }
                catch (OperationCanceledException) { Console.WriteLine("BuyLoop: 操作被取消。"); throw; }
                catch (HttpRequestException httpEx) { Console.WriteLine($"BuyLoop 错误: OCR API 请求失败: {httpEx.Message}"); errorOccurred = true; }
                catch (JsonException jsonEx) { Console.WriteLine($"BuyLoop 错误: OCR 响应解析失败: {jsonEx.Message}"); errorOccurred = true; }
                catch (Exception ex) { Console.WriteLine($"BuyLoop 错误: 发生意外错误: {ex.Message}\n{ex.StackTrace}"); errorOccurred = true; }

                // 刷新逻辑: 仅在购买尝试后或发生非价格识别错误时触发
                // 如果因为连续价格失败触发了刷新，上面的 continue 已经跳过了这里
                if (running && !token.IsCancellationRequested && (purchasedOrFailedThisRound || errorOccurred))
                {
                    Console.WriteLine("BuyLoop: 因购买尝试或非价格检查错误，刷新价格..."); // 明确日志
                    RefreshPrice();
                    await DelayAction(token);
                    consecutivePriceCheckFailures = 0; // <--- 在其他原因的刷新后也重置计数器
                }
                else if (!running || token.IsCancellationRequested)
                {
                    Console.WriteLine("BuyLoop: 因 running=false 或取消请求而退出循环。");
                    break;
                }
            }
            token.ThrowIfCancellationRequested();
            Console.WriteLine("BuyLoop: 循环正常结束（或达到购买限制）。");
        }
        catch (OperationCanceledException) { Console.WriteLine("BuyLoop: 任务被取消。"); }
        catch (Exception ex) { Console.WriteLine($"BuyLoop: 任务意外终止: {ex.Message}\n{ex.StackTrace}"); }
        finally
        {
            Console.WriteLine("BuyLoop: 退出 BuyLoop 任务。通过 CancelAndCleanup 启动清理...");
            if (!_stopping) { _stopping = true; CancelAndCleanup(); }
        }
    }


    // ... (ClickQuantityButton, EnterItemPage, RefreshPrice, ClickAt, CheckPrice, CheckPurchaseNotification, RecognizeScreenArea, CaptureScreen, 延迟方法, Dispose 等保持不变) ...
    private void ClickQuantityButton()
    {
        Point pos;
        switch (settings.PurchaseQuantity)
        {
            case 1: pos = settings.MinQtyButtonPos; break;
            case 25: pos = settings.SmallQtyButtonPos; break;
            case 100: pos = settings.MediumQtyButtonPos; break;
            default: pos = settings.MaxQtyButtonPos; break;
        }
        Console.WriteLine($"动作: 点击数量按钮 ({settings.PurchaseQuantity}) 于 {pos}");
        ClickAt(pos.X, pos.Y, true);
    }

    private void EnterItemPage() { Console.WriteLine($"动作: 点击商品入口点于 {settings.ItemClickPos}"); ClickAt(settings.ItemClickPos.X, settings.ItemClickPos.Y, false); }

    private void RefreshPrice()
    {
        Console.WriteLine("动作: 按下 ESC 键刷新价格...");
        if (!running || cts?.IsCancellationRequested == true)
        { Console.WriteLine("RefreshPrice: 因未运行或请求取消而跳过按键。"); return; }
        KeyboardSimulator.SendKey(Keys.Escape);
    }

    private void ClickAt(int x, int y, bool preciseEnd)
    {
        if (!running || (cts?.IsCancellationRequested ?? true))
        { Console.WriteLine($"ClickAt ({x},{y}): 因未运行或请求取消而跳过。"); return; }
        MouseSimulator.MoveTo(x, y, preciseEnd);
        if (!running || (cts?.IsCancellationRequested ?? true))
        { Console.WriteLine($"ClickAt ({x},{y}): 在移动后、点击前检测到取消，跳过点击。"); return; }
        MouseSimulator.Click();
    }


    private async Task<int> CheckPrice(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 1; attempt <= settings.OcrRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine($"价格检查: 尝试 {attempt}/{settings.OcrRetryAttempts}");
            string text1 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token);
            lastReadText = text1;

            if (!string.IsNullOrWhiteSpace(text1) && int.TryParse(text1.Trim(), out int price1))
            {
                Console.WriteLine($"价格检查: 读取到价格 {price1}。二次读取以确认...");
                await DelayOcrCheck(token);
                token.ThrowIfCancellationRequested();
                string text2 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token);
                lastReadText = text2;

                if (!string.IsNullOrWhiteSpace(text2) && int.TryParse(text2.Trim(), out int price2))
                {
                    if (price1 == price2) { Console.WriteLine($"价格检查: 确认价格: {price1}"); return price1; }
                    else { Console.WriteLine($"价格检查警告: 两次价格不一致 ({price1} vs {price2})。重试..."); }
                }
                else { Console.WriteLine($"价格检查警告: 二次读取失败或无效 ('{text2}')。重试..."); }
            }
            else { Console.WriteLine($"价格检查警告: 首次读取失败或无效 ('{text1}')。重试..."); }

            if (attempt < settings.OcrRetryAttempts) { await DelayOcrRetry(token); }
        }
        Console.WriteLine($"价格检查错误: {settings.OcrRetryAttempts} 次尝试后失败。最后读取文本: '{lastReadText}'");
        return -1;
    }

    private async Task<bool> CheckPurchaseNotification(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 1; attempt <= settings.NotificationRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine($"通知检查: 尝试 {attempt}/{settings.NotificationRetryAttempts}");
            string text = await RecognizeScreenArea(settings.NotificationPos, settings.NotificationSize, false, token);
            lastReadText = text?.Trim() ?? "";

            foreach (string keyword in settings.SuccessKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                { Console.WriteLine($"通知检查: 在 '{lastReadText}' 中找到成功关键字 '{keyword}'。"); return true; }
            }
            foreach (string keyword in settings.FailureKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                { Console.WriteLine($"通知检查: 在 '{lastReadText}' 中找到失败关键字 '{keyword}'。"); return false; }
            }
            Console.WriteLine($"通知检查: 在 '{lastReadText}' 中未找到明确关键字。重试...");
            if (attempt < settings.NotificationRetryAttempts) { await DelayOcrRetry(token); }
        }
        Console.WriteLine($"通知检查警告: {settings.NotificationRetryAttempts} 次尝试后未能识别结果。最后读取文本: '{lastReadText}'");
        return false;
    }

    private async Task<string> RecognizeScreenArea(Point pos, Size size, bool numbersOnly, CancellationToken token)
    {
        byte[] imageBytes;
        try
        {
            using (Bitmap bmp = CaptureScreen(pos.X, pos.Y, size.Width, size.Height))
            using (MemoryStream ms = new MemoryStream())
            { bmp.Save(ms, ImageFormat.Png); imageBytes = ms.ToArray(); }
            if (imageBytes == null || imageBytes.Length == 0) { Console.WriteLine($"OCR 错误: 区域 {pos} 的截图数据为空。"); return ""; }
        }
        catch (Exception ex) { Console.WriteLine($"OCR 错误: 捕获屏幕失败于 {pos} ({size.Width}x{size.Height}): {ex.Message}"); return ""; }

        try
        {
            using (var content = new MultipartFormDataContent())
            using (var imageContent = new ByteArrayContent(imageBytes))
            {
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                content.Add(imageContent, "image", "screenshot.png");
                content.Add(new StringContent(numbersOnly.ToString().ToLower()), "numbersOnly");
                HttpResponseMessage response = await httpClient.PostAsync(settings.OcrApiUrl, content, token);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) { Console.WriteLine($"OCR API 错误: 请求失败。状态码: {response.StatusCode}, 响应: {jsonResponse}"); return ""; }
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("text", out JsonElement textElement))
                    { string recognizedText = textElement.GetString()?.Trim() ?? ""; Console.WriteLine($"OCR 结果 ({pos.X},{pos.Y}): '{recognizedText}' (仅数字: {numbersOnly})"); return recognizedText; }
                    else if (root.TryGetProperty("error", out JsonElement errorElement)) { Console.WriteLine($"OCR API 错误: 服务器返回错误: {errorElement.GetString()}"); return ""; }
                    else { Console.WriteLine($"OCR API 错误: 未知的响应格式: {jsonResponse}"); return ""; }
                }
            }
        }
        catch (HttpRequestException httpEx) { Console.WriteLine($"OCR 网络错误: {httpEx.Message}"); throw; }
        catch (TaskCanceledException ex) when (ex.CancellationToken == token && token.IsCancellationRequested) { Console.WriteLine($"OCR 操作被 CancellationToken 取消。"); throw; }
        catch (TaskCanceledException) { Console.WriteLine($"OCR 超时 ({settings.OcrTimeoutSeconds}秒)。"); throw; }
        catch (JsonException jsonEx) { Console.WriteLine($"OCR 响应 JSON 解析错误: {jsonEx.Message}"); throw; }
        catch (Exception ex) { Console.WriteLine($"OCR 未知错误（API 调用期间）: {ex.Message}"); throw; }
    }


    private Bitmap CaptureScreen(int x, int y, int width, int height)
    {
        width = Math.Max(1, width); height = Math.Max(1, height);
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try
        {
            using (Graphics g = Graphics.FromImage(bmp))
            { g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy); }
        }
        catch (Exception ex)
        { Console.WriteLine($"截图捕获错误 ({x},{y} - {width}x{height}): {ex.Message}"); bmp?.Dispose(); return new Bitmap(1, 1, PixelFormat.Format32bppArgb); }
        return bmp;
    }

    private Task DelayBeforeCheck(CancellationToken token) => Task.Delay(random.Next(190, 200), token);
    private Task DelayBeforeCheckR(CancellationToken token) => Task.Delay(50, token);
    private Task DelayAction(CancellationToken token) => Task.Delay(random.Next(settings.ActionDelayMin, settings.ActionDelayMax + 1), token);
    private Task DelayOcrRetry(CancellationToken token) => Task.Delay(random.Next(settings.OcrRetryDelayMin, settings.OcrRetryDelayMax + 1), token);
    private Task DelayOcrCheck(CancellationToken token) => Task.Delay(random.Next(settings.OcrCheckDelayMin, settings.OcrCheckDelayMax + 1), token);

    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Console.WriteLine("释放 AutoBuyer 资源...");
                KeyboardHook.StopAutoBuyer -= HandleStopRequest;
                Console.WriteLine("已取消订阅 KeyboardHook。");
                if (running || _stopping) { Console.WriteLine("Dispose: AutoBuyer 正在运行或停止中，启动清理..."); if (!_stopping) _stopping = true; CancelAndCleanup(); }
                else { var currentCts = cts; if (currentCts != null) { try { Console.WriteLine("Dispose: 尝试释放 CancellationTokenSource..."); currentCts.Dispose(); } catch (ObjectDisposedException) { Console.WriteLine("Dispose: CTS 已经释放 (捕获到 ObjectDisposedException)。"); } catch (Exception ex) { Console.WriteLine($"Dispose: 释放 CTS 时出错: {ex.Message}"); } finally { if (ReferenceEquals(cts, currentCts)) { cts = null; } } } }
            }
            disposedValue = true;
            Console.WriteLine("AutoBuyer 已释放。");
        }
    }

    public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
}