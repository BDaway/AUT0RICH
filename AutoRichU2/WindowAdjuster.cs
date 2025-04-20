using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for MessageBox, DialogResult, etc.
using System.Text; // For StringBuilder
using System.ComponentModel; // For Win32Exception

/// <summary>
/// 提供调整目标窗口大小、位置和样式（如无边框）的功能。
/// </summary>
public class WindowAdjuster
{
    // --- 配置 (从 AppSettings 读取) ---
    public int TargetWidth { get; set; } = 1920;
    public int TargetHeight { get; set; } = 1080;
    public int TargetX { get; set; } = 0;
    public int TargetY { get; set; } = 0;

    // --- 用于恢复窗口状态 ---
    private IntPtr originalHWnd = IntPtr.Zero; // 保存上次调整的窗口句柄
    private RECT originalRect;                // 保存原始窗口矩形
    private int originalStyle;                 // 保存原始窗口样式
    private bool originalStateSaved = false;   // 标记是否有状态被保存

    // --- Win32 API Structures ---
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    // --- Win32 API Imports ---
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // --- Win32 Constants ---
    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000; private const int WS_BORDER = 0x00800000;
    private const int WS_DLGFRAME = 0x00400000; private const int WS_THICKFRAME = 0x00040000;
    private const uint SWP_FRAMECHANGED = 0x0020; private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001; private const uint SWP_NOZORDER = 0x0004;
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;

    /// <summary>
    /// 调整当前获得焦点的窗口，移除边框并设置为配置的目标尺寸和位置。
    /// </summary>
    public void AdjustFocusedWindow()
    {
        DialogResult result = MessageBox.Show(
            $"请在 5 秒内点击目标游戏窗口，使其获得焦点。\n\n程序将尝试将其调整为无边框 {TargetWidth}x{TargetHeight} @ ({TargetX},{TargetY})。\n\n(可通过'恢复窗口'按钮还原)",
            "准备调整窗口", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

        if (result != DialogResult.OK) { Console.WriteLine("用户取消了窗口调整操作。"); return; }

        Console.WriteLine("等待用户点击目标窗口...");
        Thread.Sleep(5000);

        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
        {
            MessageBox.Show("未能获取到前台窗口句柄！请确保已点击目标窗口。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        AdjustWindowInternal(hWnd);
    }

    /// <summary>
    /// 内部方法，执行窗口调整的核心逻辑。
    /// </summary>
    private void AdjustWindowInternal(IntPtr hWnd)
    {
        StringBuilder titleBuilder = new StringBuilder(256);
        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
        string windowTitle = titleBuilder.Length > 0 ? titleBuilder.ToString() : "[无标题窗口]";
        Console.WriteLine($"开始调整窗口: '{windowTitle}' (句柄: {hWnd})");

        try // Wrap the adjustment process in try-catch
        {
            // --- 1. Save Original State ---
            if (!GetWindowRect(hWnd, out originalRect))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"无法获取窗口 '{windowTitle}' 的矩形信息！");
            originalStyle = GetWindowLong(hWnd, GWL_STYLE);
            if (originalStyle == 0 && Marshal.GetLastWin32Error() != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"无法获取窗口 '{windowTitle}' 的样式信息！");
            originalHWnd = hWnd;
            originalStateSaved = true;
            Console.WriteLine($"已保存原始状态: Rect({originalRect.Left},{originalRect.Top}-{originalRect.Right},{originalRect.Bottom}), Style(0x{originalStyle:X})");

            // --- 2. Remove Border/Caption ---
            Console.WriteLine("设置无边框样式...");
            int newStyle = originalStyle & ~(WS_CAPTION | WS_BORDER | WS_DLGFRAME | WS_THICKFRAME);
            SetWindowLong(hWnd, GWL_STYLE, newStyle); // Returns old style, check for error? No, just proceed.

            // --- 3. Move and Resize ---
            Console.WriteLine($"移动窗口到 ({TargetX},{TargetY}) 并调整大小为 {TargetWidth}x{TargetHeight}...");
            if (!MoveWindow(hWnd, TargetX, TargetY, TargetWidth, TargetHeight, true))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"移动和调整窗口 '{windowTitle}' 大小失败！");

            // --- 4. Force Style Update ---
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);

            Console.WriteLine("窗口调整完成。");
            MessageBox.Show($"窗口 '{windowTitle}' 已成功调整！\n按 '恢复窗口' 可还原。", "调整成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Win32Exception w32Ex)
        {
            MessageBox.Show($"调整窗口时出错: {w32Ex.Message} (错误码: {w32Ex.NativeErrorCode})", "调整错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Attempt to restore original style if adjustment failed partially
            if (originalStateSaved && hWnd == originalHWnd) // Check if we have state and it's the same window
            {
                Console.WriteLine("尝试恢复原始样式...");
                SetWindowLong(hWnd, GWL_STYLE, originalStyle);
                originalStateSaved = false; // Clear saved state as restoration was attempted
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"调整窗口时发生意外错误: {ex.Message}", "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    /// <summary>
    /// 恢复上次调整的窗口到其原始的大小、位置和样式。
    /// </summary>
    public void RestoreOriginalWindow()
    {
        if (!originalStateSaved || originalHWnd == IntPtr.Zero)
        {
            MessageBox.Show("没有保存的窗口状态可供恢复，请先调整一个窗口。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Console.WriteLine($"开始恢复窗口 (句柄: {originalHWnd}) 到原始状态...");

        try
        {
            // Check if handle is still valid
            if (GetWindowLong(originalHWnd, GWL_STYLE) == 0 && Marshal.GetLastWin32Error() != 0)
            {
                MessageBox.Show("原始窗口句柄已失效 (窗口可能已关闭)，无法恢复。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                originalStateSaved = false; return;
            }

            // --- 1. Restore Style ---
            Console.WriteLine($"恢复样式: 0x{originalStyle:X}");
            SetWindowLong(originalHWnd, GWL_STYLE, originalStyle);

            // --- 2. Restore Size and Position ---
            int width = originalRect.Right - originalRect.Left;
            int height = originalRect.Bottom - originalRect.Top;
            Console.WriteLine($"恢复位置: ({originalRect.Left},{originalRect.Top}), 尺寸: {width}x{height}");
            if (!MoveWindow(originalHWnd, originalRect.Left, originalRect.Top, width, height, true))
            {
                // Log error but proceed, style might be restored
                Console.WriteLine($"警告: 恢复窗口位置和大小失败！(错误码: {Marshal.GetLastWin32Error()})");
                MessageBox.Show($"恢复窗口位置和大小失败！(错误码: {Marshal.GetLastWin32Error()}) 但样式可能已恢复。", "恢复警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // --- 3. Force Update ---
            SetWindowPos(originalHWnd, HWND_TOP, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);

            Console.WriteLine("窗口恢复操作完成。");
            MessageBox.Show("窗口已尝试恢复到原始状态。", "恢复完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"恢复窗口时发生错误: {ex.Message}", "恢复错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Clear saved state after attempting restoration
            originalStateSaved = false;
            originalHWnd = IntPtr.Zero;
        }
    }
}