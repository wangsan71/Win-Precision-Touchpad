namespace WinTouchpad.Core.Contacts;

/// <summary>
/// Tracks contacts across frames and computes per-contact deltas for the drag engine (M2).
/// </summary>
public sealed class ContactTracker
{
    private readonly Dictionary<int, TouchpadContact> _previous = new();
    private readonly List<ContactDelta> _deltas = new(8);
    private readonly List<int> _removeScratch = new(8);

    /// <summary>Update with the latest frame and return per-contact movement since the previous frame.</summary>
    public IReadOnlyList<ContactDelta> Update(IReadOnlyList<TouchpadContact> contacts)
    {
        _deltas.Clear();

        foreach (var c in contacts)
        {
            if (_previous.TryGetValue(c.ContactId, out var prev))
            {
                _deltas.Add(new ContactDelta(c.ContactId, c.X - prev.X, c.Y - prev.Y, c.X, c.Y, isNew: false));
            }
            else
            {
                _deltas.Add(new ContactDelta(c.ContactId, 0, 0, c.X, c.Y, isNew: true));
            }

            _previous[c.ContactId] = c;
        }

        if (_previous.Count > contacts.Count)
        {
            _removeScratch.Clear();
            foreach (var id in _previous.Keys)
            {
                var found = false;
                for (var i = 0; i < contacts.Count; i++)
                {
                    if (contacts[i].ContactId == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    _removeScratch.Add(id);
            }

            foreach (var id in _removeScratch)
                _previous.Remove(id);
        }

        return _deltas;
    }

    public void Reset()
    {
        _previous.Clear();
        _deltas.Clear();
    }
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
