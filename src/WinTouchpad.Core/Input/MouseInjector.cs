using System.Runtime.InteropServices;

namespace WinTouchpad.Core.Input;

/// <summary>
/// SendInput-based mouse injector (relative move + left button).
/// Approach adapted from ClementGre/ThreeFingerDragOnWindows MouseOperations (MIT) — rewritten for this project.
/// </summary>
public sealed class MouseInjector : IMouseInjector
{
    private const int InputMouse = 0;
    private const int MouseEventFMove = 0x0001;
    private const int MouseEventFLeftDown = 0x0002;
    private const int MouseEventFLeftUp = 0x0004;

    private float _residualX;
    private float _residualY;

    public void LeftDown() => SendButton(MouseEventFLeftDown);

    public void LeftUp() => SendButton(MouseEventFLeftUp);

    public void MoveRelative(float dx, float dy)
    {
        var totalX = dx + _residualX;
        var totalY = dy + _residualY;
        var intX = (int)totalX;
        var intY = (int)totalY;
        _residualX = totalX - intX;
        _residualY = totalY - intY;

        if (intX == 0 && intY == 0)
            return;

        var input = new Input
        {
            type = InputMouse,
            mi = new MouseInput
            {
                dx = intX,
                dy = intY,
                dwFlags = MouseEventFMove
            }
        };

        var sent = SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
        if (sent == 0)
            throw new InvalidOperationException($"SendInput MOVE failed (Win32={Marshal.GetLastWin32Error()}).");
    }

    private static void SendButton(int flag)
    {
        var input = new Input
        {
            type = InputMouse,
            mi = new MouseInput { dwFlags = flag }
        };

        var sent = SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
        if (sent == 0)
            throw new InvalidOperationException($"SendInput button failed (Win32={Marshal.GetLastWin32Error()}).");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public int type;
        public MouseInput mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
    }
}