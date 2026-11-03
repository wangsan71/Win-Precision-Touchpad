using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinTouchpad.App.Localization;
using WinTouchpad.Core.Settings;

namespace WinTouchpad.App;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;
    private readonly Action<AppSettings> _applyLive;
    private readonly Func<string> _getExePath;
    private bool _suppress;

    public SettingsWindow(
        SettingsStore store,
        AppSettings current,
        Action<AppSettings> applyLive,
        Func<string> getExePath)
    {
        _store = store;
        _applyLive = applyLive;
        _getExePath = getExePath;

        InitializeComponent();
        ApplyLocalizedStrings();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(560, 720));
        WindowChromeHelper.ApplyDarkTitleBar(this);

        SensitivitySlider.ValueChanged += (_, _) =>
        {
            SensitivityValueText.Text = SensitivitySlider.Value.ToString("0.00");
            if (!_suppress) ApplyLiveFromUi();
        };
        EnableDragToggle.Toggled += (_, _) => { if (!_suppress) ApplyLiveFromUi(); };
        StartDistanceBox.ValueChanged += (_, _) => { if (!_suppress) ApplyLiveFromUi(); };

        SaveButton.Click += (_, _) => Save();
        CloseButton.Click += (_, _) => Close();

        SyncFrom(current);
    }

    private void ApplyLocalizedStrings()
    {
        Title = UiStrings.SettingsWindowTitle;
        HeadingText.Text = UiStrings.SettingsHeading;
        IntroText.Text = UiStrings.SettingsIntro;
        SectionDragText.Text = UiStrings.SectionDrag;
        EnableDragToggle.Header = UiStrings.EnableThreeFingerDrag;
        EnableDragToggle.OnContent = UiStrings.ToggleOn;
        EnableDragToggle.OffContent = UiStrings.ToggleOff;
        SystemGestureHintText.Text = UiStrings.SystemGestureHint;
        SensitivityLabel.Text = UiStrings.Sensitivity;
        SensitivityHintText.Text = UiStrings.SensitivityHint;
        StartDistanceLabel.Text = UiStrings.StartDistance;
        StartDistanceHintText.Text = UiStrings.StartDistanceHint;
        SectionStartupText.Text = UiStrings.SectionStartup;
        StartWithWindowsToggle.Header = UiStrings.StartWithWindows;
        StartWithWindowsToggle.OnContent = UiStrings.ToggleOn;
        StartWithWindowsToggle.OffContent = UiStrings.ToggleOff;
        SectionAboutText.Text = UiStrings.SectionAbout;
        CloseButton.Content = UiStrings.Close;
        SaveButton.Content = UiStrings.Save;
    }

    public void SyncFrom(AppSettings settings)
    {
        _suppress = true;
        try
        {
            EnableDragToggle.IsOn = settings.DragEnabled;
            SensitivitySlider.Value = settings.Sensitivity;
            SensitivityValueText.Text = settings.Sensitivity.ToString("0.00");
            StartDistanceBox.Value = settings.StartDistanceThreshold;
            StartWithWindowsToggle.IsOn = settings.StartWithWindows;
            PathHintText.Text = string.Format(
                UiStrings.Culture,
                UiStrings.PathHintFormat,
                _store.FilePath,
                _getExePath(),
                AutostartHelper.RegistryKeyPath,
                AutostartHelper.RegistryValueName);
            StatusText.Text = string.Empty;
        }
        finally
        {
            _suppress = false;
        }
    }

    private AppSettings ReadUi()
    {
        var s = new AppSettings
        {
            DragEnabled = EnableDragToggle.IsOn,
            Sensitivity = (float)SensitivitySlider.Value,
            StartDistanceThreshold = (float)(double.IsNaN(StartDistanceBox.Value) ? 12 : StartDistanceBox.Value),
            StartWithWindows = StartWithWindowsToggle.IsOn
        };
        s.Clamp();
        return s;
    }

    private void ApplyLiveFromUi() => _applyLive(ReadUi());

    private void Save()
    {
        var settings = ReadUi();
        try
        {
            _store.Save(settings);
            AutostartHelper.SetEnabled(settings.StartWithWindows, _getExePath());
            _applyLive(settings);
            StatusText.Text = string.Format(UiStrings.Culture, UiStrings.SavedStatusFormat, DateTime.Now);
        }
        catch (Exception ex)
        {
            StatusText.Text = string.Format(UiStrings.Culture, UiStrings.SaveFailedFormat, ex.Message);
        }
    }
}
