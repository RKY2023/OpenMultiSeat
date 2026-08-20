using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenMultiSeat.Devices;

public interface IHidDeviceEnumerator
{
    Task<IReadOnlyList<InputDevice>> EnumerateKeyboardsAsync();
    Task<IReadOnlyList<InputDevice>> EnumerateMiceAsync();
    Task<IReadOnlyList<InputDevice>> EnumerateAllDevicesAsync();
}

public class HidDeviceEnumerator : IHidDeviceEnumerator
{
    private readonly ILogger<HidDeviceEnumerator> _logger;
    private readonly IDevicePersistence _persistence;

    public HidDeviceEnumerator(ILogger<HidDeviceEnumerator> logger, IDevicePersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    public async Task<IReadOnlyList<InputDevice>> EnumerateKeyboardsAsync()
    {
        return await EnumerateDevicesOfTypeAsync(InputDeviceType.Keyboard);
    }

    public async Task<IReadOnlyList<InputDevice>> EnumerateMiceAsync()
    {
        return await EnumerateDevicesOfTypeAsync(InputDeviceType.Mouse);
    }

    public async Task<IReadOnlyList<InputDevice>> EnumerateAllDevicesAsync()
    {
        return await Task.Run(async () => await EnumerateDevices());
    }

    private async Task<IReadOnlyList<InputDevice>> EnumerateDevicesOfTypeAsync(InputDeviceType type)
    {
        var all = await EnumerateDevices();
        return all.Where(d => d.Type == type).ToList();
    }

    private async Task<List<InputDevice>> EnumerateDevices()
    {
        var devices = new List<InputDevice>();

        try
        {
            uint numDevices = 0;
            uint size = (uint)Marshal.SizeOf<NativeMethods.RawInputDeviceList>();

            // One Win32_PnPEntity query for the whole scan, reused for every device's friendly-
            // name lookup below, instead of a separate WMI round-trip per device.
            var pnpEntities = await GetAllPnpEntitiesAsync();

            if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref numDevices, size) == 0)
            {
                if (numDevices == 0)
                {
                    _logger.LogInformation("No input devices found");
                    return devices;
                }

                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(size * numDevices));

                try
                {
                    if (NativeMethods.GetRawInputDeviceList(pRawInputDeviceList, ref numDevices, size) == numDevices)
                    {
                        for (int i = 0; i < numDevices; i++)
                        {
                            var offset = i * size;
                            var deviceList = Marshal.PtrToStructure<NativeMethods.RawInputDeviceList>(
                                pRawInputDeviceList + (int)offset);

                            var device = await GetDeviceInfoAsync(deviceList.Device, (NativeMethods.RawInputDeviceType)deviceList.Type, pnpEntities);
                            if (device != null)
                            {
                                devices.Add(device);
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating devices");
        }

        return devices;
    }

    private async Task<InputDevice?> GetDeviceInfoAsync(
        IntPtr deviceHandle,
        NativeMethods.RawInputDeviceType type,
        IReadOnlyList<(string PnpDeviceId, string? Name, string? Manufacturer)> pnpEntities)
    {
        try
        {
            uint nameSize = 0;

            if (NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RidDeviceInfoDevicename,
                IntPtr.Zero, ref nameSize) != 0)
            {
                return null;
            }

            var nameBuf = new StringBuilder((int)nameSize);
            if (NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RidDeviceInfoDevicename,
                nameBuf, ref nameSize) == 0)
            {
                return null;
            }

            string devicePath = nameBuf.ToString();
            string hardwareId = ExtractHardwareId(devicePath);
            var vidPid = ExtractVidPid(devicePath);

            var stableId = await _persistence.GenerateStableIdAsync(hardwareId);
            var (wmiName, wmiManufacturer) = FindFriendlyName(hardwareId, pnpEntities);

            var device = new InputDevice
            {
                DeviceId = stableId,
                HardwareId = hardwareId,
                Type = type switch
                {
                    NativeMethods.RawInputDeviceType.Keyboard => InputDeviceType.Keyboard,
                    NativeMethods.RawInputDeviceType.Mouse => InputDeviceType.Mouse,
                    _ => InputDeviceType.Other
                },
                // Prefer the real friendly name/manufacturer Windows already knows via
                // Win32_PnPEntity. Fall back to parsing the raw device instance path
                // (e.g. "VID_046D PID_C542 Col01") only when WMI has nothing for this
                // device — that fallback is a device path, not a product name, so it
                // reads as placeholder/dummy data even though it's real hardware.
                ProductName = wmiName ?? ExtractProductName(devicePath),
                Manufacturer = wmiManufacturer ?? ExtractManufacturer(devicePath),
                VendorId = vidPid.VendorId,
                ProductId = vidPid.ProductId
            };

            await _persistence.SaveDeviceAsync(new DeviceRecord
            {
                StableId = stableId,
                HardwareId = hardwareId,
                VendorId = vidPid.VendorId,
                ProductId = vidPid.ProductId,
                ProductName = device.ProductName,
                Manufacturer = device.Manufacturer,
                DeviceType = device.Type.ToString()
            });

            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device info");
            return null;
        }
    }

    /// <summary>
    /// Fetches every Win32_PnPEntity Windows currently knows about, once per scan — a single WMI
    /// round-trip reused for every device's friendly-name lookup below, instead of one query per
    /// device. No WHERE clause, so there's no user-influenced string built into the WQL query text
    /// (the earlier per-device version interpolated a device path fragment into a LIKE clause
    /// without escaping WQL's own wildcard characters, '%' and '_').
    /// </summary>
    private static Task<List<(string PnpDeviceId, string? Name, string? Manufacturer)>> GetAllPnpEntitiesAsync()
    {
        return Task.Run(() =>
        {
            var entities = new List<(string, string?, string?)>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT PNPDeviceID, Name, Caption, Manufacturer FROM Win32_PnPEntity");

                foreach (ManagementBaseObject result in searcher.Get())
                {
                    using var entity = result;
                    var pnpDeviceId = entity["PNPDeviceID"] as string;
                    if (string.IsNullOrEmpty(pnpDeviceId))
                        continue;

                    var name = (entity["Caption"] as string) ?? (entity["Name"] as string);
                    var manufacturer = entity["Manufacturer"] as string;
                    entities.Add((pnpDeviceId, name, manufacturer));
                }
            }
            catch
            {
                // WMI unavailable or restricted — return whatever was gathered (possibly empty);
                // callers fall back to raw path parsing per device in that case.
            }

            return entities;
        });
    }

