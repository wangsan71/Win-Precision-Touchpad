using Microsoft.UI.Xaml;

namespace WinTouchpad.App;

public partial class App : Application
{
    private TrayController? _tray;
    private Window? _lifetimeWindow;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WinPrecisionTouchpad");
                Directory.CreateDirectory(dir);
                File.AppendAllText(
                    Path.Combine(dir, "unhandled.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}{Environment.NewLine}");
            }
            catch
            {
                // ignore
            }

            System.Diagnostics.Debug.WriteLine(e.Exception);
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Keep a hidden lifetime window so the WinUI dispatcher stays alive with tray-only UX.
        _lifetimeWindow = new MainWindow();
        _lifetimeWindow.AppWindow.IsShownInSwitchers = false;
        _lifetimeWindow.Activate();

        _tray = new TrayController(_lifetimeWindow.DispatcherQueue);
        _tray.Start();

        // Hide after tray is up — immediate Hide can race with notify-icon window setup.
        _lifetimeWindow.DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () => _lifetimeWindow?.AppWindow.Hide());

        _lifetimeWindow.Closed += (_, _) =>
        {
            _tray?.Dispose();
            _tray = null;
        };
    }
}