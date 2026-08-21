using System.Runtime.InteropServices;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Minimal Win32 Raw Input P/Invoke surface for WorkplaceTileLayoutWindow's "press a key or move
/// a mouse to identify it" Indicate-device behavior. Deliberately narrow: registers for keyboard
/// and mouse raw input on this window only (no RIDEV_INPUTSINK), so events are delivered only
/// while the Tile Layout window itself has focus -- not a global keylogger/mouse-hook, and nothing
/// is captured once the window loses focus or closes (explicitly unregistered on Closed).
///
/// Only RAWINPUTHEADER is parsed (via RID_HEADER), not the full RAWINPUT union (RAWKEYBOARD/
/// RAWMOUSE/RAWHID payloads) -- the header's hDevice is all that's needed to know *which physical
/// device* generated the event; the actual key/movement data itself is discarded unread.
///
/// RAWINPUTDEVICE and RAWINPUTHEADER below use the standard, widely-published Win32 layouts (the
/// same struct shapes used in effectively every Raw Input sample); these are simple, unlike the
/// DISPLAYCONFIG family that caused this project's two prior P/Invoke struct-layout crashes (see
/// docs/known-issues.md), which is why raw input was chosen as feasible to add here at all.
/// </summary>
internal static class RawInputInterop
{
    public const int WM_INPUT = 0x00FF;

    private const uint RIDEV_REMOVE = 0x00000001;
    private const uint RID_HEADER = 0x10000005;
    public const uint RIDI_DEVICENAME = 0x20000007;

    private const ushort HidUsagePageGeneric = 0x01;
    private const ushort HidUsageMouse = 0x02;
    private const ushort HidUsageKeyboard = 0x06;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, System.Text.StringBuilder pData, ref uint pcbData);

    /// <summary>Registers this window (by HWND) to receive WM_INPUT for keyboard and mouse
    /// devices while it has focus. Returns false if registration failed (rare -- e.g. another
    /// process already holds an exclusive raw input registration); the window still functions
    /// without live indicate in that case, just without the auto-highlight-on-input behavior.</summary>
    public static bool Register(IntPtr hwnd)
    {
        var devices = new[]
        {
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageKeyboard, Flags = 0, Target = hwnd },
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageMouse, Flags = 0, Target = hwnd }
        };

        return RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    /// <summary>Undoes Register -- called on window Closed so no raw input keeps targeting a
    /// destroyed HWND.</summary>
    public static void Unregister()
    {
        var devices = new[]
        {
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageKeyboard, Flags = RIDEV_REMOVE, Target = IntPtr.Zero },
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageMouse, Flags = RIDEV_REMOVE, Target = IntPtr.Zero }
        };

        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    /// <summary>Extracts just the originating device handle from a WM_INPUT message's lParam, or
    /// IntPtr.Zero if the header couldn't be read.</summary>
    public static IntPtr GetSourceDevice(IntPtr hRawInput)
    {
        var headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
        var size = headerSize;
        var buffer = Marshal.AllocHGlobal((int)headerSize);
        try
        {
            if (GetRawInputData(hRawInput, RID_HEADER, buffer, ref size, headerSize) == unchecked((uint)-1))
                return IntPtr.Zero;

            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
            return header.Device;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Resolves a raw input device handle to its Windows device path (e.g.
    /// "\\?\HID#VID_...") -- the same string HidDeviceEnumerator reads via
    /// GetRawInputDeviceInfo/RIDI_DEVICENAME when first enumerating devices. Null if the lookup
    /// fails (e.g. the device was unplugged between the event and this call).</summary>
    public static string? GetDevicePath(IntPtr hDevice)
    {
        uint size = 0;
        if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, null!, ref size) != 0)
            return null;
        if (size == 0)
            return null;

        var buffer = new System.Text.StringBuilder((int)size);
        return GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, buffer, ref size) == unchecked((uint)-1)
            ? null
            : buffer.ToString();
    }

    /// <summary>Mirrors HidDeviceEnumerator.ExtractHardwareId: a raw input device path is
    /// "Enumerator#HardwareID#InstanceID#{classGuid}"; the third '#'-delimited segment is the
    /// instance-ID fragment DevicePersistence's StableId is generated from.</summary>
    public static string ExtractHardwareId(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return string.Empty;

        var parts = devicePath.Split('#');
        return parts.Length >= 3 ? parts[2] : devicePath;
    }
}
