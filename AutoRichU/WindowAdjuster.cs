using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for MessageBox, DialogResult, etc.
using System.Text; // For StringBuilder
using System.ComponentModel; // For Win32Exception

/// <summary>
/// �ṩ����Ŀ�괰�ڴ�С��λ�ú���ʽ�����ޱ߿򣩵Ĺ��ܡ�
/// </summary>
public class WindowAdjuster
{
    // --- ���� (�� AppSettings ��ȡ) ---
    public int TargetWidth { get; set; } = 1920;
    public int TargetHeight { get; set; } = 1080;
    public int TargetX { get; set; } = 0;
    public int TargetY { get; set; } = 0;

    // --- ���ڻָ�����״̬ ---
    private IntPtr originalHWnd = IntPtr.Zero; // �����ϴε����Ĵ��ھ��
    private RECT originalRect;                // ����ԭʼ���ھ���
    private int originalStyle;                 // ����ԭʼ������ʽ
    private bool originalStateSaved = false;   // ����Ƿ���״̬������

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
    /// ������ǰ��ý���Ĵ��ڣ��Ƴ��߿�����Ϊ���õ�Ŀ��ߴ��λ�á�
    /// </summary>
    public void AdjustFocusedWindow()
    {
        DialogResult result = MessageBox.Show(
            $"���� 5 ���ڵ��Ŀ����Ϸ���ڣ�ʹ���ý��㡣\n\n���򽫳��Խ������Ϊ�ޱ߿� {TargetWidth}x{TargetHeight} @ ({TargetX},{TargetY})��\n\n(��ͨ��'�ָ�����'��ť��ԭ)",
            "׼����������", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

        if (result != DialogResult.OK) { Console.WriteLine("�û�ȡ���˴��ڵ���������"); return; }

        Console.WriteLine("�ȴ��û����Ŀ�괰��...");
        Thread.Sleep(5000);

        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
        {
            MessageBox.Show("δ�ܻ�ȡ��ǰ̨���ھ������ȷ���ѵ��Ŀ�괰�ڡ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        AdjustWindowInternal(hWnd);
    }

    /// <summary>
    /// �ڲ�������ִ�д��ڵ����ĺ����߼���
    /// </summary>
    private void AdjustWindowInternal(IntPtr hWnd)
    {
        StringBuilder titleBuilder = new StringBuilder(256);
        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
        string windowTitle = titleBuilder.Length > 0 ? titleBuilder.ToString() : "[�ޱ��ⴰ��]";
        Console.WriteLine($"��ʼ��������: '{windowTitle}' (���: {hWnd})");

        try // Wrap the adjustment process in try-catch
        {
            // --- 1. Save Original State ---
            if (!GetWindowRect(hWnd, out originalRect))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"�޷���ȡ���� '{windowTitle}' �ľ�����Ϣ��");
            originalStyle = GetWindowLong(hWnd, GWL_STYLE);
            if (originalStyle == 0 && Marshal.GetLastWin32Error() != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"�޷���ȡ���� '{windowTitle}' ����ʽ��Ϣ��");
            originalHWnd = hWnd;
            originalStateSaved = true;
            Console.WriteLine($"�ѱ���ԭʼ״̬: Rect({originalRect.Left},{originalRect.Top}-{originalRect.Right},{originalRect.Bottom}), Style(0x{originalStyle:X})");

            // --- 2. Remove Border/Caption ---
            Console.WriteLine("�����ޱ߿���ʽ...");
            int newStyle = originalStyle & ~(WS_CAPTION | WS_BORDER | WS_DLGFRAME | WS_THICKFRAME);
            SetWindowLong(hWnd, GWL_STYLE, newStyle); // Returns old style, check for error? No, just proceed.

            // --- 3. Move and Resize ---
            Console.WriteLine($"�ƶ����ڵ� ({TargetX},{TargetY}) ��������СΪ {TargetWidth}x{TargetHeight}...");
            if (!MoveWindow(hWnd, TargetX, TargetY, TargetWidth, TargetHeight, true))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"�ƶ��͵������� '{windowTitle}' ��Сʧ�ܣ�");

            // --- 4. Force Style Update ---
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);

            Console.WriteLine("���ڵ�����ɡ�");
            MessageBox.Show($"���� '{windowTitle}' �ѳɹ�������\n�� '�ָ�����' �ɻ�ԭ��", "�����ɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Win32Exception w32Ex)
        {
            MessageBox.Show($"��������ʱ����: {w32Ex.Message} (������: {w32Ex.NativeErrorCode})", "��������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Attempt to restore original style if adjustment failed partially
            if (originalStateSaved && hWnd == originalHWnd) // Check if we have state and it's the same window
            {
                Console.WriteLine("���Իָ�ԭʼ��ʽ...");
                SetWindowLong(hWnd, GWL_STYLE, originalStyle);
                originalStateSaved = false; // Clear saved state as restoration was attempted
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"��������ʱ�����������: {ex.Message}", "���ش���", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    /// <summary>
    /// �ָ��ϴε����Ĵ��ڵ���ԭʼ�Ĵ�С��λ�ú���ʽ��
    /// </summary>
    public void RestoreOriginalWindow()
    {
        if (!originalStateSaved || originalHWnd == IntPtr.Zero)
        {
            MessageBox.Show("û�б���Ĵ���״̬�ɹ��ָ������ȵ���һ�����ڡ�", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Console.WriteLine($"��ʼ�ָ����� (���: {originalHWnd}) ��ԭʼ״̬...");

        try
        {
            // Check if handle is still valid
            if (GetWindowLong(originalHWnd, GWL_STYLE) == 0 && Marshal.GetLastWin32Error() != 0)
            {
                MessageBox.Show("ԭʼ���ھ����ʧЧ (���ڿ����ѹر�)���޷��ָ���", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                originalStateSaved = false; return;
            }

            // --- 1. Restore Style ---
            Console.WriteLine($"�ָ���ʽ: 0x{originalStyle:X}");
            SetWindowLong(originalHWnd, GWL_STYLE, originalStyle);

            // --- 2. Restore Size and Position ---
            int width = originalRect.Right - originalRect.Left;
            int height = originalRect.Bottom - originalRect.Top;
            Console.WriteLine($"�ָ�λ��: ({originalRect.Left},{originalRect.Top}), �ߴ�: {width}x{height}");
            if (!MoveWindow(originalHWnd, originalRect.Left, originalRect.Top, width, height, true))
            {
                // Log error but proceed, style might be restored
                Console.WriteLine($"����: �ָ�����λ�úʹ�Сʧ�ܣ�(������: {Marshal.GetLastWin32Error()})");
                MessageBox.Show($"�ָ�����λ�úʹ�Сʧ�ܣ�(������: {Marshal.GetLastWin32Error()}) ����ʽ�����ѻָ���", "�ָ�����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // --- 3. Force Update ---
            SetWindowPos(originalHWnd, HWND_TOP, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);

            Console.WriteLine("���ڻָ�������ɡ�");
            MessageBox.Show("�����ѳ��Իָ���ԭʼ״̬��", "�ָ����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"�ָ�����ʱ��������: {ex.Message}", "�ָ�����", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Clear saved state after attempting restoration
            originalStateSaved = false;
            originalHWnd = IntPtr.Zero;
        }
    }
}