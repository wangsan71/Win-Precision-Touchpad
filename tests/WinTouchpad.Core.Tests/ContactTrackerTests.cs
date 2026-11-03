using WinTouchpad.Core.Contacts;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class ContactTrackerTests
{
    [Fact]
    public void Update_ComputesDeltas_AndMarksNewContacts()
    {
        var tracker = new ContactTracker();
        var first = tracker.Update(new[] { new TouchpadContact(1, 100, 200) });
        Assert.Single(first);
        Assert.True(first[0].IsNew);
        Assert.Equal(0, first[0].Dx);

        var second = tracker.Update(new[] { new TouchpadContact(1, 110, 190) });
        Assert.Single(second);
        Assert.False(second[0].IsNew);
        Assert.Equal(10, second[0].Dx);
        Assert.Equal(-10, second[0].Dy);
    }
}