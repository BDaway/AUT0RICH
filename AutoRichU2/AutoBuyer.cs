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
using System.Windows.Forms; // ȷ�� using

public class AutoBuyer : IDisposable
{
    private readonly AppSettings settings;
    private volatile bool running = false; // �Ƿ���������
    private CancellationTokenSource cts;
    private Task buyTask;
    private int successfulPurchases = 0;
    private readonly Random random = new Random();
    private volatile bool _stopping = false; // �Ƿ�����ִ��ֹͣ����
    private static readonly HttpClient httpClient = new HttpClient(); // ��̬HttpClient������

    public bool IsRunning => running && buyTask != null && !buyTask.IsCompleted && !_stopping;
    public event Action Stopped;

    public AutoBuyer(AppSettings appSettings)
    {
        this.settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        httpClient.Timeout = TimeSpan.FromSeconds(settings.OcrTimeoutSeconds);
        KeyboardHook.StopAutoBuyer += HandleStopRequest;
        Console.WriteLine("AutoBuyer ��ʼ���������� KeyboardHook��");
    }

    public void Start()
    {
        if (IsRunning) { Console.WriteLine("�Զ������Ѿ��������С�"); return; }
        if (_stopping) { Console.WriteLine("�Զ���������ֹͣ�У��޷�������"); return; }

        Console.WriteLine("���������Զ���������...");
        lock (this)
        {
            if (IsRunning) { Console.WriteLine("�Զ������ѱ������̲߳���������ȡ��������������"); return; }
            _stopping = false;
            successfulPurchases = 0;
            running = true;
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            buyTask = Task.Run(async () => await BuyLoop(token), token);
            Console.WriteLine("�Զ����������ѳɹ�������");
        }
    }

    public void Stop()
    {
        if (!running && !_stopping) { Console.WriteLine("Stop ���󣬵� AutoBuyer δ������δ��ֹͣ�С�"); return; }
        if (_stopping) { Console.WriteLine("ֹͣ�������ڽ����С�"); return; }
        Console.WriteLine("Stop: �յ��ⲿֹͣ����");
        _stopping = true;
        CancelAndCleanup();
    }

    private void CancelAndCleanup()
    {
        Console.WriteLine("CancelAndCleanup: ��ʼִ��ȡ��������...");
        running = false;
        var currentCts = cts;
        if (currentCts != null)
        {
            try
            {
                if (!currentCts.IsCancellationRequested)
                {
                    Console.WriteLine("CancelAndCleanup: ����ȡ�� CancellationTokenSource...");
                    currentCts.Cancel();
                }
                else { Console.WriteLine("CancelAndCleanup: ȡ���ѱ�����"); }
            }
            catch (ObjectDisposedException) { Console.WriteLine("CancelAndCleanup: CancellationTokenSource �ڳ���ȡ��ʱ�ѱ��ͷš�"); }
            catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: ȡ�� CancellationTokenSource ʱ����: {ex.Message}"); }
            finally
            {
                try
                {
                    if (currentCts != null)
                    {
                        Console.WriteLine("CancelAndCleanup: �����ͷ� CancellationTokenSource...");
                        currentCts.Dispose();
                    }
                }
                catch (ObjectDisposedException) { Console.WriteLine("CancelAndCleanup: CTS �Ѿ��ͷ� (���� ObjectDisposedException)��"); }
                catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: �ͷ� CancellationTokenSource ʱ����: {ex.Message}"); }
                finally
                {
                    if (ReferenceEquals(cts, currentCts)) { cts = null; }
                }
            }
        }
        else { Console.WriteLine("CancelAndCleanup: CancellationTokenSource ��Ϊ null��"); }

        var currentTask = buyTask;
        if (currentTask != null && !currentTask.IsCompleted)
        {
            Console.WriteLine("CancelAndCleanup: �ȴ� BuyLoop �������...");
            try
            {
                bool completed = currentTask.Wait(TimeSpan.FromSeconds(2));
                if (!completed) { Console.WriteLine("CancelAndCleanup: ���� - BuyLoop ����δ�ڳ�ʱʱ������ɡ�"); }
                else { Console.WriteLine("CancelAndCleanup: BuyLoop ��������ɡ�"); }
            }
            catch (AggregateException ae) { ae.Handle(ex => ex is OperationCanceledException); Console.WriteLine("CancelAndCleanup: BuyLoop ����Ԥ��ȡ����"); }
            catch (Exception ex) { Console.WriteLine($"CancelAndCleanup: �ȴ� BuyLoop ����ʱ����: {ex.Message}"); }
        }
        buyTask = null;

        try
        {
            Console.WriteLine("CancelAndCleanup: ���� Stopped �¼�...");
            Stopped?.Invoke();
        }
        catch (Exception ex) { Console.WriteLine($"����: ���� Stopped �¼�ʱ����: {ex.Message}"); }
        finally
        {
            _stopping = false;
            Console.WriteLine("CancelAndCleanup: ֹͣ������ɡ�");
        }
    }

