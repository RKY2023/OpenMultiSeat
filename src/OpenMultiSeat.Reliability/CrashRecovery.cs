using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Reliability;

public interface ICrashRecovery
{
    Task RecordCrashAsync(string component, Exception exception);
    Task<CrashReport> GetLastCrashAsync();
    Task<IReadOnlyList<CrashReport>> GetCrashHistoryAsync(int limit = 10);
    Task<bool> RecoverFromCrashAsync(string component);
    Task ClearCrashHistoryAsync();
}

public class CrashReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Component { get; set; } = "";
    public string ExceptionMessage { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public RecoveryStatus Status { get; set; } = RecoveryStatus.Pending;
    public int RetryCount { get; set; }
    public string? RecoveryNotes { get; set; }
}

public enum RecoveryStatus { Pending, Recovering, Recovered, Failed }

public class CrashRecovery : ICrashRecovery
{
    private readonly ILogger<CrashRecovery> _logger;
    private readonly List<CrashReport> _crashHistory = [];
    private readonly string _crashLogPath;

    public CrashRecovery(ILogger<CrashRecovery> logger)
    {
        _logger = logger;
        _crashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat", "logs", "crashes.log");
        Directory.CreateDirectory(Path.GetDirectoryName(_crashLogPath)!);
    }

    public async Task RecordCrashAsync(string component, Exception exception)
    {
        var report = new CrashReport
        {
            Component = component,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace ?? "No stack trace available",
            OccurredAt = DateTime.UtcNow
        };

        lock (_crashHistory)
        {
            _crashHistory.Add(report);
        }

        await LogCrashAsync(report);
        _logger.LogError($"Crash recorded: {component} - {exception.Message}");
    }

    public async Task<CrashReport> GetLastCrashAsync()
    {
        lock (_crashHistory)
        {
            return _crashHistory.LastOrDefault() ?? new CrashReport();
        }
    }

    public async Task<IReadOnlyList<CrashReport>> GetCrashHistoryAsync(int limit = 10)
    {
        lock (_crashHistory)
        {
            return _crashHistory.TakeLast(limit).ToList();
        }
    }

    public async Task<bool> RecoverFromCrashAsync(string component)
    {
        try
        {
            var report = _crashHistory.FirstOrDefault(c => c.Component == component && !c.Status.Equals(RecoveryStatus.Recovered));
            if (report == null)
                return false;

            report.Status = RecoveryStatus.Recovering;
            report.RetryCount++;

            if (report.RetryCount > 3)
            {
                report.Status = RecoveryStatus.Failed;
                report.RecoveryNotes = "Max retry attempts exceeded";
                _logger.LogError($"Failed to recover {component} after {report.RetryCount} attempts");
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, report.RetryCount)));

            report.Status = RecoveryStatus.Recovered;
            report.RecoveryNotes = $"Recovered after {report.RetryCount} attempt(s)";

            _logger.LogInformation($"Successfully recovered {component}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recovering {component}");
            return false;
        }
    }

    public async Task ClearCrashHistoryAsync()
    {
        lock (_crashHistory)
        {
            _crashHistory.Clear();
        }

        try
        {
            if (File.Exists(_crashLogPath))
                File.Delete(_crashLogPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing crash history file");
        }
    }

    private async Task LogCrashAsync(CrashReport report)
    {
        try
        {
            var logEntry = $"""
                [{report.OccurredAt:yyyy-MM-dd HH:mm:ss.fff}] CRASH
                Component: {report.Component}
                Exception: {report.ExceptionMessage}
                StackTrace: {report.StackTrace}
                ---
                """;

            await File.AppendAllTextAsync(_crashLogPath, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error logging crash");
        }
    }
}
