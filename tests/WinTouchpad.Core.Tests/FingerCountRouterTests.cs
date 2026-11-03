using WinTouchpad.Core.Contacts;
using WinTouchpad.Core.Routing;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class FingerCountRouterTests
{
    private readonly FingerCountRouter _router = new();

    [Theory]
    [InlineData(0, FingerRoute.None)]
    [InlineData(1, FingerRoute.IgnoredLowCount)]
    [InlineData(2, FingerRoute.IgnoredLowCount)]
    [InlineData(3, FingerRoute.ThreeFinger)]
    [InlineData(4, FingerRoute.FourFingerReserved)]
    [InlineData(5, FingerRoute.IgnoredHighCount)]
    public void Route_ByCount_ReturnsExpected(int count, FingerRoute expected)
    {
        Assert.Equal(expected, _router.Route(count));
    }

    [Fact]
    public void Route_Frame_UsesFingerCount()
    {
        var contacts = new[]
        {
            new TouchpadContact(1, 10, 20),
            new TouchpadContact(2, 30, 40),
            new TouchpadContact(3, 50, 60)
        };
        var frame = new ContactFrame(contacts, reportedContactCount: 3, deviceHandle: IntPtr.Zero);
        Assert.Equal(FingerRoute.ThreeFinger, _router.Route(frame));
    }
}