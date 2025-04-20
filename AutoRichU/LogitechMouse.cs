using System;
using System.Drawing; // 需要引用 System.Drawing.Common NuGet 包（.NET Core/5+）或添加项目引用
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for Cursor.Position
using System.IO; // 用于路径处理
using System.ComponentModel; // 用于 Win32Exception

/// <summary>
/// 提供与罗技鼠标驱动交互的功能（移动、点击）。
/// 需要 logitech.driver.dll 文件和已安装的罗技驱动 (G HUB 或 LGS)。
/// </summary>
public static class LogitechMouse
{
    // --- 配置 (由外部设置，例如从 AppSettings) ---
    public static string DllPath { get; set; } = "logitech.driver.dll"; // DLL 路径
    public static double MoveSpeedFactor { get; set; } = 0.01;   // 移动速度因子
    public static int RandomRangeNear { get; set; } = 3;       // 目标点附近随机范围
    public static int RandomRangeShake { get; set; } = 2;      // 抖动随机范围
    public static int MaxMoveTimeMs { get; set; } = 1;        // 最大移动等待时间

    // --- P/Invoke ---
    // 显式加载 DLL 比直接 DllImport 更能控制路径和错误处理
    // 但这里为了简化，我们假设 LoadLibrary 后，后续 P/Invoke 能找到函数
    // 注意: 如果 DLL 确实无法通过这种方式调用函数，需要使用 GetProcAddress 获取函数指针并手动调用
    [DllImport("logitech.driver.dll", EntryPoint = "device_open", CallingConvention = CallingConvention.Cdecl)]
    private static extern int device_open();

    [DllImport("logitech.driver.dll", EntryPoint = "mouse_down", CallingConvention = CallingConvention.Cdecl)]
    private static extern void mouse_down(int code); // code: 1=左键, 2=右键, 3=中键

    [DllImport("logitech.driver.dll", EntryPoint = "mouse_up", CallingConvention = CallingConvention.Cdecl)]
    private static extern void mouse_up(int code);

    [DllImport("logitech.driver.dll", EntryPoint = "moveR", CallingConvention = CallingConvention.Cdecl)]
    private static extern void moveR(int x, int y, bool absolute); // 驱动函数通常是相对移动 (absolute=false)

    // --- Kernel32 for DLL loading ---
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    // --- State ---
    private static volatile bool initialized = false; // 标记是否初始化成功
    private static IntPtr dllHandle = IntPtr.Zero; // DLL 句柄
    private static readonly Random random = new Random(); // Use readonly for static Random

    /// <summary>
    /// 获取一个值，该值指示罗技驱动是否已成功初始化。
    /// </summary>
    public static bool IsInitialized => initialized;

    /// <summary>
    /// 初始化罗技驱动连接。
    /// </summary>
    /// <exception cref="FileNotFoundException">如果找不到指定的 DLL 文件。</exception>
    /// <exception cref="Win32Exception">如果加载 DLL 失败。</exception>
    /// <exception cref="Exception">如果驱动加载后设备打开失败。</exception>
    /// <exception cref="InvalidOperationException">如果驱动已经被初始化。</exception>
    public static void Initialize()
    {
        if (initialized)
        {
            throw new InvalidOperationException("罗技驱动已被初始化。");
        }

        string absoluteDllPath = Path.IsPathRooted(DllPath) ? DllPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllPath);
        Console.WriteLine($"尝试加载罗技驱动 DLL: {absoluteDllPath}");

        if (!File.Exists(absoluteDllPath))
        {
            throw new FileNotFoundException($"罗技驱动 DLL 未找到: {absoluteDllPath}。请将其放置在程序目录下或提供正确路径。", absoluteDllPath);
        }

