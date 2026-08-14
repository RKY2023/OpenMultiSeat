namespace OpenMultiSeat.IPC;

public interface IMultiSeatServiceClient
{
    Task<T> CallAsync<T>(string methodName, object? parameters = null);
}

public class PipeServer
{
    private const string PipeName = "OpenMultiSeat.Service";
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
    }
}
