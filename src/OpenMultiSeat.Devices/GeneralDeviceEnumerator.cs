using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using System.Management;

namespace OpenMultiSeat.Devices;

public interface IGeneralDeviceEnumerator
{
    Task<IReadOnlyList<DeviceRecord>> EnumerateGeneralDevicesAsync();
}

/// <summary>
/// Enumerates non-HID-input devices (cameras, general USB peripherals, Bluetooth radios/devices)
/// via WMI's Win32_PnPEntity, complementing HidDeviceEnumerator's Raw-Input-based keyboard/mouse
/// scan. These are shown on the Devices page for visibility, but aren't assignable to a seat —
/// SeatManager.AssignDeviceToSeatAsync only accepts Keyboard/Mouse InputDeviceType values, and
/// there's no Core model concept of a seat "owning" a camera or a USB drive today. The Devices
/// page's assign action checks DeviceType and refuses these classes rather than letting an
/// InputDeviceType get force-picked for a device that isn't actually one.
/// </summary>
public class GeneralDeviceEnumerator : IGeneralDeviceEnumerator
{
    /// <summary>WMI's PNPClass values this enumerator surfaces — kept in sync with
    /// DevicesPage's non-assignable-class check.</summary>
    public static readonly IReadOnlyList<string> GeneralDeviceClasses = ["Camera", "Image", "USB", "Bluetooth"];

    private readonly ILogger<GeneralDeviceEnumerator> _logger;
    private readonly IDevicePersistence _persistence;

    public GeneralDeviceEnumerator(ILogger<GeneralDeviceEnumerator> logger, IDevicePersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    public Task<IReadOnlyList<DeviceRecord>> EnumerateGeneralDevicesAsync()
        => Task.Run(EnumerateAsync);

    private async Task<IReadOnlyList<DeviceRecord>> EnumerateAsync()
    {
        var devices = new List<DeviceRecord>();

        try
        {
            var classFilter = string.Join(" OR ", GeneralDeviceClasses.Select(c => $"PNPClass='{c}'"));
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Name, Caption, Manufacturer, PNPDeviceID, PNPClass FROM Win32_PnPEntity WHERE {classFilter}");

            foreach (ManagementBaseObject result in searcher.Get())
            {
                using var entity = result;
                var pnpDeviceId = entity["PNPDeviceID"] as string;
                if (string.IsNullOrEmpty(pnpDeviceId))
                    continue;

                var name = (entity["Caption"] as string) ?? (entity["Name"] as string) ?? "Unknown device";
                var manufacturer = entity["Manufacturer"] as string;
                var pnpClass = entity["PNPClass"] as string ?? "Other";

                var stableId = await _persistence.GenerateStableIdAsync(pnpDeviceId);
                var record = new DeviceRecord
                {
                    StableId = stableId,
                    HardwareId = pnpDeviceId,
                    ProductName = name,
                    Manufacturer = manufacturer,
                    DeviceType = pnpClass
                };

                await _persistence.SaveDeviceAsync(record);
                devices.Add(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating general (non-HID) devices");
        }

        return devices;
    }
}
