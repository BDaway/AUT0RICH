// AppSettings.cs
using System;
using System.Drawing;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Forms;

/// <summary>
/// �洢Ӧ�ó���������������á�
/// ��Ҫ��ʾ���û������ʹ�ñ�����������ø���ȫ�����Ρ�
/// �û�����ȷ����ʹ�÷�ʽ�����κ�Ŀ��Ӧ�ó���ķ����������ط��ɷ��档
/// ��Դ�˴����Ϊ����������ѧϰĿ�ġ�
/// </summary>
public class AppSettings
{
    // --- �����߼�������� ---
    /// <summary>���ڻ���ڴ˼۸�ų��Թ���</summary>
    public int PriceThreshold { get; set; } = 10;
    /// <summary>ÿ�γ��Թ�������� (���� 1, 25, 100, 200)</summary>
    public int PurchaseQuantity { get; set; } = 200;
    /// <summary>����ɹ����ٴκ��Զ�ֹͣ (0 = ����)</summary>
    public int StopAfterPurchases { get; set; } = 0;

    // --- OCR (��ѧ�ַ�ʶ��) ������� ---
    /// <summary>�û��������е� OCR �������ĵ�ַ (��Ҫ�û����в�������м��ݵ� OCR API ����)</summary>
    public string OcrApiUrl { get; set; } = "http://localhost:8000/recognize";
    /// <summary>OCR ����ĳ�ʱʱ�䣨�룩</summary>
    public int OcrTimeoutSeconds { get; set; } = 5;
    /// <summary>�۸��֪ͨʶ��ʧ��ʱ�����Դ���</summary>
    public int OcrRetryAttempts { get; set; } = 5;
    /// <summary>�������֪ͨ�����Դ���</summary>
    public int NotificationRetryAttempts { get; set; } = 5;
    /// <summary>
    /// !! �û��������� !!
    /// �жϹ���ɹ��Ĺؼ��� (��Ҫ����Ŀ��Ӧ�ó������ʾ���������)
    /// </summary>
    public string[] SuccessKeywords { get; set; } = new string[0]; // ���Ĭ��ֵ
    /// <summary>
    /// !! �û��������� !!
    /// �жϹ���ʧ�ܵĹؼ��� (��Ҫ����Ŀ��Ӧ�ó������ʾ���������)
    /// </summary>
    public string[] FailureKeywords { get; set; } = new string[0]; // ���Ĭ��ֵ

    // --- ��Ļ����������С���� ---
    // !! �û��������� !! ������������ͳߴ綼��Ҫ�û�������Ŀ��Ӧ�ó������Ļ�ֱ�������У׼�����á�
    // ����ͨ����ͼ���߻�ר�ŵ�����ʰȡ���߻�ȡ��

    /// <summary>
    /// !! �û��������� !!
    /// �۸��б�ͨ���ǵ�һ���ɹ�����Ʒ�ļ۸񣩵����Ͻ���Ļ���ꡣ
    /// </summary>
    public Point PriceListPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// �۸�ʶ������Ĵ�С (���, �߶�)��
    /// </summary>
    public Size PriceListSize { get; set; } = Size.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// ����ɹ�/ʧ��֪ͨ��������Ͻ���Ļ���ꡣ
    /// </summary>
    public Point NotificationPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// ֪ͨʶ������Ĵ�С (���, �߶�)��
    /// </summary>
    public Size NotificationSize { get; set; } = Size.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// ���ڵ��������Ʒ����ҳ�����Ļ���ꡣ
    /// </summary>
    public Point ItemClickPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    // public Point BackButtonPos { get; set; } = Point.Empty; // ���ذ�ť���� (RefreshPrice �Ѹ�Ϊ�� ESC�������ò���ֱ��ʹ��)
    /// <summary>
    /// !! �û��������� !!
    /// "����" ������ȷ�ϰ�ť����Ļ���ꡣ
    /// </summary>
    public Point BuyButtonPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// �������� "1" (����С����) ��ť����Ļ���ꡣ
    /// </summary>
    public Point MinQtyButtonPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// �������� "25" (�����Ƶ�С����) ��ť����Ļ���ꡣ
    /// </summary>
    public Point SmallQtyButtonPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// �������� "100" (�����Ƶ�������) ��ť����Ļ���ꡣ
    /// </summary>
    public Point MediumQtyButtonPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ
    /// <summary>
    /// !! �û��������� !!
    /// �������� "200" (�����Ƶ��������) ��ť����Ļ���ꡣ
    /// </summary>
    public Point MaxQtyButtonPos { get; set; } = Point.Empty; // ����Ϊ��Чֵ

