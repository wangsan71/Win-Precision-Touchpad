namespace WinTouchpad.Core.Drag;

/// <summary>Three-finger drag state machine states (plan: Idle → Pending → Dragging → End).</summary>
public enum DragState
{
    Idle = 0,
    Pending = 1,
    Dragging = 2,
    /// <summary>Transient end-of-gesture marker reported on StateChanged before returning to Idle.</summary>
    End = 3,
    /// <summary>The injected left button is still down because release has not succeeded.</summary>
    ReleasePending = 4
}