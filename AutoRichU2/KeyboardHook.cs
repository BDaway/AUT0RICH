using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms; // Needed for Keys enum and Action
using System.ComponentModel; // 用于 Win32Exception

/// <summary>
/// 提供全局键盘钩子功能，用于监听特定按键（如停止热键）。
/// </summary>
public static class KeyboardHook
{
    private const int WH_KEYBOARD_LL = 13; // 低级键盘钩子类型
    private const int WM_KEYDOWN = 0x0100; // 按键按下消息
    private static LowLevelKeyboardProc _proc = HookCallback; // 钩子回调函数委托实例
    private static IntPtr _hookID = IntPtr.Zero; // 钩子句柄

    /// <summary>
    /// 获取或设置用于触发 StopAutoBuyer 事件的热键。
    /// 默认为 F12。应在 Initialize() 之前设置。
    /// </summary>
    public static Keys StopKey { get; set; } = Keys.F12;

    /// <summary>
    /// 当设置的 StopKey 被按下时触发此事件。
    /// </summary>
    public static event Action StopAutoBuyer;

    // --- P/Invoke 声明 ---
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // 低级键盘处理委托
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// 初始化并安装全局键盘钩子。
    /// </summary>
    /// <exception cref="Win32Exception">当 SetWindowsHookEx 失败时抛出。</exception>
    /// <exception cref="InvalidOperationException">如果钩子已经被初始化。</exception>
    public static void Initialize()
    {
        if (_hookID != IntPtr.Zero)
        {
            throw new InvalidOperationException("键盘钩子已经被初始化。");
        }

        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            IntPtr hMod = GetModuleHandle(curModule.ModuleName);
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
            if (_hookID == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"键盘钩子安装失败 (错误码: {errorCode})。请检查程序权限或是否有其他冲突程序。");
            }
            Console.WriteLine("键盘钩子安装成功。");
        }
    }

    /// <summary>
    /// 清理并卸载键盘钩子（如果已安装）。
    /// </summary>
    public static void Cleanup()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            Console.WriteLine("键盘钩子已卸载。");
        }
    }

    /// <summary>
    /// 键盘钩子回调函数。
    /// </summary>
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if ((Keys)vkCode == StopKey)
            {
                Console.WriteLine($"检测到停止键 {StopKey}，触发 StopAutoBuyer 事件...");
                try
                {
                    StopAutoBuyer?.Invoke(); // 触发事件
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误：处理 StopAutoBuyer 事件时发生异常: {ex.Message}");
                }
            }
        }
        // 必须调用 CallNextHookEx 将消息传递给下一个钩子
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
}