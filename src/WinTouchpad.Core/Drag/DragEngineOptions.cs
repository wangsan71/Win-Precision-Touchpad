namespace WinTouchpad.Core.Drag;

/// <summary>Tunable thresholds for three-finger drag (exposed by M3 Settings UI / SettingsStore).</summary>
public sealed class DragEngineOptions
{
    /// <summary>
    /// Euclidean HID-unit distance that must accumulate in Pending before left-button down.
    /// Prevents accidental clicks when three fingers land without intending to drag.
    /// DragEngine requires 3 established contacts; one settle frame only when a contact was just added.
    /// </summary>
    public float StartDistanceThreshold { get; set; } = 12f;

    /// <summary>Multiplier from average contact HID delta to screen-pixel relative move.</summary>
    public float Sensitivity { get; set; } = 0.6f;

    /// <summary>If true, DragEngine injects mouse events; if false, state stays Idle and Process is a no-op for injection.</summary>
    public bool Enabled { get; set; } = true;
}
