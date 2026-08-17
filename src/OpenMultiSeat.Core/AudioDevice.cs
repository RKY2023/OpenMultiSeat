namespace OpenMultiSeat.Core;

public enum AudioDeviceType
{
    Playback,
    Recording
}

public enum AudioDeviceRole
{
    Playback,
    Recording,
    BiDirectional
}

public sealed class AudioDevice
{
    public required string DeviceId { get; set; }
    public required string EndpointId { get; set; }
    
    public required string FriendlyName { get; set; }
    public AudioDeviceType Type { get; set; }
    
    public bool IsDefault { get; set; }
    public bool IsConnected { get; set; }
    
    public string? AssignedSeatId { get; set; }
}
