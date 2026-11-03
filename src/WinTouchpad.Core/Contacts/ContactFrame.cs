namespace WinTouchpad.Core.Contacts;

/// <summary>A single parsed Raw Input sample: reported contact count + contacts present in the report.</summary>
public sealed class ContactFrame
{
    public ContactFrame(IReadOnlyList<TouchpadContact> contacts, uint reportedContactCount, IntPtr deviceHandle)
    {
        Contacts = contacts;
        ReportedContactCount = reportedContactCount;
        DeviceHandle = deviceHandle;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<TouchpadContact> Contacts { get; }
    public uint ReportedContactCount { get; }
    public IntPtr DeviceHandle { get; }
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>Preferred finger count: max of reported count and contacts actually parsed.</summary>
    public int FingerCount => Math.Max((int)ReportedContactCount, Contacts.Count);
}