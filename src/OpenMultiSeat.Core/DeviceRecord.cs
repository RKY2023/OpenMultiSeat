namespace OpenMultiSeat.Core;

public interface IDevicePersistence
{
    Task<DeviceRecord?> GetDeviceByHardwareIdAsync(string hardwareId);
    Task<DeviceRecord?> GetDeviceByStableIdAsync(string stableId);
    Task SaveDeviceAsync(DeviceRecord device);
    Task<IReadOnlyList<DeviceRecord>> GetAllDevicesAsync();
    Task DeleteDeviceAsync(string stableId);
    Task<string> GenerateStableIdAsync(string hardwareId, string? serialNumber = null);
}

public class DeviceRecord
{
    public required string StableId { get; set; }
    public required string HardwareId { get; set; }
    public string? SerialNumber { get; set; }
    public string? ContainerId { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Manufacturer { get; set; }
    public string? DeviceType { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int ConnectionCount { get; set; }
}
