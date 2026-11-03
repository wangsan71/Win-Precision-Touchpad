namespace WinTouchpad.Core.Contacts;

/// <summary>
/// Tracks contacts across frames and computes per-contact deltas for the drag engine (M2).
/// </summary>
public sealed class ContactTracker
{
    private readonly Dictionary<int, TouchpadContact> _previous = new();

    /// <summary>Update with the latest frame and return per-contact movement since the previous frame.</summary>
    public IReadOnlyList<ContactDelta> Update(IReadOnlyList<TouchpadContact> contacts)
    {
        var deltas = new List<ContactDelta>(contacts.Count);
        var seen = new HashSet<int>();

        foreach (var c in contacts)
        {
            seen.Add(c.ContactId);
            if (_previous.TryGetValue(c.ContactId, out var prev))
            {
                deltas.Add(new ContactDelta(c.ContactId, c.X - prev.X, c.Y - prev.Y, c.X, c.Y, isNew: false));
            }
            else
            {
                deltas.Add(new ContactDelta(c.ContactId, 0, 0, c.X, c.Y, isNew: true));
            }

            _previous[c.ContactId] = c;
        }

        foreach (var id in _previous.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _previous.Remove(id);
        }

        return deltas;
    }

    public void Reset() => _previous.Clear();
}

/// <summary>Delta for one contact between consecutive frames.</summary>
public readonly struct ContactDelta
{
    public ContactDelta(int contactId, int dx, int dy, int x, int y, bool isNew)
    {
        ContactId = contactId;
        Dx = dx;
        Dy = dy;
        X = x;
        Y = y;
        IsNew = isNew;
    }

    public int ContactId { get; }
    public int Dx { get; }
    public int Dy { get; }
    public int X { get; }
    public int Y { get; }
    public bool IsNew { get; }

    public override string ToString() =>
        IsNew
            ? $"id={ContactId} NEW x={X} y={Y}"
            : $"id={ContactId} dx={Dx} dy={Dy} x={X} y={Y}";
}