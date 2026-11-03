using WinTouchpad.Core.Logging;
using WinTouchpad.Core.Pipeline;
using WinTouchpad.Core.Settings;

namespace WinTouchpad.Tray;

/// <summary>System-tray host for three-finger drag (M3: settings + autostart).</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly AppLogger _logger;
    private readonly TouchpadPipeline _pipeline;
    private readonly RawInputMessageWindow _rawWindow;
    private readonly SettingsStore _settingsStore;
    private readonly ToolStripMenuItem _enableDragItem;
    private AppSettings _settings;
    private LogForm? _logForm;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _logger = new AppLogger();
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        // Prefer file for StartWithWindows; also reflect actual HKCU state if already registered.
        if (!_settings.StartWithWindows && AutostartHelper.IsEnabled())
            _settings.StartWithWindows = true;

        _pipeline = new TouchpadPipeline(_logger);
        _pipeline.ApplySettings(_settings);

        _enableDragItem = new ToolStripMenuItem("啟用三指拖曳")
        {
            Checked = _settings.DragEnabled,
            CheckOnClick = true
        };
        _enableDragItem.CheckedChanged += OnEnableDragMenuChanged;

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        UpdateTrayText();
        _tray.DoubleClick += (_, _) => ShowSettingsWindow();

        _rawWindow = new RawInputMessageWindow(_pipeline, msg => _logger.Info(msg));
        _logger.Info($"日誌目錄：{_logger.LogDirectory}");
        _logger.Info($"設定檔：{_settingsStore.FilePath}");
        _logger.Info("M3：設定窗 + 自啟。請先將系統三指手勢設為「無」（唔會自動改）。");
        _logger.Info($"自啟狀態：{(AutostartHelper.IsEnabled() ? "ON" : "OFF")}（{AutostartHelper.RegistryKeyPath}\\{AutostartHelper.RegistryValueName}）");
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("設定…", null, (_, _) => ShowSettingsWindow());
        menu.Items.Add(_enableDragItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("開啟日誌視窗", null, (_, _) => ShowLogWindow());
        menu.Items.Add("開啟日誌資料夾", null, (_, _) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _logger.LogDirectory,
                UseShellExecute = true
            });
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("結束", null, (_, _) => Exit());
        return menu;
    }

    private void OnEnableDragMenuChanged(object? sender, EventArgs e)
    {
        _settings.DragEnabled = _enableDragItem.Checked;
        _pipeline.SetDragEnabled(_enableDragItem.Checked);
        UpdateTrayText();
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception ex)
        {
            _logger.Warn($"無法儲存設定：{ex.Message}");
        }

        _settingsForm?.SyncFrom(_settings);
    }

    private void ApplyLive(AppSettings settings)
    {
        _settings = settings.Clone();
        _pipeline.ApplySettings(_settings);

        if (_enableDragItem.Checked != _settings.DragEnabled)
        {
            _enableDragItem.CheckedChanged -= OnEnableDragMenuChanged;
            _enableDragItem.Checked = _settings.DragEnabled;
            _enableDragItem.CheckedChanged += OnEnableDragMenuChanged;
        }

        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        var on = _enableDragItem.Checked ? "ON" : "OFF";
        _tray.Text = $"Win Precision Touchpad (M3) [{on}]";
    }

    private void ShowSettingsWindow()
    {
        if (_settingsForm is null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(
                _settingsStore,
                _settings.Clone(),
                ApplyLive,
                () => Application.ExecutablePath);
            _settingsForm.FormClosed += (_, _) => _settingsForm = null;
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.SyncFrom(_settings);
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.Activate();
        }
    }

    private void ShowLogWindow()
    {
        if (_logForm is null || _logForm.IsDisposed)
        {
            _logForm = new LogForm(_logger);
            _logForm.Show();
        }
        else
        {
            _logForm.WindowState = FormWindowState.Normal;
            _logForm.Activate();
        }
    }

    private void Exit()
    {
        _pipeline.Drag.Reset();
        _tray.Visible = false;
        _rawWindow.Dispose();
        _logger.Dispose();
        _logForm?.Close();
        _settingsForm?.Close();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pipeline.Drag.Reset();
            _tray.Dispose();
            _rawWindow.Dispose();
            _logger.Dispose();
        }

        base.Dispose(disposing);
    }
}