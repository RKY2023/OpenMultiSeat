namespace OpenMultiSeat.Service;

public sealed class MultiSeatWorker
{
    private readonly ILogger<MultiSeatWorker> _logger;
    
    public MultiSeatWorker(ILogger<MultiSeatWorker> logger)
    {
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OpenMultiSeat service starting");
        await Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OpenMultiSeat service stopping");
        await Task.CompletedTask;
    }
}

public sealed class WindowsServiceWorker : BackgroundService
{
    private readonly ILogger<WindowsServiceWorker> _logger;
    private readonly MultiSeatWorker _worker;
    
    public WindowsServiceWorker(ILogger<WindowsServiceWorker> logger, MultiSeatWorker worker)
    {
        _logger = logger;
        _worker = worker;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _worker.StartAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancelled");
        }
        finally
        {
            await _worker.StopAsync(stoppingToken);
        }
    }
}
