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
        // Retain a XAML window and its dispatcher for the tray application's lifetime.
        // Window construction creates the native host; Activate() would display its
        // empty, dark content. Never show it just to hide it later: tray startup can
        // block or fail before a queued Hide() gets a chance to run.
        _lifetimeWindow = new MainWindow();
        _lifetimeWindow.AppWindow.IsShownInSwitchers = false;
        _lifetimeWindow.AppWindow.Hide();

        _lifetimeWindow.Closed += (_, _) =>
        {
            _tray?.Dispose();
            _tray = null;
        };

        _tray = new TrayController(_lifetimeWindow.DispatcherQueue);
        _tray.Start();
    }
}