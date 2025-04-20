using System;
using System.Drawing; // ��Ҫ���� System.Drawing.Common NuGet ����.NET Core/5+���������Ŀ����
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for Cursor.Position
using System.IO; // ����·������
using System.ComponentModel; // ���� Win32Exception

/// <summary>
/// �ṩ���޼�������������Ĺ��ܣ��ƶ����������
/// ��Ҫ logitech.driver.dll �ļ����Ѱ�װ���޼����� (G HUB �� LGS)��
/// </summary>
public static class LogitechMouse
{
    // --- ���� (���ⲿ���ã������ AppSettings) ---
    public static string DllPath { get; set; } = "logitech.driver.dll"; // DLL ·��
    public static double MoveSpeedFactor { get; set; } = 0.01;   // �ƶ��ٶ�����
    public static int RandomRangeNear { get; set; } = 3;       // Ŀ��㸽�������Χ
    public static int RandomRangeShake { get; set; } = 2;      // ���������Χ
    public static int MaxMoveTimeMs { get; set; } = 1;        // ����ƶ��ȴ�ʱ��

    // --- P/Invoke ---
    // ��ʽ���� DLL ��ֱ�� DllImport ���ܿ���·���ʹ�����
    // ������Ϊ�˼򻯣����Ǽ��� LoadLibrary �󣬺��� P/Invoke ���ҵ�����
    // ע��: ��� DLL ȷʵ�޷�ͨ�����ַ�ʽ���ú�������Ҫʹ�� GetProcAddress ��ȡ����ָ�벢�ֶ�����
    [DllImport("logitech.driver.dll", EntryPoint = "device_open", CallingConvention = CallingConvention.Cdecl)]
    private static extern int device_open();

    [DllImport("logitech.driver.dll", EntryPoint = "mouse_down", CallingConvention = CallingConvention.Cdecl)]
    private static extern void mouse_down(int code); // code: 1=���, 2=�Ҽ�, 3=�м�

    [DllImport("logitech.driver.dll", EntryPoint = "mouse_up", CallingConvention = CallingConvention.Cdecl)]
    private static extern void mouse_up(int code);

    [DllImport("logitech.driver.dll", EntryPoint = "moveR", CallingConvention = CallingConvention.Cdecl)]
    private static extern void moveR(int x, int y, bool absolute); // ��������ͨ��������ƶ� (absolute=false)

    // --- Kernel32 for DLL loading ---
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    // --- State ---
    private static volatile bool initialized = false; // ����Ƿ��ʼ���ɹ�
    private static IntPtr dllHandle = IntPtr.Zero; // DLL ���
    private static readonly Random random = new Random(); // Use readonly for static Random

    /// <summary>
    /// ��ȡһ��ֵ����ֵָʾ�޼������Ƿ��ѳɹ���ʼ����
    /// </summary>
    public static bool IsInitialized => initialized;

    /// <summary>
    /// ��ʼ���޼��������ӡ�
    /// </summary>
    /// <exception cref="FileNotFoundException">����Ҳ���ָ���� DLL �ļ���</exception>
    /// <exception cref="Win32Exception">������� DLL ʧ�ܡ�</exception>
    /// <exception cref="Exception">����������غ��豸��ʧ�ܡ�</exception>
    /// <exception cref="InvalidOperationException">��������Ѿ�����ʼ����</exception>
    public static void Initialize()
    {
        if (initialized)
        {
            throw new InvalidOperationException("�޼������ѱ���ʼ����");
        }

        string absoluteDllPath = Path.IsPathRooted(DllPath) ? DllPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllPath);
        Console.WriteLine($"���Լ����޼����� DLL: {absoluteDllPath}");

        if (!File.Exists(absoluteDllPath))
        {
            throw new FileNotFoundException($"�޼����� DLL δ�ҵ�: {absoluteDllPath}���뽫������ڳ���Ŀ¼�»��ṩ��ȷ·����", absoluteDllPath);
        }

        dllHandle = LoadLibrary(absoluteDllPath);
        if (dllHandle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"�����޼����� DLL ʧ�� (������: {errorCode})����� DLL �Ƿ���Ч��ռ�á�·��: {absoluteDllPath}");
        }
        Console.WriteLine("�޼����� DLL ���سɹ���");
        Console.WriteLine("��л�޼����ӿ����ߣ��������������ӿ�����ˮӡ���Ǵ˽ű���");
        try
        {
            // ���� device_open ǰȷ�� LoadLibrary �ɹ�
            int openResult = device_open();
            if (openResult == 1)
            {
                initialized = true;
                Console.WriteLine("�޼��豸�򿪳ɹ���������ʼ����ɡ�");
            }
            else
            {
                FreeLibrary(dllHandle);
                dllHandle = IntPtr.Zero;
                throw new Exception($"�޼��豸��ʧ�� (device_open ���� {openResult})����ȷ�� G HUB �� LGS ��������ȷ��װ�����в������������������");
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
            Console.WriteLine($"��ʼ���޼�����ʱ��������: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ������Դ���ͷ��Ѽ��ص� DLL��
    /// </summary>
    public static void Cleanup()
    {
        if (dllHandle != IntPtr.Zero)
        {
            FreeLibrary(dllHandle);
            dllHandle = IntPtr.Zero;
            initialized = false;
            Console.WriteLine("�޼�������ж�ء�");
        }
    }

    /// <summary>
    /// ִ����������������²�̧�𣩡�
    /// </summary>
    /// <param name="code">��갴������ (1=��, 2=��, 3=��)��</param>
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
            Console.WriteLine($"�޼������ʱ���� SEH ����: {sehEx.Message}������������ʧЧ��");
            initialized = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"�޼������ʱ��������: {ex.Message}");
        }
    }

    /// <summary>
    /// ������ƶ���ָ����Ļ���ꡣ����ģ�������ƶ�������ԡ�
    /// </summary>
    /// <param name="x">Ŀ�� X ���ꡣ</param>
    /// <param name="y">Ŀ�� Y ���ꡣ</param>
    /// <param name="preciseEnd">�Ƿ���Ҫ���ƶ�����ʱ��ȷ����Ŀ��㡣</param>
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
            Console.WriteLine($"�޼�����ƶ�ʱ���� SEH ����: {sehEx.Message}������������ʧЧ��");
            initialized = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"�޼�����ƶ�ʱ��������: {ex.Message}");
        }
    }

    /// <summary>
    /// ģ�������ƶ��ĺ����߼���ʹ������ƶ���
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
    /// ִ��һ�����������
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
    /// ��������Ƿ��ʼ�������δ��ʼ�����ӡ���档
    /// </summary>
    /// <returns>����ѳ�ʼ������ true�����򷵻� false��</returns>
    private static bool EnsureInitialized()
    {
        if (!initialized)
        {
            Console.WriteLine("����: �޼�����δ��ʼ�����ʼ��ʧ�ܣ��޷�ִ����������");
            return false;
        }
        return true;
    }
}