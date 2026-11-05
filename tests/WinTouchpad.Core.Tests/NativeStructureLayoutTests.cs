using System.Reflection;
using System.Runtime.InteropServices;
using WinTouchpad.Core.Contacts;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class NativeStructureLayoutTests
{
    [Fact]
    public void RidDeviceInfo_MatchesWindowsAbiSizeAndOffsets()
    {
        var assembly = typeof(ContactFrame).Assembly;
        var infoType = assembly.GetType("WinTouchpad.Core.RawInput.NativeMethods+RID_DEVICE_INFO", throwOnError: true)!;
        var unionType = assembly.GetType("WinTouchpad.Core.RawInput.NativeMethods+RID_DEVICE_INFO_UNION", throwOnError: true)!;
        var keyboardType = assembly.GetType("WinTouchpad.Core.RawInput.NativeMethods+RID_DEVICE_INFO_KEYBOARD", throwOnError: true)!;

        Assert.Equal(24, Marshal.SizeOf(keyboardType));
        Assert.Equal(24, Marshal.SizeOf(unionType));
        Assert.Equal(32, Marshal.SizeOf(infoType));
        Assert.Equal(8, Marshal.OffsetOf(infoType, "data").ToInt32());
    }
}
