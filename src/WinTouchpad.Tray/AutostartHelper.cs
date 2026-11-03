using WinTouchpad.Core.Settings;

namespace WinTouchpad.Tray;

/// <summary>Compatibility wrapper — implementation lives in <see cref="AutostartHelper"/> (Core).</summary>
internal static class AutostartHelper
{
    public static string RegistryValueName => WinTouchpad.Core.Settings.AutostartHelper.RegistryValueName;
    public static string RegistryKeyPath => WinTouchpad.Core.Settings.AutostartHelper.RegistryKeyPath;
    public static bool IsEnabled() => WinTouchpad.Core.Settings.AutostartHelper.IsEnabled();
    public static void SetEnabled(bool enabled, string executablePath) =>
        WinTouchpad.Core.Settings.AutostartHelper.SetEnabled(enabled, executablePath);
}
