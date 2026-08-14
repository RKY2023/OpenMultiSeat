using OpenMultiSeat.Core;
using System.Runtime.InteropServices;
using System.Text;

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

    public HidDeviceEnumerator(ILogger<HidDeviceEnumerator> logger)
    {
        _logger = logger;
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
        return await Task.Run(() => EnumerateDevices());
    }

    private async Task<IReadOnlyList<InputDevice>> EnumerateDevicesOfTypeAsync(InputDeviceType type)
    {
        return await Task.Run(() => EnumerateDevices().Where(d => d.Type == type).ToList());
    }

    private List<InputDevice> EnumerateDevices()
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

                            var device = GetDeviceInfo(deviceList.Device, (NativeMethods.RawInputDeviceType)deviceList.Type);
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

    private InputDevice? GetDeviceInfo(IntPtr deviceHandle, NativeMethods.RawInputDeviceType type)
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

            var device = new InputDevice
            {
                DeviceId = $"HID_{deviceHandle:X}",
                HardwareId = ExtractHardwareId(devicePath),
                Type = type switch
                {
                    NativeMethods.RawInputDeviceType.Keyboard => InputDeviceType.Keyboard,
                    NativeMethods.RawInputDeviceType.Mouse => InputDeviceType.Mouse,
                    _ => InputDeviceType.Other
                },
                ProductName = ExtractProductName(devicePath),
                Manufacturer = ExtractManufacturer(devicePath)
            };

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
