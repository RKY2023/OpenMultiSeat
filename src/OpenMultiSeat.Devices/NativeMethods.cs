using System.Runtime.InteropServices;
using System.Text;

namespace OpenMultiSeat.Devices;

internal static partial class NativeMethods
{
    private const string SetupApiDll = "setupapi.dll";
    private const string Kernel32Dll = "kernel32.dll";
    private const string User32Dll = "user32.dll";

    #region SetupAPI

    [Flags]
    public enum DiGetClassFlags : uint
    {
        Default = 0x00000001,
        Present = 0x00000002,
        AllClasses = 0x00000004,
        Profile = 0x00000008,
        DeviceInterface = 0x00000010
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpDeviceInterfaceData
    {
        public uint CbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SpDeviceInterfaceDetailData
    {
        public uint CbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpDevinfoData
    {
        public uint CbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    public static readonly Guid HidClassGuid = new("4d1e55b2-f16f-11cf-88cb-001111000030");

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        IntPtr enumerator,
        IntPtr hwndParent,
        DiGetClassFlags flags);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        ref SpDeviceInterfaceDetailData deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        IntPtr deviceInfoData);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SpDevinfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [DllImport(Kernel32Dll, SetLastError = true)]
    public static extern bool SetupDiGetDeviceInstanceId(
        IntPtr deviceInfoSet,
        ref SpDevinfoData deviceInfoData,
        StringBuilder deviceInstanceId,
        uint deviceInstanceIdSize,
        out uint requiredSize);

    #endregion

    #region Raw Input

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDeviceList
    {
        public IntPtr Device;
        public uint Type;
    }

    // Real Win32 RIM_TYPE* values (WinUser.h): RIM_TYPEMOUSE=0, RIM_TYPEKEYBOARD=1, RIM_TYPEHID=2.
    // This previously read { Mouse = 1, Keyboard = 2, Hid = 4 } with a [Flags] attribute -- wrong on
    // both counts: these three are mutually-exclusive tag values reported in
    // RAWINPUTDEVICELIST.dwType, not a bitmask, and the numbers didn't match the real constants at
    // all. The practical effect: every real mouse (native dwType=0) failed to match any declared
    // member and fell through to InputDeviceType.Other; every real keyboard (dwType=1) matched the
    // old Mouse=1 and was saved as a mouse; every generic HID collection (dwType=2) matched the old
    // Keyboard=2 and was saved as a keyboard. This is very likely the actual source of the
    // "DeviceRecord.DeviceType is unreliable for keyboards/mice" behavior worked around elsewhere in
    // this codebase (AssignDeviceToSeatWindow's manual radio button, WorkplaceTileLayoutWindow's
    // list-membership-first TileKind derivation) -- not an inherent Windows limitation, a wrong enum.
    public enum RawInputDeviceType : uint
    {
        Mouse = 0,
        Keyboard = 1,
        Hid = 2
    }

    [DllImport(User32Dll, SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        IntPtr pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    [DllImport(User32Dll, SetLastError = true)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbData);

    [DllImport(User32Dll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        StringBuilder pData,
        ref uint pcbData);

    public const uint RidDeviceInfo = 0x2000000b;
    public const uint RidDeviceInfoPreparsedata = 0x20000005;
    public const uint RidDeviceInfoDevicename = 0x20000007;

    #endregion
}
