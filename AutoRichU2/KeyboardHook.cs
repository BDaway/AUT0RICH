using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms; // Needed for Keys enum and Action
using System.ComponentModel; // ���� Win32Exception

/// <summary>
/// �ṩȫ�ּ��̹��ӹ��ܣ����ڼ����ض���������ֹͣ�ȼ�����
/// </summary>
public static class KeyboardHook
{
    private const int WH_KEYBOARD_LL = 13; // �ͼ����̹�������
    private const int WM_KEYDOWN = 0x0100; // ����������Ϣ
    private static LowLevelKeyboardProc _proc = HookCallback; // ���ӻص�����ί��ʵ��
    private static IntPtr _hookID = IntPtr.Zero; // ���Ӿ��

    /// <summary>
    /// ��ȡ���������ڴ��� StopAutoBuyer �¼����ȼ���
    /// Ĭ��Ϊ F12��Ӧ�� Initialize() ֮ǰ���á�
    /// </summary>
    public static Keys StopKey { get; set; } = Keys.F12;

    /// <summary>
    /// �����õ� StopKey ������ʱ�������¼���
    /// </summary>
    public static event Action StopAutoBuyer;

    // --- P/Invoke ���� ---
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // �ͼ����̴���ί��
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// ��ʼ������װȫ�ּ��̹��ӡ�
    /// </summary>
    /// <exception cref="Win32Exception">�� SetWindowsHookEx ʧ��ʱ�׳���</exception>
    /// <exception cref="InvalidOperationException">��������Ѿ�����ʼ����</exception>
    public static void Initialize()
    {
        if (_hookID != IntPtr.Zero)
        {
            throw new InvalidOperationException("���̹����Ѿ�����ʼ����");
        }

        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            IntPtr hMod = GetModuleHandle(curModule.ModuleName);
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
            if (_hookID == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"���̹��Ӱ�װʧ�� (������: {errorCode})���������Ȩ�޻��Ƿ���������ͻ����");
            }
            Console.WriteLine("���̹��Ӱ�װ�ɹ���");
        }
    }

    /// <summary>
    /// ����ж�ؼ��̹��ӣ�����Ѱ�װ����
    /// </summary>
    public static void Cleanup()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            Console.WriteLine("���̹�����ж�ء�");
        }
    }

    /// <summary>
    /// ���̹��ӻص�������
    /// </summary>
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if ((Keys)vkCode == StopKey)
            {
                Console.WriteLine($"��⵽ֹͣ�� {StopKey}������ StopAutoBuyer �¼�...");
                try
                {
                    StopAutoBuyer?.Invoke(); // �����¼�
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"���󣺴��� StopAutoBuyer �¼�ʱ�����쳣: {ex.Message}");
                }
            }
        }
        // ������� CallNextHookEx ����Ϣ���ݸ���һ������
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
}