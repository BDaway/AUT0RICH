// AppSettings.cs
using System;
using System.Drawing;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Forms;

/// <summary>
/// 存储应用程序的所有配置设置。
/// 重要提示：用户对如何使用本软件及其配置负有全部责任。
/// 用户必须确保其使用方式符合任何目标应用程序的服务条款和相关法律法规。
/// 开源此代码仅为技术交流和学习目的。
/// </summary>
public class AppSettings
{
    // --- 购买逻辑相关设置 ---
    /// <summary>低于或等于此价格才尝试购买</summary>
    public int PriceThreshold { get; set; } = 10;
    /// <summary>每次尝试购买的数量 (例如 1, 25, 100, 200)</summary>
    public int PurchaseQuantity { get; set; } = 200;
    /// <summary>购买成功多少次后自动停止 (0 = 无限)</summary>
    public int StopAfterPurchases { get; set; } = 0;

    // --- OCR (光学字符识别) 相关设置 ---
    /// <summary>用户本地运行的 OCR 服务器的地址 (需要用户自行部署和运行兼容的 OCR API 服务)</summary>
    public string OcrApiUrl { get; set; } = "http://localhost:8000/recognize";
    /// <summary>OCR 请求的超时时间（秒）</summary>
    public int OcrTimeoutSeconds { get; set; } = 5;
    /// <summary>价格或通知识别失败时的重试次数</summary>
    public int OcrRetryAttempts { get; set; } = 5;
    /// <summary>购买后检查通知的重试次数</summary>
    public int NotificationRetryAttempts { get; set; } = 5;
    /// <summary>
    /// !! 用户必须配置 !!
    /// 判断购买成功的关键字 (需要根据目标应用程序的提示语进行配置)
    /// </summary>
    public string[] SuccessKeywords { get; set; } = new string[0]; // 清空默认值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 判断购买失败的关键字 (需要根据目标应用程序的提示语进行配置)
    /// </summary>
    public string[] FailureKeywords { get; set; } = new string[0]; // 清空默认值

    // --- 屏幕坐标和区域大小设置 ---
    // !! 用户必须配置 !! 以下所有坐标和尺寸都需要用户根据其目标应用程序和屏幕分辨率自行校准和配置。
    // 可以通过截图工具或专门的坐标拾取工具获取。

    /// <summary>
    /// !! 用户必须配置 !!
    /// 价格列表（通常是第一个可购买物品的价格）的左上角屏幕坐标。
    /// </summary>
    public Point PriceListPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 价格识别区域的大小 (宽度, 高度)。
    /// </summary>
    public Size PriceListSize { get; set; } = Size.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 购买成功/失败通知区域的左上角屏幕坐标。
    /// </summary>
    public Point NotificationPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 通知识别区域的大小 (宽度, 高度)。
    /// </summary>
    public Size NotificationSize { get; set; } = Size.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 用于点击进入商品详情页面的屏幕坐标。
    /// </summary>
    public Point ItemClickPos { get; set; } = Point.Empty; // 设置为无效值
    // public Point BackButtonPos { get; set; } = Point.Empty; // 返回按钮坐标 (RefreshPrice 已改为按 ESC，此设置不再直接使用)
    /// <summary>
    /// !! 用户必须配置 !!
    /// "购买" 或类似确认按钮的屏幕坐标。
    /// </summary>
    public Point BuyButtonPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 购买数量 "1" (或最小数量) 按钮的屏幕坐标。
    /// </summary>
    public Point MinQtyButtonPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 购买数量 "25" (或类似的小数量) 按钮的屏幕坐标。
    /// </summary>
    public Point SmallQtyButtonPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 购买数量 "100" (或类似的中数量) 按钮的屏幕坐标。
    /// </summary>
    public Point MediumQtyButtonPos { get; set; } = Point.Empty; // 设置为无效值
    /// <summary>
    /// !! 用户必须配置 !!
    /// 购买数量 "200" (或类似的最大数量) 按钮的屏幕坐标。
    /// </summary>
    public Point MaxQtyButtonPos { get; set; } = Point.Empty; // 设置为无效值

