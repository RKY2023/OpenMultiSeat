namespace OpenMultiSeat.Core;

public sealed class Display
{
    public required string DisplayId { get; set; }
    public required string DeviceName { get; set; }
    
    public string? FriendlyName { get; set; }
    public string? Manufacturer { get; set; }
    
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint RefreshRate { get; set; }
    
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    
    public bool IsPrimary { get; set; }
    public bool IsConnected { get; set; }
    
    public string? ConnectionType { get; set; }
    
    public byte[]? EdidData { get; set; }
    
    public string? AssignedSeatId { get; set; }
}
