namespace OpenMultiSeat.Core;

public enum InputDeviceType
{
    Keyboard,
    Mouse,
    Gamepad,
    Other
}

public sealed class InputDevice
{
    public required string DeviceId { get; set; }
    public required string HardwareId { get; set; }
    
    public string? Manufacturer { get; set; }
    public string? ProductName { get; set; }
    
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? SerialNumber { get; set; }
    
    public InputDeviceType Type { get; set; }
    
    public string? AssignedSeatId { get; set; }
    
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
