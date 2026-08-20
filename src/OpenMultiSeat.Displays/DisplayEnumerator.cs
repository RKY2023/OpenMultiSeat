using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using System.Runtime.InteropServices;

namespace OpenMultiSeat.Displays;

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
            uint pathCount = 0, modeCount = 0;

            if (NativeMethods.GetDisplayConfigBufferSizes(
                NativeMethods.QueryDisplayConfigFlags.AllPaths,
                out pathCount, out modeCount) != NativeMethods.ErrorSuccess)
            {
                _logger.LogWarning("Failed to get display config buffer sizes");
                return displays;
            }

            if (pathCount == 0)
            {
                _logger.LogInformation("No displays found");
                return displays;
            }

            var paths = new NativeMethods.DisplayConfigPathInfo[pathCount];
            var modes = new NativeMethods.DisplayConfigModeInfo[modeCount];

            if (NativeMethods.QueryDisplayConfig(
                NativeMethods.QueryDisplayConfigFlags.AllPaths,
                ref pathCount, paths,
                ref modeCount, modes,
                IntPtr.Zero) != NativeMethods.ErrorSuccess)
            {
                _logger.LogWarning("Failed to query display config");
                return displays;
            }

            for (int i = 0; i < pathCount; i++)
            {
                var display = CreateDisplayFromPath(paths[i], modes);
                if (display != null)
                {
                    displays.Add(display);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating displays");
        }

        return displays;
    }

    private Display? CreateDisplayFromPath(
        NativeMethods.DisplayConfigPathInfo path,
        NativeMethods.DisplayConfigModeInfo[] modes)
    {
        try
        {
            var targetMode = modes.FirstOrDefault(m =>
                m.Id == path.TargetInfo.Id &&
                m.InfoType == NativeMethods.DisplayConfigModeInfoType.Target);

            if (targetMode.InfoType != NativeMethods.DisplayConfigModeInfoType.Target)
                return null;

            var sourceMode = modes.FirstOrDefault(m =>
                m.Id == path.SourceInfo.Id &&
                m.InfoType == NativeMethods.DisplayConfigModeInfoType.Source);

            var displayName = GetDisplayName(path.TargetInfo.AdapterId, path.TargetInfo.Id);

            var display = new Display
            {
                DisplayId = $"DISPLAY_{path.TargetInfo.Id}",
                DeviceName = displayName ?? $"Display {path.TargetInfo.Id}",
                Width = targetMode.ModeInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.CX,
                Height = targetMode.ModeInfo.TargetMode.TargetVideoSignalInfo.ActiveSize.CY,
                RefreshRate = targetMode.ModeInfo.TargetMode.TargetVideoSignalInfo.VSyncFreq.Numerator /
                              targetMode.ModeInfo.TargetMode.TargetVideoSignalInfo.VSyncFreq.Denominator,
                PositionX = (int)sourceMode.ModeInfo.SourceMode.Position.X,
                PositionY = (int)sourceMode.ModeInfo.SourceMode.Position.Y,
                IsPrimary = (path.Flags & NativeMethods.DisplayConfigPathInfoFlags.PathPrimary) != 0,
                IsConnected = (path.TargetInfo.OutputTechnology !=
                              NativeMethods.DisplayConfigVideoOutputTechnology.Other),
                ConnectionType = path.TargetInfo.OutputTechnology.ToString()
            };

            return display;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating display from path");
            return null;
        }
    }

    private string? GetDisplayName(NativeMethods.Luid adapterId, uint targetId)
    {
        try
        {
            var targetName = new NativeMethods.DisplayConfigTargetDeviceName
            {
                Header = new NativeMethods.DisplayConfigDeviceInfoHeader
                {
                    Type = NativeMethods.DisplayConfigDeviceInfoType.GetTargetName,
                    Size = (uint)Marshal.SizeOf<NativeMethods.DisplayConfigTargetDeviceName>(),
                    AdapterId = adapterId,
                    Id = targetId
                }
            };

            if (NativeMethods.DisplayConfigGetDeviceInfo(ref targetName.Header) == NativeMethods.ErrorSuccess)
            {
                return targetName.MonitorFriendlyDeviceName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting display name");
        }

        return null;
    }
}

