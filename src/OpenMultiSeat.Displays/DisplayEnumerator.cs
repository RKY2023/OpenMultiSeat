using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using System.Management;
using System.Runtime.InteropServices;

namespace OpenMultiSeat.Displays;

/// <summary>
/// Enumerates displays via the classic EnumDisplayMonitors/GetMonitorInfo/EnumDisplaySettings
/// GDI APIs, not the newer DisplayConfig API family (QueryDisplayConfig etc.). This replaced an
/// earlier QueryDisplayConfig-based implementation that turned out to be doubly broken: first a
/// struct-size mismatch caused genuine heap corruption on every call (fixed), and after that fix
/// QueryDisplayConfig still returned an all-zeroed path even though GetDisplayConfigBufferSizes
/// correctly reported exactly one active path — a deeper marshaling issue that wasn't worth
/// continuing to chase given a much simpler, far more commonly used API does the same job
/// correctly (verified directly against this environment: System.Windows.Forms.Screen.AllScreens,
/// which is a thin wrapper over these same GDI calls, correctly found the real monitor here).
/// Trade-off: no connector/output-technology type (HDMI/DP/etc.) — GDI doesn't expose that, only
/// DisplayConfig does. Everything else the Display model needs (name, resolution, refresh rate,
/// position, primary flag) is available and simpler to get right.
/// </summary>
public class DisplayEnumerator : IDisplayEnumerator
{
    private readonly ILogger<DisplayEnumerator> _logger;

    public DisplayEnumerator(ILogger<DisplayEnumerator> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<Display>> EnumerateDisplaysAsync()
    {
        return await Task.Run(() => EnumerateDisplays());
    }

    private List<Display> EnumerateDisplays()
    {
        var displays = new List<Display>();

        try
        {
            var monitorHandles = new List<IntPtr>();

            bool Callback(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData)
            {
                monitorHandles.Add(hMonitor);
                return true; // keep enumerating
            }

            if (!NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero))
            {
                _logger.LogWarning("EnumDisplayMonitors failed");
                return displays;
            }

            if (monitorHandles.Count == 0)
            {
                _logger.LogInformation("No displays found");
                return displays;
            }

            var friendlyNames = GetFriendlyNamesFromWmi();
            var friendlyNameIndex = 0;

            foreach (var hMonitor in monitorHandles)
            {
                var display = CreateDisplayFromMonitor(hMonitor, friendlyNames, ref friendlyNameIndex);
                if (display != null)
                    displays.Add(display);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating displays");
        }

        return displays;
    }

    private Display? CreateDisplayFromMonitor(IntPtr hMonitor, IReadOnlyList<string> friendlyNames, ref int friendlyNameIndex)
    {
        try
        {
            var info = new NativeMethods.MonitorInfoEx
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
            };

            if (!NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                _logger.LogWarning("GetMonitorInfo failed for a monitor handle");
                return null;
            }

            var width = (uint)Math.Max(0, info.rcMonitor.Right - info.rcMonitor.Left);
            var height = (uint)Math.Max(0, info.rcMonitor.Bottom - info.rcMonitor.Top);
            var isPrimary = (info.dwFlags & NativeMethods.MonitorInfoFPrimary) != 0;

            uint refreshRate = 0;
            var devMode = new NativeMethods.DevMode { dmSize = (short)Marshal.SizeOf<NativeMethods.DevMode>() };
            if (NativeMethods.EnumDisplaySettings(info.szDevice, NativeMethods.EnumCurrentSettings, ref devMode)
                && devMode.dmDisplayFrequency > 1) // 0/1 both mean "hardware default", not a real Hz value
            {
                refreshRate = (uint)devMode.dmDisplayFrequency;
            }

            // WMI's Win32_DesktopMonitor doesn't expose a key that reliably correlates to
            // \\.\DISPLAYn device names, so this matches by enumeration order — good enough for
            // the common single/dual-monitor case, not guaranteed correct for larger setups.
            var friendlyName = friendlyNameIndex < friendlyNames.Count ? friendlyNames[friendlyNameIndex] : null;
            friendlyNameIndex++;

            return new Display
            {
                DisplayId = info.szDevice,
                DeviceName = friendlyName ?? info.szDevice,
                Width = width,
                Height = height,
                RefreshRate = refreshRate,
                PositionX = info.rcMonitor.Left,
                PositionY = info.rcMonitor.Top,
                IsPrimary = isPrimary,
                IsConnected = true,
                ConnectionType = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating display from monitor handle");
            return null;
        }
    }

    private List<string> GetFriendlyNamesFromWmi()
    {
        var names = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Caption FROM Win32_DesktopMonitor");
            foreach (ManagementBaseObject result in searcher.Get())
            {
                using var monitor = result;
                var name = (monitor["Caption"] as string) ?? (monitor["Name"] as string);
                if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name, "Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                    names.Add(name);
            }
        }
        catch
        {
            // WMI unavailable or restricted — callers fall back to the raw device path string.
        }

        return names;
    }
}

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    public const uint MonitorInfoFPrimary = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MonitorInfoEx
    {
        public uint cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public const int EnumCurrentSettings = -1;

    // Standard Win32 DEVMODE layout. dmPositionX/dmPositionY/dmDisplayOrientation/
    // dmDisplayFixedOutput occupy the same offsets the real struct's union gives to printer-only
    // fields (dmOrientation/dmPaperSize/dmPaperLength/dmPaperWidth) — that's correct for display
    // devices, which is the only use here.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DevMode devMode);
}
