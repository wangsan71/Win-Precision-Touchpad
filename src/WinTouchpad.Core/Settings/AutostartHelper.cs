using Microsoft.Win32;

namespace WinTouchpad.Core.Settings;

/// <summary>
/// Current-User logon autostart via
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> (no admin required).
/// Does <b>not</b> modify touchpad gesture registry keys.
/// </summary>
public static class AutostartHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WinPrecisionTouchpad";

    public static string RegistryValueName => ValueName;
    public static string RegistryKeyPath => @"HKEY_CURRENT_USER\" + RunKeyPath;

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(ValueName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new ArgumentException("Executable path is required.", nameof(executablePath));

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Cannot open HKCU Run key.");

        if (enabled)
        {
            var quoted = executablePath.Contains('"')
                ? executablePath
                : $"\"{executablePath}\"";
            key.SetValue(ValueName, quoted, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