internal static class NativeMethods
{
    public const uint ErrorSuccess = 0;

    [Flags]
    public enum QueryDisplayConfigFlags : uint
    {
        AllPaths = 1,
        OnlyActivePaths = 2
    }

    [Flags]
    public enum DisplayConfigPathInfoFlags : uint
    {
        PathActive = 1,
        PathPrimary = 4
    }

    public enum DisplayConfigModeInfoType : uint
    {
        Source = 1,
        Target = 2
    }

    public enum DisplayConfigVideoOutputTechnology : uint
    {
        Other = 0xffffffff,
        Hdmi = 0,
        Analog = 1,
        Dvi = 2,
        Lvds = 3,
        Dport = 4,
        Sdtvdongle = 5
    }

    public enum DisplayConfigDeviceInfoType : uint
    {
        GetSourceName = 1,
        GetTargetName = 2,
        GetTargetPreferredMode = 3,
        GetAdapterName = 4,
        GetMonitorDescriptor = 5,
        GetMonitorColorSpace = 6
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public uint X;
        public uint Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Size
    {
        public uint CX;
        public uint CY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rational
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigVideoSignalInfo
    {
        public Rational PixelRate;
        public Rational HSyncFreq;
        public Rational VSyncFreq;
        public Size ActiveSize;
        public Size TotalSize;
        public uint VideoStandard;
        public DisplayConfigScanLineOrdering ScanLineOrdering;
    }

    public enum DisplayConfigScanLineOrdering : uint
    {
        Progressive = 1,
        Interlaced = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigTargetMode
    {
        public DisplayConfigVideoSignalInfo TargetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigSourceMode
    {
        public uint Width;
        public uint Height;
        public DisplayConfigPixelFormat PixelFormat;
        public Point Position;
    }

    public enum DisplayConfigPixelFormat : uint
    {
        Format8bit = 1,
        Format16bit = 2,
        Format32bit = 3,
        FormatNative = 4
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DisplayConfigModeInfoUnion
    {
        [FieldOffset(0)]
        public DisplayConfigSourceMode SourceMode;

        [FieldOffset(0)]
        public DisplayConfigTargetMode TargetMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigModeInfo
    {
        public DisplayConfigModeInfoType InfoType;
        public uint Id;
        public Luid AdapterId;
        public DisplayConfigModeInfoUnion ModeInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigRatioInfo
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigRotation
    {
        public uint Rotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigTargetDeviceNameFlags
    {
        public uint FriendlyNameFromEDID;
        public uint Edid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigDeviceInfoHeader
    {
        public DisplayConfigDeviceInfoType Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DisplayConfigTargetDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;
        public DisplayConfigTargetDeviceNameFlags Flags;
        public DisplayConfigVideoOutputTechnology OutputTechnology;
        public ushort EdidConnectorType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string MonitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string MonitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public DisplayConfigPathInfoFlags StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public DisplayConfigVideoOutputTechnology OutputTechnology;
        public DisplayConfigRotation Rotation;
        public DisplayConfigRatioInfo ScalingPercentage;
        public DisplayConfigPathInfoFlags RefreshRateMode;
        public DisplayConfigPathInfoFlags StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathInfo
    {
        public DisplayConfigPathSourceInfo SourceInfo;
        public DisplayConfigPathTargetInfo TargetInfo;
        public DisplayConfigPathInfoFlags Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetDisplayConfigBufferSizes(
        QueryDisplayConfigFlags flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint QueryDisplayConfig(
        QueryDisplayConfigFlags flags,
        ref uint numPathArrayElements,
        DisplayConfigPathInfo[] pathArray,
        ref uint numModeInfoArrayElements,
        DisplayConfigModeInfo[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint DisplayConfigGetDeviceInfo(ref DisplayConfigDeviceInfoHeader deviceInfo);
}
