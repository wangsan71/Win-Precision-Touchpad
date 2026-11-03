namespace WinTouchpad.Core.Contacts;

/// <summary>
/// One finger contact reported by a Precision Touchpad HID report.
/// Adapted from emoacht/RawInput.Touchpad (MIT) and ClementGre/ThreeFingerDragOnWindows (MIT).
/// </summary>
public readonly struct TouchpadContact : IEquatable<TouchpadContact>
{
    public int ContactId { get; }
    public int X { get; }
    public int Y { get; }

    public TouchpadContact(int contactId, int x, int y)
    {
        ContactId = contactId;
        X = x;
        Y = y;
    }

    public bool Equals(TouchpadContact other) =>
        ContactId == other.ContactId && X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is TouchpadContact other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ContactId, X, Y);

    public static bool operator ==(TouchpadContact a, TouchpadContact b) => a.Equals(b);
    public static bool operator !=(TouchpadContact a, TouchpadContact b) => !a.Equals(b);

    public override string ToString() => $"id={ContactId} x={X} y={Y}";
}

/// <summary>Accumulates ContactId/X/Y fields while walking HID value caps.</summary>
internal sealed class TouchpadContactBuilder
{
    public int? ContactId { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }

    public bool TryCreate(out TouchpadContact contact)
    {
        if (ContactId.HasValue && X.HasValue && Y.HasValue)
        {
            contact = new TouchpadContact(ContactId.Value, X.Value, Y.Value);
            return true;
        }

        contact = default;
        return false;
    }

    public void Clear()
    {
        ContactId = null;
        X = null;
        Y = null;
    }
}