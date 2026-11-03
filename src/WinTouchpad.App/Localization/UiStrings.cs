using System.Globalization;
using System.Resources;

namespace WinTouchpad.App.Localization;

/// <summary>
/// UI string lookup via .resx satellites.
/// Default (neutral) = Traditional Chinese; zh-CN / en* override.
/// Culture follows Windows UI language (CurrentUICulture).
/// </summary>
internal static class UiStrings
{
    private static readonly ResourceManager Manager =
        new("WinTouchpad.App.Localization.Strings", typeof(UiStrings).Assembly);

    public static CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

    public static void SetCulture(CultureInfo culture)
    {
        Culture = culture ?? CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
    }

    public static string Get(string name) =>
        Manager.GetString(name, Culture) ?? Manager.GetString(name, CultureInfo.InvariantCulture) ?? name;

    public static string Format(string name, params object[] args) =>
        string.Format(Culture, Get(name), args);

    public static string SettingsWindowTitle => Get(nameof(SettingsWindowTitle));
    public static string SettingsHeading => Get(nameof(SettingsHeading));
    public static string SettingsIntro => Get(nameof(SettingsIntro));
    public static string SectionDrag => Get(nameof(SectionDrag));
    public static string EnableThreeFingerDrag => Get(nameof(EnableThreeFingerDrag));
    public static string ToggleOn => Get(nameof(ToggleOn));
    public static string ToggleOff => Get(nameof(ToggleOff));
    public static string SystemGestureHint => Get(nameof(SystemGestureHint));
    public static string Sensitivity => Get(nameof(Sensitivity));
    public static string SensitivityHint => Get(nameof(SensitivityHint));
    public static string StartDistance => Get(nameof(StartDistance));
    public static string StartDistanceHint => Get(nameof(StartDistanceHint));
    public static string SectionStartup => Get(nameof(SectionStartup));
    public static string StartWithWindows => Get(nameof(StartWithWindows));
    public static string SectionAbout => Get(nameof(SectionAbout));
    public static string PathHintFormat => Get(nameof(PathHintFormat));
    public static string Close => Get(nameof(Close));
    public static string Save => Get(nameof(Save));
    public static string SavedStatusFormat => Get(nameof(SavedStatusFormat));
    public static string SaveFailedFormat => Get(nameof(SaveFailedFormat));
    public static string LogWindowTitle => Get(nameof(LogWindowTitle));
    public static string TraySettings => Get(nameof(TraySettings));
    public static string TrayOpenLogWindow => Get(nameof(TrayOpenLogWindow));
    public static string TrayOpenLogFolder => Get(nameof(TrayOpenLogFolder));
    public static string TrayExit => Get(nameof(TrayExit));
    public static string TrayToolTipFormat => Get(nameof(TrayToolTipFormat));
    public static string TrayOn => Get(nameof(TrayOn));
    public static string TrayOff => Get(nameof(TrayOff));
    public static string CannotOpenLogFolderFormat => Get(nameof(CannotOpenLogFolderFormat));
    public static string CannotSaveSettingsFormat => Get(nameof(CannotSaveSettingsFormat));
}
