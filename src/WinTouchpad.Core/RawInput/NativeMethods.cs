using System.Runtime.InteropServices;

namespace WinTouchpad.Core.RawInput;

/// <summary>
/// Win32 / HID P/Invoke used by <see cref="TouchpadRawInput"/>.
/// Structures follow emoacht/RawInput.Touchpad (MIT).
/// </summary>
internal static class NativeMethods
{
    public const int WM_INPUT = 0x00FF;
    public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;

    public const uint RIM_TYPEHID = 2;
    public const uint RID_INPUT = 0x10000003;
    public const uint RIDI_PREPARSEDDATA = 0x20000005;
    public const uint RIDI_DEVICEINFO = 0x2000000b;

    /// <summary>RIDEV_INPUTSINK | RIDEV_DEVICENOTIFY — receive in background + device change.</summary>
    public const uint RidEvInputSinkAndNotify = 0x00002100;

    public const ushort HidUsagePageDigitizer = 0x000D;
    public const ushort HidUsageTouchpad = 0x0005;

    public const uint HidPStatusSuccess = 0x00110000;

    [DllImport("User32", SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        [Out] RAWINPUTDEVICELIST[]? pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        ref RID_DEVICE_INFO pData,
        ref uint pcbSize);

    [DllImport("Hid.dll", SetLastError = true)]
    public static extern uint HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

    [DllImport("Hid.dll", CharSet = CharSet.Auto)]
    public static extern uint HidP_GetValueCaps(
        HIDP_REPORT_TYPE reportType,
        [Out] HIDP_VALUE_CAPS[] valueCaps,
        ref ushort valueCapsLength,
        IntPtr preparsedData);

    [DllImport("Hid.dll", CharSet = CharSet.Auto)]
    public static extern uint HidP_GetUsageValue(
        HIDP_REPORT_TYPE reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        IntPtr preparsedData,
        IntPtr report,
        uint reportLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWHID
    {
        public uint dwSizeHid;
        public uint dwCount;
        public IntPtr bRawData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUT
    {
        public RAWINPUTHEADER Header;
        public RAWHID Hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RID_DEVICE_INFO
    {
        public uint cbSize;
        public uint dwType;
        public RID_DEVICE_INFO_HID hid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RID_DEVICE_INFO_HID
    {
        public uint dwVendorId;
        public uint dwProductId;
        public uint dwVersionNumber;
        public ushort usUsagePage;
        public ushort usUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;

        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    public enum HIDP_REPORT_TYPE
    {
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_VALUE_CAPS
    {
        public ushort UsagePage;
        public byte ReportID;
        [MarshalAs(UnmanagedType.U1)] public bool IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        [MarshalAs(UnmanagedType.U1)] public bool IsRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsStringRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsDesignatorRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsAbsolute;
        [MarshalAs(UnmanagedType.U1)] public bool HasNull;
        public byte Reserved;
        public ushort BitSize;
        public ushort ReportCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] Reserved2;
        public uint UnitsExp;
        public uint Units;
        public int LogicalMin;
        public int LogicalMax;
        public int PhysicalMin;
        public int PhysicalMax;
        public ushort UsageMin;
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;
        public ushort DataIndexMax;

        public readonly ushort Usage => UsageMin;
    }
}