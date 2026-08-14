using OpenMultiSeat.Core;
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

                            var device = await GetDeviceInfoAsync(deviceList.Device, (NativeMethods.RawInputDeviceType)deviceList.Type);
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

    private async Task<InputDevice?> GetDeviceInfoAsync(IntPtr deviceHandle, NativeMethods.RawInputDeviceType type)
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
                ProductName = ExtractProductName(devicePath),
                Manufacturer = ExtractManufacturer(devicePath),
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
