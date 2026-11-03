using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinTouchpad.Core.Logging;
using WinTouchpad.Core.Pipeline;
using WinTouchpad.Core.RawInput;
using WinTouchpad.App.Localization;
using WinTouchpad.Core.Settings;

namespace WinTouchpad.App;

/// <summary>
/// WinUI tray host: Raw Input + pipeline + TaskbarIcon + Settings window.
/// Note: H.NotifyIcon PopupMenu mode only invokes MenuFlyoutItem.Command (not Click).
/// MenuActivation=None + RightClickCommand → ShowContextMenu(GetCursorPos) fixes Win11 overflow offset.
/// </summary>
internal sealed class TrayController : IDisposable
{
    private readonly AppLogger _logger = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly TouchpadPipeline _pipeline;
    private readonly DispatcherQueue _ui;
    private AppSettings _settings;
    private RawInputHost? _rawHost;
    private TaskbarIcon? _tray;
    private Icon? _trayIcon;
    private SettingsWindow? _settingsWindow;
    private Window? _logWindow;
    private bool _disposed;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public TrayController(DispatcherQueue uiQueue)
    {
        _ui = uiQueue ?? throw new ArgumentNullException(nameof(uiQueue));
        _settings = _settingsStore.Load();
        if (!_settings.StartWithWindows && AutostartHelper.IsEnabled())
            _settings.StartWithWindows = true;

        _pipeline = new TouchpadPipeline(_logger);
        _pipeline.ApplySettings(_settings);
    }

