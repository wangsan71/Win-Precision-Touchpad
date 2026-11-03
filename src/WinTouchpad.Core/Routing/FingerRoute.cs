namespace WinTouchpad.Core.Routing;

/// <summary>Which pipeline should handle a contact frame.</summary>
public enum FingerRoute
{
    /// <summary>Zero contacts / lift.</summary>
    None = 0,

    /// <summary>One or two fingers — ignored for v1 drag (system cursor still works).</summary>
    IgnoredLowCount = 1,

    /// <summary>Three-finger path (v1 active).</summary>
    ThreeFinger = 3,

    /// <summary>Four-finger path (reserved; v1 logs only).</summary>
    FourFingerReserved = 4,

    /// <summary>Five+ fingers — ignored.</summary>
    IgnoredHighCount = 5
}