namespace OpenMultiSeat.Core;

public sealed class SeatConfiguration
{
    public int Version { get; set; } = 1;
    public List<Seat> Seats { get; set; } = [];
}

public sealed class SystemStatus
{
    public ServiceStatus ServiceStatus { get; set; } = ServiceStatus.Stopped;
    public uint ActiveSeatCount { get; set; }
    public uint TotalSeatCount { get; set; }
}

public enum ServiceStatus
{
    Stopped,
    Starting,
    Running,
    Degraded,
    Recovering,
    Stopping
}
