using System.Runtime.InteropServices;
using WinTouchpad.Core.Pipeline;
using WinTouchpad.Core.RawInput;

namespace WinTouchpad.Tray;

/// <summary>
/// Invisible NativeWindow that owns the Raw Input registration and receives WM_INPUT.
/// </summary>
internal sealed class RawInputMessageWindow : NativeWindow, IDisposable
{
    private readonly TouchpadPipeline _pipeline;
    private readonly Action<string> _status;
    private bool _disposed;

    public RawInputMessageWindow(TouchpadPipeline pipeline, Action<string> status)
    {
        _pipeline = pipeline;
        _status = status;

        var cp = new CreateParams
        {
            Caption = "WinTouchpad.RawInput",
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            Style = 0,
            ExStyle = 0
        };
        CreateHandle(cp);

        bool exists = TouchpadRawInput.Exists();
        _status(exists
            ? "已偵測到 Precision Touchpad 裝置。"
            : "未偵測到 Precision Touchpad（Usage Page 0x0D / Usage 0x05）。");

        bool registered = TouchpadRawInput.Register(Handle);
        _status(registered
            ? "Raw Input 已註冊（含背景 INPUTSINK）。"
            : $"Raw Input 註冊失敗（Win32={Marshal.GetLastWin32Error()}）。");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == TouchpadRawInput.WmInput)
        {
            var frame = TouchpadRawInput.ParseInput(m.LParam);
            if (frame is not null)
            {
                _pipeline.HandleFrame(frame);
            }
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyHandle();
    }
}