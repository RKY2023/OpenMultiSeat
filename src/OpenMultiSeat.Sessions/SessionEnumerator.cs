namespace OpenMultiSeat.Sessions;

public interface ISessionEnumerator
{
    Task<IReadOnlyList<WindowsSession>> EnumerateSessionsAsync();
}

public sealed class WindowsSession
{
    public required uint SessionId { get; set; }
    public required string UserName { get; set; }
    public SessionState State { get; set; }
}

public enum SessionState
{
    Active,
    Connected,
    ConnectQuery,
    Shadow,
    Disconnected,
    Idle,
    Listen,
    Reset,
    Down,
    Init
}

public class SessionEnumerator : ISessionEnumerator
{
    public async Task<IReadOnlyList<WindowsSession>> EnumerateSessionsAsync()
    {
        return await Task.FromResult<IReadOnlyList<WindowsSession>>([]);
    }
}