    // --- 延迟和时序设置 (毫秒) ---
    // 这些是行为设置，可以保留合理的默认值，但用户仍可调整
    /// <summary>每次鼠标点击/键盘操作后的最小等待时间</summary>
    public int ActionDelayMin { get; set; } = 15;
    /// <summary>每次鼠标点击/键盘操作后的最大等待时间</summary>
    public int ActionDelayMax { get; set; } = 30;
    /// <summary>OCR 识别失败后，重试前的最小等待时间</summary>
    public int OcrRetryDelayMin { get; set; } = 15;
    /// <summary>OCR 识别失败后，重试前的最大等待时间</summary>
    public int OcrRetryDelayMax { get; set; } = 20;
    /// <summary>点击购买后，检查通知前的最小等待时间</summary>
    public int NotificationDelayMin { get; set; } = 500;
    /// <summary>点击购买后，检查通知前的最大等待时间</summary>
    public int NotificationDelayMax { get; set; } = 510;
    /// <summary>价格二次确认读取之间的最小延迟</summary>
    public int OcrCheckDelayMin { get; set; } = 5;
    /// <summary>价格二次确认读取之间的最大延迟</summary>
    public int OcrCheckDelayMax { get; set; } = 10;

    // --- 定时器设置 ---
    /// <summary>是否启用定时启动/停止功能</summary>
    public bool IsTimerEnabled { get; set; } = false;
    /// <summary>定时启动时间 (HH:mm:ss)</summary>
    public string StartTimeString { get; set; } = "08:00:00";
    /// <summary>定时停止时间 (HH:mm:ss)</summary>
    public string StopTimeString { get; set; } = "23:00:00";

    // --- 窗口调整设置 (WindowAdjuster 使用) ---
    /// <summary>用于窗口调整功能的目标窗口宽度 (用户可修改)</summary>
    public int TargetWindowWidth { get; set; } = 1920;
    /// <summary>用于窗口调整功能的目标窗口高度 (用户可修改)</summary>
    public int TargetWindowHeight { get; set; } = 1080;
    /// <summary>用于窗口调整功能的目标窗口左上角 X 坐标 (用户可修改)</summary>
    public int TargetWindowX { get; set; } = 0;
    /// <summary>用于窗口调整功能的目标窗口左上角 Y 坐标 (用户可修改)</summary>
    public int TargetWindowY { get; set; } = 0;

    // --- 鼠标模拟随机性设置 (MouseSimulator 使用) ---
    /// <summary>鼠标移动目标点附近的随机偏移范围 (+/-)</summary>
    public int RandomRangeNear { get; set; } = 3;
    /// <summary>鼠标移动过程中的随机抖动范围 (+/-)</summary>
    public int RandomRangeShake { get; set; } = 2;

    // --- 热键设置 (KeyboardHook 使用) ---
    /// <summary>用于停止 AutoBuyer 的全局热键 (用户可修改)</summary>
    public Keys StopKey { get; set; } = Keys.F12;

    /// <summary>
    /// 获取配置文件的完整路径。
    /// 配置文件存储在 %AppData%\[AppName]\config.json
    /// [AppName] 在此定义为 "AUT0RICH_Lite2"
    /// </summary>
    [JsonIgnore] // 这个属性不需要保存到 JSON 文件中
    public static string ConfigFilePath
    {
        get
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // !! 如果更改此名称，用户现有的配置文件将不会被找到 !!
            string appName = "AUT0RICH_Lite2";
            string appFolder = Path.Combine(appDataPath, appName);
            try // 增加目录创建的错误处理
            {
                Directory.CreateDirectory(appFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：无法创建配置文件夹 '{appFolder}': {ex.Message}");
                
            }
            return Path.Combine(appFolder, "config.json");
        }
    }
}