    /// <summary>
    /// Matches a device's PnP instance-ID fragment (the "5&amp;318818&amp;0&amp;0002"-style segment
    /// ExtractHardwareId pulls out of the raw input device path) against the batch of PnP entities
    /// fetched by <see cref="GetAllPnpEntitiesAsync"/>. Uses EndsWith rather than a bare substring
    /// match: Windows' PNPDeviceID convention is "Enumerator\HardwareID\InstanceID", and the
    /// fragment we have is exactly that trailing InstanceID segment — matching only at the end
    /// avoids picking up an unrelated PnP entity whose HardwareID segment happens to contain the
    /// same characters as another device's instance ID.
    /// </summary>
    private static (string? Name, string? Manufacturer) FindFriendlyName(
        string hardwareId,
        IReadOnlyList<(string PnpDeviceId, string? Name, string? Manufacturer)> pnpEntities)
    {
        if (string.IsNullOrWhiteSpace(hardwareId))
            return (null, null);

        foreach (var entity in pnpEntities)
        {
            if (entity.PnpDeviceId.EndsWith(hardwareId, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entity.Name))
            {
                return (entity.Name, entity.Manufacturer);
            }
        }

        return (null, null);
    }

    private string ExtractHardwareId(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return string.Empty;

        var parts = devicePath.Split('#');
        return parts.Length >= 3 ? parts[2] : devicePath;
    }

    private (string? VendorId, string? ProductId) ExtractVidPid(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return (null, null);

        var vidMatch = Regex.Match(devicePath, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
        var pidMatch = Regex.Match(devicePath, @"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);

        return (
            vidMatch.Success ? vidMatch.Groups[1].Value : null,
            pidMatch.Success ? pidMatch.Groups[1].Value : null
        );
    }

    private string ExtractProductName(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return "Unknown Device";

        var parts = devicePath.Split('#');
        if (parts.Length >= 2)
        {
            return parts[1].Replace("&", " ").Trim();
        }

        return "Unknown Device";
    }

    private string ExtractManufacturer(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return "Unknown";

        var parts = devicePath.Split('#');
        if (parts.Length >= 1)
        {
            return parts[0].Replace("\\\\?\\", "").Split('\\')[0];
        }

        return "Unknown";
    }
}
