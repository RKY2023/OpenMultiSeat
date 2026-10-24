using System.Runtime.InteropServices;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Minimal Win32 Raw Input P/Invoke surface for WorkplaceTileLayoutWindow's "press a key or move
/// a mouse to identify it" Indicate-device behavior.
///
/// Registers with RIDEV_INPUTSINK, so events are delivered whenever the Tile Layout window exists
/// (not only while it happens to be the literal Win32 foreground window). Without that flag this
/// was found to be unreliable in practice -- without RIDEV_INPUTSINK, Windows only delivers WM_INPUT
/// while the target window is the actual foreground window, a stricter condition than WPF-level
/// focus; something as small as this same window's own right-click context menu taking the
/// foreground for a moment is enough to miss events, which is what made this silently never fire in
/// testing even though registration itself succeeded and the parsing code was correct. The
/// trade-off: while this window is open, any physical key press or mouse move anywhere on the
/// system reaches WndProc, not just input aimed at this window. Still not a keylogger -- only
/// RAWINPUTHEADER is parsed (see below), the actual key/movement data is discarded unread, and
/// registration is undone the moment the window closes (Unregister, tied to Window.Closed).
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

    private const uint RIDEV_INPUTSINK = 0x00000100;
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

    // Two overloads of the SAME native function, mirroring HidDeviceEnumerator/NativeMethods.cs
    // exactly (down to the RIDI_DEVICENAME usage) rather than inventing a different call pattern:
    // the IntPtr overload for the first, size-query call (pData=IntPtr.Zero), the StringBuilder
    // overload for the second, real-fetch call. Passing `null` to a StringBuilder-marshaled
    // parameter for the size query (this file's original version) is not the same thing as
    // passing IntPtr.Zero to the IntPtr overload -- that mismatch was the actual bug behind
    // "indicate never fires for keyboard/mouse": the size query silently came back empty every
    // time, so GetDevicePath always returned null before ever reaching StableId resolution.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbData);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, System.Text.StringBuilder pData, ref uint pcbData);

    /// <summary>Registers this window (by HWND) to receive WM_INPUT for keyboard and mouse
    /// devices for as long as the window exists (RIDEV_INPUTSINK -- see the class doc comment for
    /// why plain focus-scoped registration proved unreliable). Returns false if registration failed
    /// (rare -- e.g. another process already holds an exclusive raw input registration); the window
    /// still functions without live indicate in that case, just without the auto-highlight-on-input
    /// behavior.</summary>
    public static bool Register(IntPtr hwnd)
    {
        var devices = new[]
        {
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageKeyboard, Flags = RIDEV_INPUTSINK, Target = hwnd },
            new RAWINPUTDEVICE { UsagePage = HidUsagePageGeneric, Usage = HidUsageMouse, Flags = RIDEV_INPUTSINK, Target = hwnd }
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
        if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref size) != 0)
            return null;
        if (size == 0)
            return null;

        var buffer = new System.Text.StringBuilder((int)size);
        return GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, buffer, ref size) == 0
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
