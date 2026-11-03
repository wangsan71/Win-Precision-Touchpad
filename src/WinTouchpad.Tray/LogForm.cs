using WinTouchpad.Core.Logging;

namespace WinTouchpad.Tray;

/// <summary>Simple live log window for verification.</summary>
internal sealed class LogForm : Form
{
    private readonly TextBox _textBox;
    private readonly AppLogger _logger;
    private readonly Action<string> _handler;

    public LogForm(AppLogger logger)
    {
        _logger = logger;
        Text = "Win Precision Touchpad — 日誌";
        Width = 900;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = true;
        ShowInTaskbar = true;

        _textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9f),
            WordWrap = false
        };
        Controls.Add(_textBox);

        var existing = string.Join(Environment.NewLine, _logger.GetRecentLines());
        if (!string.IsNullOrEmpty(existing))
        {
            _textBox.Text = existing + Environment.NewLine;
            _textBox.SelectionStart = _textBox.TextLength;
            _textBox.ScrollToCaret();
        }

        _handler = AppendLine;
        _logger.LineWritten += _handler;
        FormClosed += (_, _) => _logger.LineWritten -= _handler;
    }

    private void AppendLine(string line)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLine(line));
            return;
        }

        _textBox.AppendText(line + Environment.NewLine);
    }
}