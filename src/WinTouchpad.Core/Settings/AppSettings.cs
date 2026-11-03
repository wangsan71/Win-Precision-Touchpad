using System.Text.Json.Serialization;

namespace WinTouchpad.Core.Settings;

/// <summary>
/// Persisted application settings (JSON). <see cref="SettingsVersion"/> enables future migrations.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Current schema version written by this build.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Settings file schema version for upgrades.</summary>
    [JsonPropertyName("settingsVersion")]
    public int SettingsVersion { get; set; } = CurrentVersion;

    /// <summary>When false, three-finger drag injection is disabled.</summary>
    [JsonPropertyName("dragEnabled")]
    public bool DragEnabled { get; set; } = true;

    /// <summary>Multiplier from HID delta to screen pixels (typical range 0.2–2.0).</summary>
    [JsonPropertyName("sensitivity")]
    public float Sensitivity { get; set; } = 0.6f;

    /// <summary>HID-unit distance in Pending before left-button down.</summary>
    [JsonPropertyName("startDistanceThreshold")]
    public float StartDistanceThreshold { get; set; } = 12f;

    /// <summary>When true, app should register itself for Current-User logon start.</summary>
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; }

    /// <summary>Creates defaults used when no file exists.</summary>
    public static AppSettings CreateDefault() => new();

    /// <summary>Clamp numeric fields to safe ranges after load or UI edit.</summary>
    public void Clamp()
    {
        if (Sensitivity < 0.2f) Sensitivity = 0.2f;
        if (Sensitivity > 2.0f) Sensitivity = 2.0f;
        if (StartDistanceThreshold < 0f) StartDistanceThreshold = 0f;
        if (StartDistanceThreshold > 200f) StartDistanceThreshold = 200f;
    }

    /// <summary>Deep-ish copy for UI editing without mutating the live store copy until Apply.</summary>
    public AppSettings Clone() => new()
    {
        SettingsVersion = SettingsVersion,
        DragEnabled = DragEnabled,
        Sensitivity = Sensitivity,
        StartDistanceThreshold = StartDistanceThreshold,
        StartWithWindows = StartWithWindows
    };
}