    private void HandleStopRequest()
    {
        if (running && !_stopping)
        {
            Console.WriteLine("AutoBuyer �յ����� KeyboardHook ��ֹͣ����");
            Stop();
        }
        else { Console.WriteLine("�յ� KeyboardHook ֹͣ���󣬵� AutoBuyer ��ǰ�޷�ֹͣ��δ���л�����ֹͣ�У���"); }
    }

    public void SetPriceThreshold(int newThreshold) { settings.PriceThreshold = Math.Max(0, newThreshold); Console.WriteLine($"��ߵ��۸���Ϊ: {settings.PriceThreshold}"); }
    public void SetPurchaseQuantity(int quantity) { settings.PurchaseQuantity = quantity; Console.WriteLine($"������������Ϊ: {settings.PurchaseQuantity}"); }
    public void SetStopAfterPurchases(int count) { settings.StopAfterPurchases = Math.Max(0, count); Console.WriteLine($"ֹͣ�����������Ϊ: {settings.StopAfterPurchases} (0=����)"); }

    private async Task BuyLoop(CancellationToken token)
    {
        Console.WriteLine("����ѭ���߳�����...");
        int consecutivePriceCheckFailures = 0; // <--- ��Ӽ�����
        try
        {
            while (running && !token.IsCancellationRequested)
            {
                bool purchasedOrFailedThisRound = false;
                bool errorOccurred = false;
                try
                {
                    token.ThrowIfCancellationRequested();

                    Console.WriteLine("BuyLoop: ������Ʒҳ��...");
                    EnterItemPage();
                    await DelayBeforeCheck(token);
                    await DelayAction(token);
                    token.ThrowIfCancellationRequested();
                    Console.WriteLine("BuyLoop: ����ʼ�۸�...");
                    int price = await CheckPrice(token);

                    if (price == -1)
                    {
                        consecutivePriceCheckFailures++; // <--- ʧ��ʱ����
                        Console.WriteLine($"BuyLoop: δ��ʶ���ʼ�۸� (����ʧ��: {consecutivePriceCheckFailures})������Ƿ���Ҫˢ��...");

                        if (consecutivePriceCheckFailures >= 5) // <--- �����ֵ
                        {
                            Console.WriteLine($"BuyLoop: ���� {consecutivePriceCheckFailures} �μ۸�ʶ��ʧ�ܣ�ִ��ˢ�²���...");
                            RefreshPrice(); // ִ��ˢ��
                            await DelayAction(token); // ˢ�º��ӳ�
                            consecutivePriceCheckFailures = 0; // <--- ˢ�º����ü�����
                            continue; // <--- ������һ��ѭ��
                        }
                        else
                        {
                            // ʧ�ܴ���δ����ֵ��ֱ�ӽ�����һ��ѭ��
                            Console.WriteLine("BuyLoop: ����ʧ�ܴ���δ����ֵ����ʼ��һ��ѭ��...");
                            continue;
                        }
                    }
                    else // �۸���ɹ�
                    {
                        consecutivePriceCheckFailures = 0; // <--- �ɹ�ʱ���ü�����
                        // ... (����ԭ���߼�) ...
                        if (price <= settings.PriceThreshold)
                        {
                            Console.WriteLine($"BuyLoop: ��⵽�͵��� {price} <= {settings.PriceThreshold}������������� {settings.PurchaseQuantity}...");
                            ClickQuantityButton();
                            await DelayBeforeCheckR(token);
                            await DelayAction(token);
                            token.ThrowIfCancellationRequested();
                            Console.WriteLine("BuyLoop: �����������ƽ���۸�...");
                            int averagePrice = await CheckPrice(token);

                            if (averagePrice == -1)
                            {
                                consecutivePriceCheckFailures++; // <--- ʧ��ʱ����
                                Console.WriteLine($"BuyLoop: ���������δ��ʶ��ƽ���۸� (����ʧ��: {consecutivePriceCheckFailures})������Ƿ���Ҫˢ��...");

                                if (consecutivePriceCheckFailures >= 5) // <--- �����ֵ
                                {
                                    Console.WriteLine($"BuyLoop: ���� {consecutivePriceCheckFailures} �μ۸�ʶ��ʧ�ܣ�ִ��ˢ�²���...");
                                    RefreshPrice(); // ִ��ˢ��
                                    await DelayAction(token); // ˢ�º��ӳ�
                                    consecutivePriceCheckFailures = 0; // <--- ˢ�º����ü�����
                                    continue; // <--- ������һ��ѭ��
                                }
                                else
                                {
                                    // ʧ�ܴ���δ����ֵ��ֱ�ӽ�����һ��ѭ��
                                    Console.WriteLine("BuyLoop: ����ʧ�ܴ���δ����ֵ����ʼ��һ��ѭ��...");
                                    continue;
                                }
                            }
                            else // ƽ���۸���ɹ�
                            {
                                consecutivePriceCheckFailures = 0; // <--- �ɹ�ʱ���ü�����
                                // ... (����ԭ���߼�) ...
                                if (averagePrice <= settings.PriceThreshold)
                                {
                                    Console.WriteLine($"BuyLoop: ƽ���۸�ȷ�� {averagePrice} <= {settings.PriceThreshold}��ִ�й���...");
                                    ClickAt(settings.BuyButtonPos.X, settings.BuyButtonPos.Y, false);
                                    await Task.Delay(random.Next(settings.NotificationDelayMin, settings.NotificationDelayMax + 1), token);
                                    token.ThrowIfCancellationRequested();
                                    Console.WriteLine("BuyLoop: ��鹺��֪ͨ...");
                                    bool success = await CheckPurchaseNotification(token);
                                    purchasedOrFailedThisRound = true;

                                    if (success)
                                    {
                                        successfulPurchases++;
                                        Console.WriteLine($"BuyLoop: ����ɹ�! (��ǰ�ɹ� {successfulPurchases}/{settings.StopAfterPurchases})");
                                        if (settings.StopAfterPurchases > 0 && successfulPurchases >= settings.StopAfterPurchases)
                                        {
                                            Console.WriteLine($"BuyLoop: �ﵽĿ�깺����� {settings.StopAfterPurchases}��ֹͣ...");
                                            running = false;
                                            break;
                                        }
                                    }
                                    else { Console.WriteLine("BuyLoop: ����ʧ�ܻ�δʶ��ɹ���ʾ����ˢ��..."); }
                                }
                                else { Console.WriteLine($"BuyLoop: ƽ���۸� {averagePrice} > {settings.PriceThreshold}��ˢ��..."); purchasedOrFailedThisRound = true; }
                            }
                        }
                        else { Console.WriteLine($"BuyLoop: ��ʼ���� {price} > {settings.PriceThreshold}��ˢ��..."); purchasedOrFailedThisRound = true; }
                    }
                }
                catch (OperationCanceledException) { Console.WriteLine("BuyLoop: ������ȡ����"); throw; }
                catch (HttpRequestException httpEx) { Console.WriteLine($"BuyLoop ����: OCR API ����ʧ��: {httpEx.Message}"); errorOccurred = true; }
                catch (JsonException jsonEx) { Console.WriteLine($"BuyLoop ����: OCR ��Ӧ����ʧ��: {jsonEx.Message}"); errorOccurred = true; }
                catch (Exception ex) { Console.WriteLine($"BuyLoop ����: �����������: {ex.Message}\n{ex.StackTrace}"); errorOccurred = true; }

                // ˢ���߼�: ���ڹ����Ժ�����Ǽ۸�ʶ�����ʱ����
                // �����Ϊ�����۸�ʧ�ܴ�����ˢ�£������ continue �Ѿ�����������
                if (running && !token.IsCancellationRequested && (purchasedOrFailedThisRound || errorOccurred))
                {
                    Console.WriteLine("BuyLoop: �����Ի�Ǽ۸������ˢ�¼۸�..."); // ��ȷ��־
                    RefreshPrice();
                    await DelayAction(token);
                    consecutivePriceCheckFailures = 0; // <--- ������ԭ���ˢ�º�Ҳ���ü�����
                }
                else if (!running || token.IsCancellationRequested)
                {
                    Console.WriteLine("BuyLoop: �� running=false ��ȡ��������˳�ѭ����");
                    break;
                }
            }
            token.ThrowIfCancellationRequested();
            Console.WriteLine("BuyLoop: ѭ��������������ﵽ�������ƣ���");
        }
        catch (OperationCanceledException) { Console.WriteLine("BuyLoop: ����ȡ����"); }
        catch (Exception ex) { Console.WriteLine($"BuyLoop: ����������ֹ: {ex.Message}\n{ex.StackTrace}"); }
        finally
        {
            Console.WriteLine("BuyLoop: �˳� BuyLoop ����ͨ�� CancelAndCleanup ��������...");
            if (!_stopping) { _stopping = true; CancelAndCleanup(); }
        }
    }


