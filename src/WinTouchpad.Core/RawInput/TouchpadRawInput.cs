using System.Runtime.InteropServices;
using WinTouchpad.Core.Contacts;

namespace WinTouchpad.Core.RawInput;

/// <summary>
/// Registers for Precision Touchpad Raw Input and parses HID contact reports.
/// <para>
/// Approach adapted from emoacht/RawInput.Touchpad (MIT) and
/// ClementGre/ThreeFingerDragOnWindows (MIT). Not a verbatim copy — cleaned for this project.
/// </para>
/// HID TLCs: Usage Page 0x0D (Digitizers), Usage 0x05 (Touch Pad).
/// </summary>
public static class TouchpadRawInput
{
    public const int WmInput = NativeMethods.WM_INPUT;
    public const int WmInputDeviceChange = NativeMethods.WM_INPUT_DEVICE_CHANGE;

    /// <summary>True if any connected HID device reports as a Precision Touchpad.</summary>
    public static bool Exists()
    {
        uint deviceListCount = 0;
        uint size = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICELIST>();

        if (NativeMethods.GetRawInputDeviceList(null, ref deviceListCount, size) != 0)
            return false;

        var devices = new NativeMethods.RAWINPUTDEVICELIST[deviceListCount];
        if (NativeMethods.GetRawInputDeviceList(devices, ref deviceListCount, size) != deviceListCount)
            return false;

        foreach (var device in devices)
        {
            if (device.dwType != NativeMethods.RIM_TYPEHID)
                continue;

            uint infoSize = 0;
            if (NativeMethods.GetRawInputDeviceInfo(device.hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref infoSize) != 0)
                continue;

            var info = new NativeMethods.RID_DEVICE_INFO { cbSize = infoSize };
            if (NativeMethods.GetRawInputDeviceInfo(device.hDevice, NativeMethods.RIDI_DEVICEINFO, ref info, ref infoSize) == unchecked((uint)-1))
                continue;

            if (info.hid.usUsagePage == NativeMethods.HidUsagePageDigitizer &&
                info.hid.usUsage == NativeMethods.HidUsageTouchpad)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Register the given HWND to receive WM_INPUT for Precision Touchpad, including when not focused.
    /// </summary>
    public static bool Register(IntPtr hwndTarget)
    {
        var device = new NativeMethods.RAWINPUTDEVICE
        {
            usUsagePage = NativeMethods.HidUsagePageDigitizer,
            usUsage = NativeMethods.HidUsageTouchpad,
            dwFlags = NativeMethods.RidEvInputSinkAndNotify,
            hwndTarget = hwndTarget
        };

        return NativeMethods.RegisterRawInputDevices(
            new[] { device },
            1,
            (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>());
    }

    /// <summary>
    /// Parse a WM_INPUT lParam into contacts. Returns null on failure / empty.
    /// </summary>
    public static ContactFrame? ParseInput(IntPtr lParam)
    {
        uint rawInputSize = 0;
        uint headerSize = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>();

        if (NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref rawInputSize, headerSize) != 0)
            return null;

        NativeMethods.RAWINPUT rawInput;
        byte[] rawHidRawData;
        IntPtr rawInputPointer = IntPtr.Zero;

        try
        {
            rawInputPointer = Marshal.AllocHGlobal((int)rawInputSize);
            if (NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, rawInputPointer, ref rawInputSize, headerSize) != rawInputSize)
                return null;

            rawInput = Marshal.PtrToStructure<NativeMethods.RAWINPUT>(rawInputPointer);
            var rawInputData = new byte[rawInputSize];
            Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

            rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];
            int offset = (int)rawInputSize - rawHidRawData.Length;
            if (offset < 0 || rawHidRawData.Length == 0)
                return null;

            Buffer.BlockCopy(rawInputData, offset, rawHidRawData, 0, rawHidRawData.Length);
        }
        finally
        {
            if (rawInputPointer != IntPtr.Zero)
                Marshal.FreeHGlobal(rawInputPointer);
        }

        IntPtr reportPtr = Marshal.AllocHGlobal(rawHidRawData.Length);
        Marshal.Copy(rawHidRawData, 0, reportPtr, rawHidRawData.Length);
        IntPtr preparsedPtr = IntPtr.Zero;

        try
        {
            uint preparsedSize = 0;
            if (NativeMethods.GetRawInputDeviceInfo(rawInput.Header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref preparsedSize) != 0)
                return null;

            preparsedPtr = Marshal.AllocHGlobal((int)preparsedSize);
            if (NativeMethods.GetRawInputDeviceInfo(rawInput.Header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, preparsedPtr, ref preparsedSize) != preparsedSize)
                return null;

            if (NativeMethods.HidP_GetCaps(preparsedPtr, out var caps) != NativeMethods.HidPStatusSuccess)
                return null;

            ushort valueCapsLength = caps.NumberInputValueCaps;
            if (valueCapsLength == 0)
                return null;

            var valueCaps = new NativeMethods.HIDP_VALUE_CAPS[valueCapsLength];
            if (NativeMethods.HidP_GetValueCaps(
                    NativeMethods.HIDP_REPORT_TYPE.HidP_Input,
                    valueCaps,
                    ref valueCapsLength,
                    preparsedPtr) != NativeMethods.HidPStatusSuccess)
            {
                return null;
            }

            uint contactCount = 0;
            var builder = new TouchpadContactBuilder();
            var contacts = new List<TouchpadContact>();

            foreach (var valueCap in valueCaps.OrderBy(x => x.LinkCollection))
            {
                if (NativeMethods.HidP_GetUsageValue(
                        NativeMethods.HIDP_REPORT_TYPE.HidP_Input,
                        valueCap.UsagePage,
                        valueCap.LinkCollection,
                        valueCap.Usage,
                        out uint value,
                        preparsedPtr,
                        reportPtr,
                        (uint)rawHidRawData.Length) != NativeMethods.HidPStatusSuccess)
                {
                    continue;
                }

                // Windows Precision Touchpad required HID usages:
                // https://learn.microsoft.com/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections
                if (valueCap.LinkCollection == 0)
                {
                    switch (valueCap.UsagePage, valueCap.Usage)
                    {
                        case (0x0D, 0x54): // Contact Count
                            contactCount = value;
                            break;
                    }
                }
                else
                {
                    switch (valueCap.UsagePage, valueCap.Usage)
                    {
                        case (0x0D, 0x51): // Contact ID
                            builder.ContactId = (int)value;
                            break;
                        case (0x01, 0x30): // X
                            builder.X = (int)value;
                            break;
                        case (0x01, 0x31): // Y
                            builder.Y = (int)value;
                            break;
                    }
                }

                if (builder.TryCreate(out var contact))
                {
                    contacts.Add(contact);
                    if (contactCount > 0 && contacts.Count >= contactCount)
                        break;
                    builder.Clear();
                }
            }

            if (contacts.Count == 0 && contactCount == 0)
                return null;

            return new ContactFrame(contacts, contactCount, rawInput.Header.hDevice);
        }
        finally
        {
            Marshal.FreeHGlobal(reportPtr);
            if (preparsedPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(preparsedPtr);
        }
    }
}