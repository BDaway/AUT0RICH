using System;
using System.Drawing; // ��Ҫ���� System.Drawing.Common NuGet ����.NET Core/5+���������Ŀ����
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging; // For ImageFormat

/// <summary>
/// ʵ���Զ�����ĺ����߼���
/// </summary>
public class AutoBuyer : IDisposable // Implement IDisposable
{
    private readonly AppSettings settings; // ����Ӧ�ó�������
    private volatile bool running = false; // ���ƹ���ѭ��������״̬ (volatile ȷ���߳̿ɼ���)
    private CancellationTokenSource cts;   // ����ȡ����̨����
    private Task buyTask;                  // ��̨�������������
    private int successfulPurchases = 0;   // �ɹ������������
    private readonly Random random = new Random(); // Use readonly
    private volatile bool _stopping = false; // Add a flag to prevent race conditions/multiple stops

    // ����� HttpClient ʵ����������ܲ�������Դռ��
    // ConfigureAwait(false) can be important if this runs in a context with a SynchronizationContext (like WinForms old style async)
    // but with Task.Run it's less critical. Still good practice.
    private static readonly HttpClient httpClient = new HttpClient();

    /// <summary>
    /// ��ȡһ��ֵ����ֵָʾ�Զ���������ǰ�Ƿ��������С�
    /// </summary>
    public bool IsRunning => running && buyTask != null && !buyTask.IsCompleted;

    /// <summary>
    /// ���Զ��������ֹͣʱ���������ֶ����ﵽ�������ǳ���������
    /// </summary>
    public event Action Stopped;

    /// <summary>
    /// ��ʼ�� AutoBuyer��
    /// </summary>
    /// <param name="appSettings">Ӧ�ó�����������á�</param>
    public AutoBuyer(AppSettings appSettings)
    {
        this.settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        httpClient.Timeout = TimeSpan.FromSeconds(settings.OcrTimeoutSeconds);
        KeyboardHook.StopAutoBuyer += HandleStopRequest;
    }

    /// <summary>
    /// �����Զ��������̡�
    /// </summary>
    /// <exception cref="InvalidOperationException">����޼�����δ��ʼ����</exception>
    public void Start()
    {
        if (IsRunning)
        {
            Console.WriteLine("�Զ������Ѿ��������С�");
            return;
        }
        if (!LogitechMouse.IsInitialized)
        {
            Console.WriteLine("�����޼�����δ��ʼ�����޷���������");
            throw new InvalidOperationException("�޼�����δ��ʼ�����޷�������");
        }

        _stopping = false; // ���� stopping ��־

        Console.WriteLine("��ʼ�����Զ���������...");
        successfulPurchases = 0;
        running = true;
        cts = new CancellationTokenSource();
        // Pass the token to the loop correctly
        CancellationToken token = cts.Token;
        buyTask = Task.Run(async () => await BuyLoop(token), token); // ʹ�� async lambda
        Console.WriteLine("�Զ�����������������");
    }

    /// <summary>
    /// ����ֹͣ�Զ��������̡�
    /// </summary>
    public void Stop()
    {
        // Check running first, but allow stopping even if already _stopping to ensure event fires?
        // Let OnStopped handle the logic
        if (!running && !_stopping) // If not running and not already stopping, do nothing.
        {
            Console.WriteLine("Stop ���󣬵� AutoBuyer δ������δ��ֹͣ�С�");
            return;
        }
        Console.WriteLine("Stop: �ⲿ����ֹͣ...");
        OnStopped(); // Trigger the unified stop logic
    }

    // Helper to trigger Stopped event and perform cleanup (idempotent)
    private void OnStopped()
    {
        // Use lock or Interlocked for thread safety if needed, but simple flag might suffice here
        if (_stopping) return; // Already processing stop
        _stopping = true; // Signal that stop processing has started

        Console.WriteLine("OnStopped: ��ʼִ��ֹͣ����...");

        running = false; // Ensure running state is false

        var currentCts = cts; // Use local variable
        if (currentCts != null)
        {
            try
            {
                if (!currentCts.IsCancellationRequested)
                {
                    Console.WriteLine("OnStopped: ����ȡ�� CancellationTokenSource...");
                    currentCts.Cancel();
                }
                Console.WriteLine("OnStopped: �ͷ� CancellationTokenSource...");
                currentCts.Dispose();
            }
            catch (ObjectDisposedException) { Console.WriteLine("OnStopped: CancellationTokenSource �ѱ��ͷš�"); }
            catch (Exception ex) { Console.WriteLine($"OnStopped: ���� CancellationTokenSource ʱ����: {ex.Message}"); }
            finally { cts = null; } // Mark as disposed/nullified
        }
        else { Console.WriteLine("OnStopped: CancellationTokenSource Ϊ null ���ѱ��ͷš�"); }

        buyTask = null; // Clear task reference

        try
        {
            Console.WriteLine("OnStopped: ���� Stopped �¼�...");
            Stopped?.Invoke(); // Notify listeners (like MainForm)
        }
        catch (Exception ex)
        {
            Console.WriteLine($"����: ���� Stopped �¼��������ʱ����: {ex.Message}");
        }
        finally
        {
            // _stopping = false; // Don't reset here, reset in Start()
            Console.WriteLine("OnStopped: ֹͣ������ɡ�");
        }
    }

