// KeyboardSimulator.cs
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for Keys enum

public static class KeyboardSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern ushort VkKeyScan(char ch);

    // Input event structures
    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // Input types
    private const int INPUT_KEYBOARD = 1;

    // Keyboard event flags
    private const uint KEYEVENTF_KEYDOWN = 0x0000; // Key down flag (Note: Sometimes 0 is used for keydown)
    private const uint KEYEVENTF_KEYUP = 0x0002;   // Key up flag
    // private const uint KEYEVENTF_UNICODE = 0x0004; // Unicode character flag
    // private const uint KEYEVENTF_SCANCODE = 0x0008; // Scan code flag

    private static readonly Random random = new Random();

    /// <summary>
    /// Simulates pressing and releasing a virtual key code.
    /// Includes small random delays to mimic human interaction.
    /// </summary>
    /// <param name="keyCode">The virtual key code to press.</param>
    public static void SendKey(Keys keyCode)
    {
        INPUT[] inputs = new INPUT[2];

        // Key down event
        inputs[0] = new INPUT
        {
            Type = INPUT_KEYBOARD,
            Data = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0, // 0 for virtual key code
                    dwFlags = KEYEVENTF_KEYDOWN, // Key down
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up event
        inputs[1] = new INPUT
        {
            Type = INPUT_KEYBOARD,
            Data = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP, // Key up
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Send key down
        SendInput(1, new INPUT[] { inputs[0] }, Marshal.SizeOf(typeof(INPUT)));
        // Simulate human-like delay between down and up
        Thread.Sleep(random.Next(5, 11)); // Random delay 

        // Send key up
        SendInput(1, new INPUT[] { inputs[1] }, Marshal.SizeOf(typeof(INPUT)));
        // Simulate small delay after key press
        Thread.Sleep(random.Next(20, 25)); // Random delay 

        Console.WriteLine($"动作: 模拟按键 {keyCode}");
    }

    // You could add other methods here, like SendKeyDown, SendKeyUp, SendString etc.
}