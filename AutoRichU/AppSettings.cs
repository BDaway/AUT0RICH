using System;
using System.Drawing; // 需要引用 System.Drawing.Common NuGet 包（.NET Core/5+）或添加项目引用
using System.IO;
using System.Text.Json.Serialization; // 用于忽略特定属性的序列化
using System.Windows.Forms; // Needed for Keys enum

/// <summary>
/// 存储应用程序的所有可配置设置。
/// </summary>
public class AppSettings
{
    // --- AutoBuyer Settings ---
    public int PriceThreshold { get; set; } = 10; // 最高购买单价阈值
    public int PurchaseQuantity { get; set; } = 200; // 默认购买数量 (1, 25, 100, 200)
    public int StopAfterPurchases { get; set; } = 0; // 购买多少次后停止 (0 = 无限)
    public string OcrApiUrl { get; set; } = "http://localhost:8000/recognize"; // OCR API 地址
    public int OcrTimeoutSeconds { get; set; } = 5; // OCR API 超时时间（秒）
    public int OcrRetryAttempts { get; set; } = 5; // OCR 识别重试次数
    public int NotificationRetryAttempts { get; set; } = 5; // 购买通知识别重试次数
    public string[] SuccessKeywords { get; set; } = { "购买成功", "价格波动", "交易", "可点击", "获取成功", "仓库" }; // 识别成功的关键字
    public string[] FailureKeywords { get; set; } = { "购买失败", "最低价格道具" }; // 识别失败的关键字

    // --- UI Element Coordinates ---
    // Point(X, Y), Size(Width, Height)
    /// <summary>
    /// 存储应用程序的所有配置设置。
    /// 重要提示：用户对如何使用本软件及其配置负有全部责任。
    /// 用户必须确保其使用方式符合任何目标应用程序的服务条款和相关法律法规。
    /// 开源此代码仅为技术交流和学习目的。
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
    public int DelayOcrCheck { get; set; } = 30; // OCR 二次确认间隔
    public int DelayRetryWaitMin { get; set; } = 30; // 重试前的等待时间下限
    public int DelayRetryWaitMax { get; set; } = 50;// 重试前的等待时间上限

    // --- Timer Settings ---
    public bool IsTimerEnabled { get; set; } = false; // 是否启用定时器
    public string StartTimeString { get; set; } = "08:00:00"; // 定时开始时间
    public string StopTimeString { get; set; } = "23:00:00"; // 定时结束时间

    // --- Window Adjuster Settings ---
    public int TargetWindowWidth { get; set; } = 1920;
    public int TargetWindowHeight { get; set; } = 1080;
    public int TargetWindowX { get; set; } = 0;
    public int TargetWindowY { get; set; } = 0;

    // --- External Dependencies ---
    public string LogitechDllPath { get; set; } = "logitech.driver.dll"; // 罗技驱动 DLL 路径 (相对或绝对)
    public Keys StopKey { get; set; } = Keys.F12; // 停止购买的快捷键

    // --- Logitech Mouse Movement Simulation ---
    public double MoveSpeedFactor { get; set; } = 0.01; // 移动速度因子 (影响移动时间)
    public int RandomRangeNear { get; set; } = 3;      // 目标点附近随机范围 (+/-)
    public int RandomRangeShake { get; set; } = 2;     // 移动后抖动随机范围 (+/-)
    public int MaxMoveTimeMs { get; set; } = 1;       // 单次 moveR 后的最大等待时间

    // --- Utility ---
    /// <summary>
    /// 获取配置文件的完整路径。
    /// 将存储在 %AppData%\AUT0RICH_Lite\config.json
    /// </summary>
    [JsonIgnore] // 防止这个属性被序列化到 JSON 文件中
    public static string ConfigFilePath
    {
        get
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // 使用 Assembly 名称或其他唯一标识符作为文件夹名更佳
            // string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "AUT0RICH_Lite";
            string appName = "AUT0RICH_Lite"; // Keep simple for now
            string appFolder = Path.Combine(appDataPath, appName);
            Directory.CreateDirectory(appFolder); // 确保文件夹存在
            return Path.Combine(appFolder, "config.json");
        }
    }
}