    // --- �ӳٺ�ʱ������ (����) ---
    // ��Щ����Ϊ���ã����Ա��������Ĭ��ֵ�����û��Կɵ���
    /// <summary>ÿ�������/���̲��������С�ȴ�ʱ��</summary>
    public int ActionDelayMin { get; set; } = 15;
    /// <summary>ÿ�������/���̲���������ȴ�ʱ��</summary>
    public int ActionDelayMax { get; set; } = 30;
    /// <summary>OCR ʶ��ʧ�ܺ�����ǰ����С�ȴ�ʱ��</summary>
    public int OcrRetryDelayMin { get; set; } = 15;
    /// <summary>OCR ʶ��ʧ�ܺ�����ǰ�����ȴ�ʱ��</summary>
    public int OcrRetryDelayMax { get; set; } = 20;
    /// <summary>�������󣬼��֪ͨǰ����С�ȴ�ʱ��</summary>
    public int NotificationDelayMin { get; set; } = 500;
    /// <summary>�������󣬼��֪ͨǰ�����ȴ�ʱ��</summary>
    public int NotificationDelayMax { get; set; } = 510;
    /// <summary>�۸����ȷ�϶�ȡ֮�����С�ӳ�</summary>
    public int OcrCheckDelayMin { get; set; } = 5;
    /// <summary>�۸����ȷ�϶�ȡ֮�������ӳ�</summary>
    public int OcrCheckDelayMax { get; set; } = 10;

    // --- ��ʱ������ ---
    /// <summary>�Ƿ����ö�ʱ����/ֹͣ����</summary>
    public bool IsTimerEnabled { get; set; } = false;
    /// <summary>��ʱ����ʱ�� (HH:mm:ss)</summary>
    public string StartTimeString { get; set; } = "08:00:00";
    /// <summary>��ʱֹͣʱ�� (HH:mm:ss)</summary>
    public string StopTimeString { get; set; } = "23:00:00";

    // --- ���ڵ������� (WindowAdjuster ʹ��) ---
    /// <summary>���ڴ��ڵ������ܵ�Ŀ�괰�ڿ�� (�û����޸�)</summary>
    public int TargetWindowWidth { get; set; } = 1920;
    /// <summary>���ڴ��ڵ������ܵ�Ŀ�괰�ڸ߶� (�û����޸�)</summary>
    public int TargetWindowHeight { get; set; } = 1080;
    /// <summary>���ڴ��ڵ������ܵ�Ŀ�괰�����Ͻ� X ���� (�û����޸�)</summary>
    public int TargetWindowX { get; set; } = 0;
    /// <summary>���ڴ��ڵ������ܵ�Ŀ�괰�����Ͻ� Y ���� (�û����޸�)</summary>
    public int TargetWindowY { get; set; } = 0;

    // --- ���ģ����������� (MouseSimulator ʹ��) ---
    /// <summary>����ƶ�Ŀ��㸽�������ƫ�Ʒ�Χ (+/-)</summary>
    public int RandomRangeNear { get; set; } = 3;
    /// <summary>����ƶ������е����������Χ (+/-)</summary>
    public int RandomRangeShake { get; set; } = 2;

    // --- �ȼ����� (KeyboardHook ʹ��) ---
    /// <summary>����ֹͣ AutoBuyer ��ȫ���ȼ� (�û����޸�)</summary>
    public Keys StopKey { get; set; } = Keys.F12;

    /// <summary>
    /// ��ȡ�����ļ�������·����
    /// �����ļ��洢�� %AppData%\[AppName]\config.json
    /// [AppName] �ڴ˶���Ϊ "AUT0RICH_Lite2"
    /// </summary>
    [JsonIgnore] // ������Բ���Ҫ���浽 JSON �ļ���
    public static string ConfigFilePath
    {
        get
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // !! ������Ĵ����ƣ��û����е������ļ������ᱻ�ҵ� !!
            string appName = "AUT0RICH_Lite2";
            string appFolder = Path.Combine(appDataPath, appName);
            try // ����Ŀ¼�����Ĵ�����
            {
                Directory.CreateDirectory(appFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�����޷����������ļ��� '{appFolder}': {ex.Message}");
                
            }
            return Path.Combine(appFolder, "config.json");
        }
    }
}