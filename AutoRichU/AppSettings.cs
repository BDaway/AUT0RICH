using System;
using System.Drawing; // ��Ҫ���� System.Drawing.Common NuGet ����.NET Core/5+���������Ŀ����
using System.IO;
using System.Text.Json.Serialization; // ���ں����ض����Ե����л�
using System.Windows.Forms; // Needed for Keys enum

/// <summary>
/// �洢Ӧ�ó�������п��������á�
/// </summary>
public class AppSettings
{
    // --- AutoBuyer Settings ---
    public int PriceThreshold { get; set; } = 10; // ��߹��򵥼���ֵ
    public int PurchaseQuantity { get; set; } = 200; // Ĭ�Ϲ������� (1, 25, 100, 200)
    public int StopAfterPurchases { get; set; } = 0; // ������ٴκ�ֹͣ (0 = ����)
    public string OcrApiUrl { get; set; } = "http://localhost:8000/recognize"; // OCR API ��ַ
    public int OcrTimeoutSeconds { get; set; } = 5; // OCR API ��ʱʱ�䣨�룩
    public int OcrRetryAttempts { get; set; } = 5; // OCR ʶ�����Դ���
    public int NotificationRetryAttempts { get; set; } = 5; // ����֪ͨʶ�����Դ���
    public string[] SuccessKeywords { get; set; } = { "����ɹ�", "�۸񲨶�", "����", "�ɵ��", "��ȡ�ɹ�", "�ֿ�" }; // ʶ��ɹ��Ĺؼ���
    public string[] FailureKeywords { get; set; } = { "����ʧ��", "��ͼ۸����" }; // ʶ��ʧ�ܵĹؼ���

    // --- UI Element Coordinates ---
    // Point(X, Y), Size(Width, Height)
    /// <summary>
    /// �洢Ӧ�ó���������������á�
    /// ��Ҫ��ʾ���û������ʹ�ñ�����������ø���ȫ�����Ρ�
    /// �û�����ȷ����ʹ�÷�ʽ�����κ�Ŀ��Ӧ�ó���ķ����������ط��ɷ��档
    /// ��Դ�˴����Ϊ����������ѧϰĿ�ġ�
    /// </summary>
    public Point PriceListPos { get; set; } = Point.Empty;
    public Size PriceListSize { get; set; } = Size.Empty;
    public Point NotificationPos { get; set; } = Point.Empty;
    public Size NotificationSize { get; set; } = Size.Empty;
    public Point ItemClickPos { get; set; } = Point.Empty;
    public Point BackButtonPos { get; set; } = Point.Empty;
    public Point BuyButtonPos { get; set; } = Point.Empty;
    public Point MinQtyButtonPos { get; set; } = Point.Empty;
    public Point SmallQtyButtonPos { get; set; } = Point.Empty;
    public Point MediumQtyButtonPos { get; set; } = Point.Empty;
    public Point MaxQtyButtonPos { get; set; } = Point.Empty;

    // --- Delays (in milliseconds) ---
    public int DelayShortMin { get; set; } = 15;
    public int DelayShortMax { get; set; } = 30;
    public int DelayMediumMin { get; set; } = 20;
    public int DelayMediumMax { get; set; } = 40;
    public int DelayLongMin { get; set; } = 400;
    public int DelayLongMax { get; set; } = 430;
    public int DelayClickMin { get; set; } = 15;
    public int DelayClickMax { get; set; } = 30;
    public int DelayOcrCheck { get; set; } = 30; // OCR ����ȷ�ϼ��
    public int DelayRetryWaitMin { get; set; } = 30; // ����ǰ�ĵȴ�ʱ������
    public int DelayRetryWaitMax { get; set; } = 50;// ����ǰ�ĵȴ�ʱ������

    // --- Timer Settings ---
    public bool IsTimerEnabled { get; set; } = false; // �Ƿ����ö�ʱ��
    public string StartTimeString { get; set; } = "08:00:00"; // ��ʱ��ʼʱ��
    public string StopTimeString { get; set; } = "23:00:00"; // ��ʱ����ʱ��

    // --- Window Adjuster Settings ---
    public int TargetWindowWidth { get; set; } = 1920;
    public int TargetWindowHeight { get; set; } = 1080;
    public int TargetWindowX { get; set; } = 0;
    public int TargetWindowY { get; set; } = 0;

    // --- External Dependencies ---
    public string LogitechDllPath { get; set; } = "logitech.driver.dll"; // �޼����� DLL ·�� (��Ի����)
    public Keys StopKey { get; set; } = Keys.F12; // ֹͣ����Ŀ�ݼ�

    // --- Logitech Mouse Movement Simulation ---
    public double MoveSpeedFactor { get; set; } = 0.01; // �ƶ��ٶ����� (Ӱ���ƶ�ʱ��)
    public int RandomRangeNear { get; set; } = 3;      // Ŀ��㸽�������Χ (+/-)
    public int RandomRangeShake { get; set; } = 2;     // �ƶ��󶶶������Χ (+/-)
    public int MaxMoveTimeMs { get; set; } = 1;       // ���� moveR ������ȴ�ʱ��

    // --- Utility ---
    /// <summary>
    /// ��ȡ�����ļ�������·����
    /// ���洢�� %AppData%\AUT0RICH_Lite\config.json
    /// </summary>
    [JsonIgnore] // ��ֹ������Ա����л��� JSON �ļ���
    public static string ConfigFilePath
    {
        get
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // ʹ�� Assembly ���ƻ�����Ψһ��ʶ����Ϊ�ļ���������
            // string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "AUT0RICH_Lite";
            string appName = "AUT0RICH_Lite"; // Keep simple for now
            string appFolder = Path.Combine(appDataPath, appName);
            Directory.CreateDirectory(appFolder); // ȷ���ļ��д���
            return Path.Combine(appFolder, "config.json");
        }
    }
}