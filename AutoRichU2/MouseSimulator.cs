using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public static class MouseSimulator
{
    public static int RandomRangeNear { get; set; } = 3;
    public static int RandomRangeShake { get; set; } = 2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private static readonly Random random = new Random();

    // *** �޸ĺ�� Click ���� ***
    public static void Click()
    {
        // �������º�̧��� INPUT �ṹ
        INPUT mouseDownInput = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN }
        };

        INPUT mouseUpInput = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP }
        };

        // 1. ������갴���¼�
        INPUT[] downInputs = { mouseDownInput };
        SendInput((uint)downInputs.Length, downInputs, Marshal.SizeOf(typeof(INPUT)));

        // 2. �ڰ��º�̧��֮���������ӳ�
        //    ���Ը�����Ҫ�����ӳٷ�Χ������ 30-100 ����
        Thread.Sleep(random.Next(5, 11)); // �����������ģ����� 101 ��ʾ��� 100ms

        // 3. �������̧���¼�
        INPUT[] upInputs = { mouseUpInput };
        SendInput((uint)upInputs.Length, upInputs, Marshal.SizeOf(typeof(INPUT)));

        // 4. ����ԭ���ĵ��*���*����ӳ�
        Thread.Sleep(random.Next(10, 16)); // �����������ģ����� 51 ��ʾ��� 50ms
    }

    // MoveTo, SimulateHumanMove, PerformRandomShake �������ֲ���
    public static void MoveTo(int x, int y, bool preciseEnd = false)
    {
        Point currentPos = Cursor.Position;
        SimulateHumanMove(currentPos.X, currentPos.Y, x, y, preciseEnd);
        Thread.Sleep(random.Next(5, 11));
    }

    private static void SimulateHumanMove(int startX, int startY, int endX, int endY, bool preciseEnd)
    {
        int targetX = endX;
        int targetY = endY;

        if (!preciseEnd)
        {
            targetX += random.Next(-RandomRangeNear, RandomRangeNear + 1);
            targetY += random.Next(-RandomRangeNear, RandomRangeNear + 1);
        }

        // ����������� (0-65535 ��Χ)
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        // ��ֹ������
        int absoluteX = (screenWidth > 1) ? (int)((targetX * 65535) / (screenWidth - 1)) : 0;
        int absoluteY = (screenHeight > 1) ? (int)((targetY * 65535) / (screenHeight - 1)) : 0;

        // ִ�о����ƶ�
        INPUT input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx = absoluteX,
                dy = absoluteY,
                dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

        // ������Ǿ�ȷ����������������
        if (!preciseEnd)
        {
            PerformRandomShake(screenWidth, screenHeight);
            Thread.Sleep(random.Next(10, 31)); // 10-30ms
            PerformRandomShake(screenWidth, screenHeight);
        }

        // �����Ҫ��ȷ������ȷ������λ��׼ȷ
        if (preciseEnd)
        {
            Point currentPos = Cursor.Position;
            // ��ֹ������
            int finalAbsoluteX = (screenWidth > 1) ? (int)((endX * 65535) / (screenWidth - 1)) : 0;
            int finalAbsoluteY = (screenHeight > 1) ? (int)((endY * 65535) / (screenHeight - 1)) : 0;


            if (currentPos.X != endX || currentPos.Y != endY)
            {
                INPUT finalInput = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = finalAbsoluteX,
                        dy = finalAbsoluteY,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                    }
                };
                SendInput(1, new[] { finalInput }, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(random.Next(5, 16)); // 5-15ms
            }
        }
    }

    private static void PerformRandomShake(int screenWidth, int screenHeight)
    {
        Point currentPos = Cursor.Position;
        int shakeX = currentPos.X + random.Next(-RandomRangeShake, RandomRangeShake + 1);
        int shakeY = currentPos.Y + random.Next(-RandomRangeShake, RandomRangeShake + 1);

        // ���ƶ�����Χ����Ļ��
        shakeX = Math.Max(0, Math.Min(shakeX, screenWidth - 1));
        shakeY = Math.Max(0, Math.Min(shakeY, screenHeight - 1));

        // ��ֹ������
        int absoluteShakeX = (screenWidth > 1) ? (int)((shakeX * 65535) / (screenWidth - 1)) : 0;
        int absoluteShakeY = (screenHeight > 1) ? (int)((shakeY * 65535) / (screenHeight - 1)) : 0;


        INPUT input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx = absoluteShakeX,
                dy = absoluteShakeY,
                dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }
}