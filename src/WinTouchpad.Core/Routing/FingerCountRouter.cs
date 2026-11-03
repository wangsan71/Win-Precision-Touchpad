using WinTouchpad.Core.Contacts;

namespace WinTouchpad.Core.Routing;

/// <summary>
/// Routes frames by simultaneous contact count so 3-finger and 4-finger logic stay separate.
/// </summary>
public sealed class FingerCountRouter
{
    /// <summary>Classify a frame. Prefer <see cref="ContactFrame.FingerCount"/>.</summary>
    public FingerRoute Route(ContactFrame frame) => Route(frame.FingerCount);

    /// <summary>Classify by finger count alone (unit-test friendly).</summary>
    public FingerRoute Route(int fingerCount)
    {
        return fingerCount switch
        {
            <= 0 => FingerRoute.None,
            1 or 2 => FingerRoute.IgnoredLowCount,
            3 => FingerRoute.ThreeFinger,
            4 => FingerRoute.FourFingerReserved,
            _ => FingerRoute.IgnoredHighCount
        };
    }
}