        dllHandle = LoadLibrary(absoluteDllPath);
        if (dllHandle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"加载罗技驱动 DLL 失败 (错误码: {errorCode})。检查 DLL 是否有效或被占用。路径: {absoluteDllPath}");
        }
        Console.WriteLine("罗技驱动 DLL 加载成功。");
        Console.WriteLine("感谢罗技链接库作者！！！下面是链接库作者水印，非此脚本。");
        try
        {
            // 调用 device_open 前确保 LoadLibrary 成功
            int openResult = device_open();
            if (openResult == 1)
            {
                initialized = true;
                Console.WriteLine("罗技设备打开成功，驱动初始化完成。");
            }
            else
            {
                FreeLibrary(dllHandle);
                dllHandle = IntPtr.Zero;
                throw new Exception($"罗技设备打开失败 (device_open 返回 {openResult})。请确保 G HUB 或 LGS 驱动已正确安装、运行并且与鼠标连接正常。");
            }
        }
        catch (Exception ex)
        {
            if (dllHandle != IntPtr.Zero)
            {
                FreeLibrary(dllHandle);
                dllHandle = IntPtr.Zero;
            }
            initialized = false;
            Console.WriteLine($"初始化罗技驱动时发生错误: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 清理资源，释放已加载的 DLL。
    /// </summary>
    public static void Cleanup()
    {
        if (dllHandle != IntPtr.Zero)
        {
            FreeLibrary(dllHandle);
            dllHandle = IntPtr.Zero;
            initialized = false;
            Console.WriteLine("罗技驱动已卸载。");
        }
    }

    /// <summary>
    /// 执行鼠标点击操作（按下并抬起）。
    /// </summary>
    /// <param name="code">鼠标按键代码 (1=左, 2=右, 3=中)。</param>
    public static void Click(int code = 1)
    {
        if (!EnsureInitialized()) return;

        try
        {
            mouse_down(code);
            Thread.Sleep(random.Next(20, 50));
            mouse_up(code);
        }
        catch (SEHException sehEx)
        {
            Console.WriteLine($"罗技鼠标点击时发生 SEH 错误: {sehEx.Message}。驱动可能已失效。");
            initialized = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"罗技鼠标点击时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 将鼠标移动到指定屏幕坐标。包含模拟人类移动的随机性。
    /// </summary>
    /// <param name="x">目标 X 坐标。</param>
    /// <param name="y">目标 Y 坐标。</param>
    /// <param name="preciseEnd">是否需要在移动结束时精确到达目标点。</param>
    public static void MoveTo(int x, int y, bool preciseEnd = false)
    {
        if (!EnsureInitialized()) return;

        try
        {
            Point currentPos = Cursor.Position;
            SimulateHumanMove(currentPos.X, currentPos.Y, x, y, preciseEnd);
        }
        catch (SEHException sehEx)
        {
            Console.WriteLine($"罗技鼠标移动时发生 SEH 错误: {sehEx.Message}。驱动可能已失效。");
            initialized = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"罗技鼠标移动时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 模拟人类移动的核心逻辑，使用相对移动。
    /// </summary>
    private static void SimulateHumanMove(int startX, int startY, int endX, int endY, bool preciseEnd)
    {
        Point currentPos = Cursor.Position;
        int actualStartX = currentPos.X;
        int actualStartY = currentPos.Y;

        double distance = Math.Sqrt(Math.Pow(endX - actualStartX, 2) + Math.Pow(endY - actualStartY, 2));
        if (distance < 1) return;

        int moveTime = (int)(distance * MoveSpeedFactor);
        moveTime = Math.Max(0, Math.Min(moveTime, MaxMoveTimeMs));

        int targetX = endX;
        int targetY = endY;
        if (!preciseEnd)
        {
            targetX += random.Next(-RandomRangeNear, RandomRangeNear + 1);
            targetY += random.Next(-RandomRangeNear, RandomRangeNear + 1);
        }

        int deltaX = targetX - actualStartX;
        int deltaY = targetY - actualStartY;

        moveR(deltaX, deltaY, false);
        if (moveTime > 0) Thread.Sleep(moveTime);

        if (!preciseEnd)
        {
            PerformRandomShake();
            Thread.Sleep(random.Next(10, 30));
            PerformRandomShake();
        }

        if (preciseEnd)
        {
            currentPos = Cursor.Position;
            int finalDeltaX = endX - currentPos.X;
            int finalDeltaY = endY - currentPos.Y;
            if (finalDeltaX != 0 || finalDeltaY != 0)
            {
                moveR(finalDeltaX, finalDeltaY, false);
                Thread.Sleep(random.Next(5, 15));
            }
        }
    }

    /// <summary>
    /// 执行一次随机抖动。
    /// </summary>
    private static void PerformRandomShake()
    {
        int shakeX = random.Next(-RandomRangeShake, RandomRangeShake + 1);
        int shakeY = random.Next(-RandomRangeShake, RandomRangeShake + 1);
        if (shakeX != 0 || shakeY != 0)
        {
            moveR(shakeX, shakeY, false);
        }
    }

    /// <summary>
    /// 检查驱动是否初始化，如果未初始化则打印警告。
    /// </summary>
    /// <returns>如果已初始化返回 true，否则返回 false。</returns>
    private static bool EnsureInitialized()
    {
        if (!initialized)
        {
            Console.WriteLine("警告: 罗技驱动未初始化或初始化失败，无法执行鼠标操作。");
            return false;
        }
        return true;
    }
}