    public void Start()
    {
        _rawHost = new RawInputHost(
            frame => _pipeline.HandleFrame(frame),
            msg => _logger.Info(msg));

        _trayIcon = LoadTrayIcon();
        _tray = new TaskbarIcon
        {
            ToolTipText = TrayText(),
            Icon = _trayIcon,
            // PopupMenu only — SecondWindow crashes unpackaged WinUI (0xc000027b).
            ContextMenuMode = ContextMenuMode.PopupMenu,
            ContextFlyout = BuildMenu(),
            // Overflow tray icons report wrong click coords in NOTIFYICON_VERSION_4 wParam
            // (often taskbar-right / chevron). Disable auto MenuActivation and show at GetCursorPos.
            MenuActivation = PopupActivationMode.None,
            NoLeftClickDelay = true
        };
        _tray.ForceCreate();
        _tray.LeftClickCommand = new RelayCommand(_ => ShowSettings());
        _tray.DoubleClickCommand = new RelayCommand(_ => ShowSettings());
        _tray.RightClickCommand = new RelayCommand(_ => ShowTrayMenuAtCursor());

        _logger.Info($"日誌目錄：{_logger.LogDirectory}");
        _logger.Info($"設定檔：{_settingsStore.FilePath}");
        _logger.Info("WinUI 3：設定窗 + 系統匣。請先將系統三指手勢設為「無」（唔會自動改）。");
        _logger.Info($"自啟狀態：{(AutostartHelper.IsEnabled() ? "ON" : "OFF")}（{AutostartHelper.RegistryKeyPath}\\{AutostartHelper.RegistryValueName}）");
        UpdateTrayText();

        if (Program.Args.Any(a => string.Equals(a, "--smoke-settings", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.Info("smoke: will open Settings shortly");
            _ = _ui.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ShowSettings();
                ShowLogWindow();
            });
        }
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(icoPath))
            {
                // Prefer the 16px frame from the multi-size generator ICO (avoids blurry downscale).
                return new Icon(icoPath, 16, 16);
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                var extracted = Icon.ExtractAssociatedIcon(exe);
                if (extracted is not null)
                    return extracted;
            }
        }
        catch
        {
            // fall through
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private MenuFlyout BuildMenu()
    {
        var menu = new MenuFlyout();

        // PopupMenu mode only runs Command — Click handlers are ignored by H.NotifyIcon.
        var settingsItem = new MenuFlyoutItem { Text = UiStrings.TraySettings };
        settingsItem.Command = new RelayCommand(_ => ShowSettings());
        menu.Items.Add(settingsItem);

        var enableItem = new ToggleMenuFlyoutItem
        {
            Text = UiStrings.EnableThreeFingerDrag,
            IsChecked = _settings.DragEnabled
        };
        enableItem.Command = new RelayCommand(_ =>
        {
            // PopupMenu does not flip ToggleMenuFlyoutItem.IsChecked for us.
            enableItem.IsChecked = !enableItem.IsChecked;
            _settings.DragEnabled = enableItem.IsChecked;
            _pipeline.SetDragEnabled(enableItem.IsChecked);
            UpdateTrayText();
            TrySave();
            RunOnUi(() => _settingsWindow?.SyncFrom(_settings));
        });
        menu.Items.Add(enableItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        var logItem = new MenuFlyoutItem { Text = UiStrings.TrayOpenLogWindow };
        logItem.Command = new RelayCommand(_ => ShowLogWindow());
        menu.Items.Add(logItem);

        var openLogDir = new MenuFlyoutItem { Text = UiStrings.TrayOpenLogFolder };
        openLogDir.Command = new RelayCommand(_ =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _logger.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Warn(string.Format(UiStrings.Culture, UiStrings.CannotOpenLogFolderFormat, ex.Message));
            }
        });
        menu.Items.Add(openLogDir);

        menu.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem { Text = UiStrings.TrayExit };
        exitItem.Command = new RelayCommand(_ => Exit());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void RunOnUi(Action action)
    {
        if (_ui.HasThreadAccess)
        {
            action();
            return;
        }

        _ = _ui.TryEnqueue(DispatcherQueuePriority.Normal, () => action());
    }

    private void ApplyLive(AppSettings settings)
    {
        _settings = settings.Clone();
        _pipeline.ApplySettings(_settings);
        UpdateTrayText();
        RebuildMenuChecked();
    }

    private void RebuildMenuChecked()
    {
        if (_tray?.ContextFlyout is not MenuFlyout menu) return;
        foreach (var item in menu.Items)
        {
            if (item is ToggleMenuFlyoutItem toggle && toggle.Text == UiStrings.EnableThreeFingerDrag)
                toggle.IsChecked = _settings.DragEnabled;
        }
    }

    private void UpdateTrayText()
    {
        if (_tray is null) return;
        _tray.ToolTipText = TrayText();
    }

    private string TrayText()
    {
        var on = _settings.DragEnabled ? UiStrings.TrayOn : UiStrings.TrayOff;
        return string.Format(UiStrings.Culture, UiStrings.TrayToolTipFormat, on);
    }

    /// <summary>
    /// H.NotifyIcon PopupMenu uses NOTIFYICON callback wParam coords; for Win11 overflow
    /// icons those often point at the taskbar notification edge (menu floats far right).
    /// Re-show at live cursor instead.
    /// </summary>
    private void ShowTrayMenuAtCursor()
    {
        if (_tray is null) return;
        if (!GetCursorPos(out var pt))
            return;
        try
        {
            _tray.ShowContextMenu(new System.Drawing.Point(pt.X, pt.Y));
        }
        catch (Exception ex)
        {
            _logger.Warn($"ShowTrayMenuAtCursor failed：{ex.Message}");
        }
    }

    private void ShowSettings() => RunOnUi(ShowSettingsCore);

    private void ShowSettingsCore()
    {
        try
        {
            _logger.Info("ShowSettings invoked");
            if (_settingsWindow is null)
            {
                _settingsWindow = new SettingsWindow(
                    _settingsStore,
                    _settings.Clone(),
                    ApplyLive,
                    GetExecutablePath);
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            }
            else
            {
                _settingsWindow.SyncFrom(_settings);
            }

            PresentWindow(_settingsWindow);
        }
        catch (Exception ex)
        {
            _logger.Warn($"ShowSettings failed：{ex}");
            WriteCrashLog("ShowSettings", ex);
        }
    }

    private void ShowLogWindow() => RunOnUi(ShowLogWindowCore);

    private void ShowLogWindowCore()
    {
        try
        {
            _logger.Info("ShowLogWindow invoked");
            if (_logWindow is null)
            {
                _logWindow = new LogWindow(_logger);
                _logWindow.Closed += (_, _) => _logWindow = null;
            }

            PresentWindow(_logWindow);
        }
        catch (Exception ex)
        {
            _logger.Warn($"ShowLogWindow failed：{ex}");
            WriteCrashLog("ShowLogWindow", ex);
        }
    }

    private static void PresentWindow(Window window)
    {
        window.AppWindow.IsShownInSwitchers = true;
        window.AppWindow.Show();
        window.Activate();
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            SetForegroundWindow(hwnd);
        }
        catch
        {
            // best-effort
        }
    }

    private static void WriteCrashLog(string where, Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinPrecisionTouchpad");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "unhandled.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {where}: {ex}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }

    private static string GetExecutablePath()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path))
                return path;
        }
        catch
        {
            // fall through
        }

        return Process.GetCurrentProcess().MainModule?.FileName
               ?? Path.Combine(AppContext.BaseDirectory, "WinTouchpad.App.exe");
    }

    private void TrySave()
    {
        try { _settingsStore.Save(_settings); }
        catch (Exception ex) { _logger.Warn(string.Format(UiStrings.Culture, UiStrings.CannotSaveSettingsFormat, ex.Message)); }
    }

    private void Exit()
    {
        RunOnUi(() =>
        {
            Dispose();
            Application.Current?.Exit();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pipeline.Drag.Reset();
        _rawHost?.Dispose();
        _tray?.Dispose();
        _trayIcon?.Dispose();
        _logger.Dispose();
        try { _settingsWindow?.Close(); } catch { /* ignore */ }
        try { _logWindow?.Close(); } catch { /* ignore */ }
    }
}

/// <summary>Minimal ICommand for TaskbarIcon / MenuFlyoutItem handlers.</summary>
internal sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute) => _execute = execute;
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}
