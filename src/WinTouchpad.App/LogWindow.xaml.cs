using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinTouchpad.App.Localization;
using WinTouchpad.Core.Logging;

namespace WinTouchpad.App;

public sealed partial class LogWindow : Window
{
    private readonly AppLogger _logger;
    private readonly Action<string> _handler;
    private readonly DispatcherQueue _dispatcher;

    public LogWindow(AppLogger logger)
    {
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        InitializeComponent();
        Title = UiStrings.LogWindowTitle;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 520));
        WindowChromeHelper.ApplyDarkTitleBar(this);

        var existing = string.Join(Environment.NewLine, _logger.GetRecentLines());
        if (!string.IsNullOrEmpty(existing))
            LogTextBox.Text = existing + Environment.NewLine;

        _handler = AppendLine;
        _logger.LineWritten += _handler;
        Closed += (_, _) => _logger.LineWritten -= _handler;
    }

    private void AppendLine(string line)
    {
        if (!_dispatcher.HasThreadAccess)
        {
            _dispatcher.TryEnqueue(() => AppendLine(line));
            return;
        }

        LogTextBox.Text += line + Environment.NewLine;
    }
}