    // ... (ClickQuantityButton, EnterItemPage, RefreshPrice, ClickAt, CheckPrice, CheckPurchaseNotification, RecognizeScreenArea, CaptureScreen, �ӳٷ���, Dispose �ȱ��ֲ���) ...
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
        Console.WriteLine($"����: ���������ť ({settings.PurchaseQuantity}) �� {pos}");
        ClickAt(pos.X, pos.Y, true);
    }

    private void EnterItemPage() { Console.WriteLine($"����: �����Ʒ��ڵ��� {settings.ItemClickPos}"); ClickAt(settings.ItemClickPos.X, settings.ItemClickPos.Y, false); }

    private void RefreshPrice()
    {
        Console.WriteLine("����: ���� ESC ��ˢ�¼۸�...");
        if (!running || cts?.IsCancellationRequested == true)
        { Console.WriteLine("RefreshPrice: ��δ���л�����ȡ��������������"); return; }
        KeyboardSimulator.SendKey(Keys.Escape);
    }

    private void ClickAt(int x, int y, bool preciseEnd)
    {
        if (!running || (cts?.IsCancellationRequested ?? true))
        { Console.WriteLine($"ClickAt ({x},{y}): ��δ���л�����ȡ����������"); return; }
        MouseSimulator.MoveTo(x, y, preciseEnd);
        if (!running || (cts?.IsCancellationRequested ?? true))
        { Console.WriteLine($"ClickAt ({x},{y}): ���ƶ��󡢵��ǰ��⵽ȡ�������������"); return; }
        MouseSimulator.Click();
    }


    private async Task<int> CheckPrice(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 1; attempt <= settings.OcrRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine($"�۸���: ���� {attempt}/{settings.OcrRetryAttempts}");
            string text1 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token);
            lastReadText = text1;

            if (!string.IsNullOrWhiteSpace(text1) && int.TryParse(text1.Trim(), out int price1))
            {
                Console.WriteLine($"�۸���: ��ȡ���۸� {price1}�����ζ�ȡ��ȷ��...");
                await DelayOcrCheck(token);
                token.ThrowIfCancellationRequested();
                string text2 = await RecognizeScreenArea(settings.PriceListPos, settings.PriceListSize, true, token);
                lastReadText = text2;

                if (!string.IsNullOrWhiteSpace(text2) && int.TryParse(text2.Trim(), out int price2))
                {
                    if (price1 == price2) { Console.WriteLine($"�۸���: ȷ�ϼ۸�: {price1}"); return price1; }
                    else { Console.WriteLine($"�۸��龯��: ���μ۸�һ�� ({price1} vs {price2})������..."); }
                }
                else { Console.WriteLine($"�۸��龯��: ���ζ�ȡʧ�ܻ���Ч ('{text2}')������..."); }
            }
            else { Console.WriteLine($"�۸��龯��: �״ζ�ȡʧ�ܻ���Ч ('{text1}')������..."); }

            if (attempt < settings.OcrRetryAttempts) { await DelayOcrRetry(token); }
        }
        Console.WriteLine($"�۸������: {settings.OcrRetryAttempts} �γ��Ժ�ʧ�ܡ�����ȡ�ı�: '{lastReadText}'");
        return -1;
    }

    private async Task<bool> CheckPurchaseNotification(CancellationToken token)
    {
        string lastReadText = "N/A";
        for (int attempt = 1; attempt <= settings.NotificationRetryAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine($"֪ͨ���: ���� {attempt}/{settings.NotificationRetryAttempts}");
            string text = await RecognizeScreenArea(settings.NotificationPos, settings.NotificationSize, false, token);
            lastReadText = text?.Trim() ?? "";

            foreach (string keyword in settings.SuccessKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                { Console.WriteLine($"֪ͨ���: �� '{lastReadText}' ���ҵ��ɹ��ؼ��� '{keyword}'��"); return true; }
            }
            foreach (string keyword in settings.FailureKeywords)
            {
                if (!string.IsNullOrEmpty(lastReadText) && lastReadText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                { Console.WriteLine($"֪ͨ���: �� '{lastReadText}' ���ҵ�ʧ�ܹؼ��� '{keyword}'��"); return false; }
            }
            Console.WriteLine($"֪ͨ���: �� '{lastReadText}' ��δ�ҵ���ȷ�ؼ��֡�����...");
            if (attempt < settings.NotificationRetryAttempts) { await DelayOcrRetry(token); }
        }
        Console.WriteLine($"֪ͨ��龯��: {settings.NotificationRetryAttempts} �γ��Ժ�δ��ʶ����������ȡ�ı�: '{lastReadText}'");
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
            if (imageBytes == null || imageBytes.Length == 0) { Console.WriteLine($"OCR ����: ���� {pos} �Ľ�ͼ����Ϊ�ա�"); return ""; }
        }
        catch (Exception ex) { Console.WriteLine($"OCR ����: ������Ļʧ���� {pos} ({size.Width}x{size.Height}): {ex.Message}"); return ""; }

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
                if (!response.IsSuccessStatusCode) { Console.WriteLine($"OCR API ����: ����ʧ�ܡ�״̬��: {response.StatusCode}, ��Ӧ: {jsonResponse}"); return ""; }
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("text", out JsonElement textElement))
                    { string recognizedText = textElement.GetString()?.Trim() ?? ""; Console.WriteLine($"OCR ��� ({pos.X},{pos.Y}): '{recognizedText}' (������: {numbersOnly})"); return recognizedText; }
                    else if (root.TryGetProperty("error", out JsonElement errorElement)) { Console.WriteLine($"OCR API ����: ���������ش���: {errorElement.GetString()}"); return ""; }
                    else { Console.WriteLine($"OCR API ����: δ֪����Ӧ��ʽ: {jsonResponse}"); return ""; }
                }
            }
        }
        catch (HttpRequestException httpEx) { Console.WriteLine($"OCR �������: {httpEx.Message}"); throw; }
        catch (TaskCanceledException ex) when (ex.CancellationToken == token && token.IsCancellationRequested) { Console.WriteLine($"OCR ������ CancellationToken ȡ����"); throw; }
        catch (TaskCanceledException) { Console.WriteLine($"OCR ��ʱ ({settings.OcrTimeoutSeconds}��)��"); throw; }
        catch (JsonException jsonEx) { Console.WriteLine($"OCR ��Ӧ JSON ��������: {jsonEx.Message}"); throw; }
        catch (Exception ex) { Console.WriteLine($"OCR δ֪����API �����ڼ䣩: {ex.Message}"); throw; }
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
        { Console.WriteLine($"��ͼ������� ({x},{y} - {width}x{height}): {ex.Message}"); bmp?.Dispose(); return new Bitmap(1, 1, PixelFormat.Format32bppArgb); }
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
                Console.WriteLine("�ͷ� AutoBuyer ��Դ...");
                KeyboardHook.StopAutoBuyer -= HandleStopRequest;
                Console.WriteLine("��ȡ������ KeyboardHook��");
                if (running || _stopping) { Console.WriteLine("Dispose: AutoBuyer �������л�ֹͣ�У���������..."); if (!_stopping) _stopping = true; CancelAndCleanup(); }
                else { var currentCts = cts; if (currentCts != null) { try { Console.WriteLine("Dispose: �����ͷ� CancellationTokenSource..."); currentCts.Dispose(); } catch (ObjectDisposedException) { Console.WriteLine("Dispose: CTS �Ѿ��ͷ� (���� ObjectDisposedException)��"); } catch (Exception ex) { Console.WriteLine($"Dispose: �ͷ� CTS ʱ����: {ex.Message}"); } finally { if (ReferenceEquals(cts, currentCts)) { cts = null; } } } }
            }
            disposedValue = true;
            Console.WriteLine("AutoBuyer ���ͷš�");
        }
    }

    public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
}