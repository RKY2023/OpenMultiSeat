namespace OpenMultiSeat.Core;

public sealed class Seat
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? WindowsUser { get; set; }
    
    public List<string> DisplayIds { get; set; } = [];
    public List<string> KeyboardIds { get; set; } = [];
    public List<string> MouseIds { get; set; } = [];
    
    public string? AudioPlaybackId { get; set; }
    public string? AudioCaptureId { get; set; }
    
    public bool Enabled { get; set; } = true;
    
    public SeatStatus Status { get; set; } = SeatStatus.Disabled;
}

public enum SeatStatus
{
    Disabled,
    Configured,
    Starting,
    Running,
    LoggedOff,
    Locked,
    Error,
    Recovering
}
