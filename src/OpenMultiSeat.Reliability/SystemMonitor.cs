using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Reliability;

public interface ISystemMonitor
{
    Task<SystemHealthReport> GetHealthStatusAsync();
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task<IReadOnlyList<HealthAlert>> GetActiveAlertsAsync();
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync();
}

public class SystemHealthReport
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public Dictionary<string, ComponentHealth> ComponentStatus { get; set; } = [];
    public List<string> Issues { get; set; } = [];
    public double OverallScore { get; set; }
}

public class ComponentHealth
{
    public string Name { get; set; } = "";
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public DateTime LastChecked { get; set; }
}

public class PerformanceMetrics
{
    public float CpuUsage { get; set; }
    public float MemoryUsage { get; set; }
    public long DiskSpaceAvailable { get; set; }
    public int ActiveProcesses { get; set; }
    public int ActiveSessions { get; set; }
    public DateTime MeasuredAt { get; set; }
}

public class HealthAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AlertSeverity Severity { get; set; }
    public string Component { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
}

public enum AlertSeverity { Info, Warning, Error, Critical }

public class SystemMonitor : ISystemMonitor
{
    private readonly ILogger<SystemMonitor> _logger;
    private readonly List<HealthAlert> _activeAlerts = [];
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    private static readonly PerformanceCounter CpuCounter = new("Processor", "% Processor Time", "_Total");
    private static readonly PerformanceCounter MemCounter = new("Memory", "% Committed Bytes In Use");

    public SystemMonitor(ILogger<SystemMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<SystemHealthReport> GetHealthStatusAsync()
    {
        var report = new SystemHealthReport { CheckedAt = DateTime.UtcNow };

        try
        {
            var cpuOk = CheckCpuHealth();
            var memOk = CheckMemoryHealth();
            var diskOk = CheckDiskHealth();
            var processesOk = CheckProcessHealth();

            report.ComponentStatus["CPU"] = new ComponentHealth
            {
                Name = "CPU",
                IsHealthy = cpuOk,
                Status = cpuOk ? "Normal" : "High Usage"
            };

            report.ComponentStatus["Memory"] = new ComponentHealth
            {
                Name = "Memory",
                IsHealthy = memOk,
                Status = memOk ? "Normal" : "High Usage"
            };

            report.ComponentStatus["Disk"] = new ComponentHealth
            {
                Name = "Disk",
                IsHealthy = diskOk,
                Status = diskOk ? "Normal" : "Low Space"
            };

            report.ComponentStatus["Processes"] = new ComponentHealth
            {
                Name = "Processes",
                IsHealthy = processesOk,
                Status = processesOk ? "Normal" : "Too Many"
            };

            var healthyCount = report.ComponentStatus.Values.Count(c => c.IsHealthy);
            report.OverallScore = (healthyCount / (double)report.ComponentStatus.Count) * 100;
            report.IsHealthy = report.OverallScore >= 75;

            if (!report.IsHealthy)
            {
                report.Issues.AddRange(report.ComponentStatus.Values
                    .Where(c => !c.IsHealthy)
                    .Select(c => $"{c.Name}: {c.Status}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system health");
            report.Issues.Add($"Health check error: {ex.Message}");
            report.IsHealthy = false;
        }

        return report;
    }

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
    {
        try
        {
            var metrics = new PerformanceMetrics
            {
                CpuUsage = (float)CpuCounter.NextValue(),
                MemoryUsage = (float)MemCounter.NextValue(),
                ActiveProcesses = Process.GetProcesses().Length,
                MeasuredAt = DateTime.UtcNow
            };

            var driveInfo = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");
            if (driveInfo != null)
            {
                metrics.DiskSpaceAvailable = driveInfo.AvailableFreeSpace;
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
            return new PerformanceMetrics { MeasuredAt = DateTime.UtcNow };
        }
    }

    public async Task<IReadOnlyList<HealthAlert>> GetActiveAlertsAsync()
    {
        return _activeAlerts.Where(a => !a.IsResolved).ToList();
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_monitoringTask != null && !_monitoringTask.IsCompleted)
        {
            _logger.LogWarning("Monitoring already running");
            return;
        }

        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitoringTask = MonitoringLoopAsync(_monitoringCts.Token);
        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        if (_monitoringCts != null)
        {
            _monitoringCts.Cancel();
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
        }
    }

    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("System monitoring started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var health = await GetHealthStatusAsync();
                var metrics = await GetPerformanceMetricsAsync();

                if (!health.IsHealthy)
                {
                    AddAlert(new HealthAlert
                    {
                        Severity = AlertSeverity.Warning,
                        Component = "System",
                        Message = $"System health degraded: {string.Join(", ", health.Issues)}"
                    });
                }

                if (metrics.CpuUsage > 80)
                {
                    AddAlert(new HealthAlert
                    {
                        Severity = AlertSeverity.Warning,
                        Component = "CPU",
                        Message = $"High CPU usage: {metrics.CpuUsage:F1}%"
                    });
                }

                if (metrics.MemoryUsage > 80)
                {
                    AddAlert(new HealthAlert
                    {
                        Severity = AlertSeverity.Warning,
                        Component = "Memory",
                        Message = $"High memory usage: {metrics.MemoryUsage:F1}%"
                    });
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring loop");
            }
        }

        _logger.LogInformation("System monitoring stopped");
    }

    private bool CheckCpuHealth()
    {
        try
        {
            return CpuCounter.NextValue() < 80;
        }
        catch { return true; }
    }

    private bool CheckMemoryHealth()
    {
        try
        {
            return MemCounter.NextValue() < 80;
        }
        catch { return true; }
    }

    private bool CheckDiskHealth()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");
            return drive?.AvailableFreeSpace > 1_000_000_000; // 1GB minimum
        }
        catch { return true; }
    }

    private bool CheckProcessHealth()
    {
        try
        {
            return Process.GetProcesses().Length < 500;
        }
        catch { return true; }
    }

    private void AddAlert(HealthAlert alert)
    {
        lock (_activeAlerts)
        {
            _activeAlerts.Add(alert);
            _logger.LogWarning($"Alert: [{alert.Severity}] {alert.Component}: {alert.Message}");
        }
    }
}