    // Handles stop request from KeyboardHook
    private void HandleStopRequest()
    {
        if (running && !_stopping) // Only process if running and not already stopping
        {
            Console.WriteLine("�յ����Լ��̹��ӵ�ֹͣ����");
            Stop(); // Use the standard stop method
        }
    }

    // --- Methods to update settings dynamically ---
    public void SetPriceThreshold(int newThreshold)
    {
        settings.PriceThreshold = Math.Max(0, newThreshold); // Ensure non-negative
        Console.WriteLine($"��ߵ����Ѹ���Ϊ: {settings.PriceThreshold}");
    }

    public void SetPurchaseQuantity(int quantity)
    {
        // Validate quantity if needed, e.g., only allow 1, 25, 100, 200
        settings.PurchaseQuantity = quantity;
        Console.WriteLine($"���������Ѹ���Ϊ: {settings.PurchaseQuantity}");
    }

    public void SetStopAfterPurchases(int count)
    {
        settings.StopAfterPurchases = Math.Max(0, count); // Ensure non-negative
        Console.WriteLine($"ֹͣ��������Ѹ���Ϊ: {settings.StopAfterPurchases} (0 ��ʾ����)");
    }


    /// <summary>
    /// ���Ĺ���ѭ�����ں�̨�߳����С�
    /// </summary>
    private async Task BuyLoop(CancellationToken token)
    {
        Console.WriteLine("����ѭ���߳�����...");
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
                        Console.WriteLine("δ��ʶ���ʼ�۸񣬽�ˢ��ҳ������...");
                        errorOccurred = true;
                    }
                    else if (price <= settings.PriceThreshold)
                    {
                        Console.WriteLine($"��⵽�͵��� {price} <= {settings.PriceThreshold}������������� {settings.PurchaseQuantity}...");
                        ClickQuantityButton();
                        await Task.Delay(random.Next(settings.DelayClickMin, settings.DelayClickMax), token);
                        token.ThrowIfCancellationRequested();

                        int averagePrice = await CheckPrice(token);
                        token.ThrowIfCancellationRequested();

                        if (averagePrice == -1)
                        {
                            Console.WriteLine("���������δ��ʶ��ƽ���۸񣬽�ˢ��ҳ������...");
                            errorOccurred = true;
                        }
                        else if (averagePrice <= settings.PriceThreshold)
                        {
                            Console.WriteLine($"���������ƽ���۸� {averagePrice} <= {settings.PriceThreshold}��ִ�й���...");
                            ClickAt(settings.BuyButtonPos.X, settings.BuyButtonPos.Y, false);
                            await Task.Delay(random.Next(settings.DelayLongMin, settings.DelayLongMax), token);
                            token.ThrowIfCancellationRequested();

                            bool success = await CheckPurchaseNotification(token);
                            purchasedOrFailedThisRound = true;

                            if (success)
                            {
                                successfulPurchases++;
                                Console.WriteLine($"����ɹ�! (��ǰ�ɹ� {successfulPurchases} �� / Ŀ�� {(settings.StopAfterPurchases == 0 ? "����" : settings.StopAfterPurchases.ToString())})");
                                if (settings.StopAfterPurchases > 0 && successfulPurchases >= settings.StopAfterPurchases)
                                {
                                    Console.WriteLine($"�ѴﵽĿ�깺����� {settings.StopAfterPurchases}��ֹͣ�Զ�����...");
                                    Stop(); // Request stop through the standard mechanism
                                    break; // Exit the loop cleanly after requesting stop
                                }
                            }
                            else
                            {
                                Console.WriteLine("����ʧ�ܻ�δʶ�𵽳ɹ���ʾ����ˢ��...");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"���������ƽ���۸� {averagePrice} > {settings.PriceThreshold}���۸񲻺��ʣ�ˢ��...");
                            purchasedOrFailedThisRound = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"��ʼ���� {price} > {settings.PriceThreshold}���۸񲻺��ʣ�ˢ��...");
                        purchasedOrFailedThisRound = true;
                    }
                }
                catch (OperationCanceledException) { throw; } // Re-throw cancellation
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"����: OCR API ����ʧ��: {httpEx.Message}"); errorOccurred = true;
                    await Task.Delay(random.Next(settings.DelayMediumMin, settings.DelayMediumMax), CancellationToken.None); // Wait before retry
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"����: OCR API ��Ӧ����ʧ��: {jsonEx.Message}"); errorOccurred = true;
                    await Task.Delay(random.Next(settings.DelayMediumMin, settings.DelayMediumMax), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"����: ����ѭ���з����������: {ex.Message}\n{ex.StackTrace}");
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
            Console.WriteLine("����ѭ����ȡ����");
            // OnStopped() will be called in finally
        }
        catch (Exception ex) // Catch unexpected exceptions from the loop itself
        {
            Console.WriteLine($"���ش���: BuyLoop ������ֹ: {ex.Message}\n{ex.StackTrace}");
            // OnStopped() will be called in finally
        }
        finally
        {
            Console.WriteLine("BuyLoop ������� (finally ��)��");
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
        Console.WriteLine("����: ������Ʒҳ��...");
        ClickAt(settings.ItemClickPos.X, settings.ItemClickPos.Y, false);
    }

    private void RefreshPrice()
    {
        Console.WriteLine("����: ���ز����½�����ˢ�¼۸�...");
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
                        Console.WriteLine($"�۸�ȷ��: {price1}");
                        return price1;
                    }
                    else { Console.WriteLine($"����: �۸� OCR ���ζ�����һ�� ({price1} vs {price2})������..."); }
                }
            }
            await Task.Delay(random.Next(settings.DelayRetryWaitMin, settings.DelayRetryWaitMax), token).ConfigureAwait(false);
        }
        Console.WriteLine($"����: ���۸��� {settings.OcrRetryAttempts} �γ��Ժ�ʧ�ܡ����ʶ���ı�: '{lastReadText}'");
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
                    Console.WriteLine($"ʶ�𵽹���ɹ��ؼ���: '{keyword}'");
                    return true;
                }
            }
            foreach (string keyword in settings.FailureKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine($"ʶ�𵽹���ʧ�ܹؼ���: '{keyword}'");
                    return false;
                }
            }
            await Task.Delay(random.Next(settings.DelayRetryWaitMin, settings.DelayRetryWaitMax), token).ConfigureAwait(false);
        }
        Console.WriteLine($"����: ��鹺��֪ͨ�� {settings.NotificationRetryAttempts} �γ��Ժ�δʶ����ȷ��������ʶ���ı�: '{lastReadText}'��������ʧ�ܴ���");
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
                Console.WriteLine("����: ��ͼ��δ�ܻ�ȡͼ�����ݡ�"); return "";
            }
        }
        catch (Exception ex) { Console.WriteLine($"����: ��ͼ��ͼ��ת��ʧ�� ({pos.X},{pos.Y}): {ex.Message}"); return ""; }

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
                    Console.WriteLine($"����: OCR API ����ʧ�ܡ�״̬��: {response.StatusCode}, URL: {settings.OcrApiUrl}, ��Ӧ: {errorResponse}");
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
                        Console.WriteLine($"����: OCR API ���ش�����Ϣ: {errorElement.GetString()}"); return "";
                    }
                    else { Console.WriteLine($"����: OCR API ��Ӧ��ʽδ֪: {jsonResponse}"); return ""; }
                }
            }
        }
        catch (HttpRequestException httpEx) { Console.WriteLine($"����: OCR API ��������ʧ��: {httpEx.Message}��URL: {settings.OcrApiUrl}"); throw; }
        catch (TaskCanceledException) // Don't need the variable name if unused
        {
            if (token.IsCancellationRequested) { Console.WriteLine("OCR �����û�ȡ����"); throw new OperationCanceledException(token); }
            else { Console.WriteLine($"����: OCR API ����ʱ ({settings.OcrTimeoutSeconds} ��)��URL: {settings.OcrApiUrl}��"); throw; }
        }
        catch (JsonException jsonEx) { Console.WriteLine($"����: ���� OCR API ��Ӧʧ��: {jsonEx.Message}��"); throw; }
        catch (Exception ex) { Console.WriteLine($"����: ���� OCR API ʱ����δ֪����: {ex.Message}"); throw; }
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
            Console.WriteLine($"����: ��ͼʧ�� ({x},{y} - {width}x{height}): {ex.Message}");
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
