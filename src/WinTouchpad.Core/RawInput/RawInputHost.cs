using System.Runtime.InteropServices;
using WinTouchpad.Core.Contacts;

namespace WinTouchpad.Core.RawInput;

/// <summary>
/// Dedicated STA thread + HWND_MESSAGE window that receives WM_INPUT for the touchpad.
/// UI frameworks (WinForms / WinUI) can start this without owning a visible window.
/// </summary>
public sealed class RawInputHost : IDisposable
{
    private readonly Action<ContactFrame> _onFrame;
    private readonly Action<string> _status;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly WndProcDlg _wndProc; // keep rooted for native callback
    private volatile bool _disposed;
    private IntPtr _hwnd;
    private uint _threadId;

    public RawInputHost(Action<ContactFrame> onFrame, Action<string> status)
    {
        _onFrame = onFrame ?? throw new ArgumentNullException(nameof(onFrame));
        _status = status ?? (_ => { });
        _wndProc = WndProc;
        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "WinTouchpad.RawInput",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("Raw Input host window failed to start.");
    }

    public IntPtr Handle => _hwnd;

    private void ThreadMain()
    {
        try
        {
            _threadId = GetCurrentThreadId();
            var className = "WinTouchpad.RawInput.MsgOnly." + Guid.NewGuid().ToString("N");
            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = _wndProc,
                hInstance = GetModuleHandle(null),
                lpszClassName = className
            };

            ushort atom = RegisterClassEx(ref wndClass);
            if (atom == 0)
            {
                _status($"RegisterClassEx failed (Win32={Marshal.GetLastWin32Error()})");
                _ready.Set();
                return;
            }

            _hwnd = CreateWindowEx(
                0, className, "WinTouchpad.RawInput",
                0, 0, 0, 0, 0,
                HWND_MESSAGE, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                _status($"CreateWindowEx failed (Win32={Marshal.GetLastWin32Error()})");
                _ready.Set();
                return;
            }

            bool exists = TouchpadRawInput.Exists();
            _status(exists
                ? "已偵測到 Precision Touchpad 裝置。"
                : "未偵測到 Precision Touchpad（Usage Page 0x0D / Usage 0x05）。");

            bool registered = TouchpadRawInput.Register(_hwnd);
            _status(registered
                ? "Raw Input 已註冊（含背景 INPUTSINK）。"
                : $"Raw Input 註冊失敗（Win32={Marshal.GetLastWin32Error()}）。");

            _ready.Set();

            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        catch (Exception ex)
        {
            _status("RawInputHost crash: " + ex.Message);
            _ready.Set();
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == TouchpadRawInput.WmInput)
        {
            try
            {
                var frame = TouchpadRawInput.ParseInput(lParam);
                if (frame is not null)
                    _onFrame(frame);
            }
            catch (Exception ex)
            {
                _status("Parse/Handle WM_INPUT failed: " + ex.Message);
            }
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_threadId != 0)
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        if (!_thread.Join(TimeSpan.FromSeconds(3)) && _hwnd != IntPtr.Zero)
            DestroyWindow(_hwnd);
        _ready.Dispose();
    }

    private const uint WM_QUIT = 0x0012;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private delegate IntPtr WndProcDlg(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WndProcDlg lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
