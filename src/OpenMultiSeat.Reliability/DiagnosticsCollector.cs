using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Reliability;

public interface IDiagnosticsCollector
{
    Task<DiagnosticsReport> CollectDiagnosticsAsync();
    Task<string> ExportDiagnosticsAsync(string? outputPath = null);
    Task<SystemInfo> GetSystemInfoAsync();
}

public class DiagnosticsReport
{
    public SystemInfo SystemInfo { get; set; } = new();
    public PerformanceMetrics Performance { get; set; } = new();
    public ProcessInfo[] Processes { get; set; } = [];
    public string[] LogFiles { get; set; } = [];
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class SystemInfo
{
    public string OsVersion { get; set; } = "";
    public string NetVersion { get; set; } = "";
    public int ProcessorCount { get; set; }
    public long TotalMemory { get; set; }
    public string ComputerName { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime InstallDate { get; set; }
}

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string Name { get; set; } = "";
    public long MemoryUsage { get; set; }
    public TimeSpan CpuTime { get; set; }
    public int ThreadCount { get; set; }
}

public class DiagnosticsCollector : IDiagnosticsCollector
{
    private readonly ILogger<DiagnosticsCollector> _logger;
    private readonly ISystemMonitor _monitor;

    public DiagnosticsCollector(ILogger<DiagnosticsCollector> logger, ISystemMonitor monitor)
    {
        _logger = logger;
        _monitor = monitor;
    }

    public async Task<DiagnosticsReport> CollectDiagnosticsAsync()
    {
        _logger.LogInformation("Starting diagnostics collection...");

        var report = new DiagnosticsReport
        {
            SystemInfo = await GetSystemInfoAsync(),
            Performance = await _monitor.GetPerformanceMetricsAsync(),
            Processes = GetProcessInfo(),
            LogFiles = GetLogFiles(),
            CollectedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Diagnostics collection completed");
        return report;
    }

    public async Task<string> ExportDiagnosticsAsync(string? outputPath = null)
    {
        try
        {
            var report = await CollectDiagnosticsAsync();
            outputPath ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMultiSeat", "diagnostics", $"diag-{DateTime.Now:yyyyMMdd-HHmmss}.json");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);

            _logger.LogInformation($"Diagnostics exported to: {outputPath}");
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting diagnostics");
            throw;
        }
    }

    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        try
        {
            return new SystemInfo
            {
                OsVersion = Environment.OSVersion.ToString(),
                NetVersion = GetNetVersion(),
                ProcessorCount = Environment.ProcessorCount,
                TotalMemory = GC.GetTotalMemory(false),
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                InstallDate = GetInstallDate()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system info");
            return new SystemInfo();
        }
    }

    private ProcessInfo[] GetProcessInfo()
    {
        try
        {
            return Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(20)
                .Select(p => new ProcessInfo
                {
                    ProcessId = p.Id,
                    Name = p.ProcessName,
                    MemoryUsage = p.WorkingSet64,
                    CpuTime = p.TotalProcessorTime,
                    ThreadCount = p.Threads.Count
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting process info");
            return [];
        }
    }

    private string[] GetLogFiles()
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMultiSeat", "logs");

            if (!Directory.Exists(logDir))
                return [];

            return Directory.GetFiles(logDir, "*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Take(10)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting log files");
            return [];
        }
    }

    private string GetNetVersion()
    {
        try
        {
            return $".NET {RuntimeInformation.FrameworkDescription}";
        }
        catch
        {
            return ".NET Unknown";
        }
    }

    private DateTime GetInstallDate()
    {
        try
        {
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OpenMultiSeat");
            if (Directory.Exists(installPath))
            {
                return new DirectoryInfo(installPath).CreationTime;
            }
        }
        catch { }

        return DateTime.MinValue;
    }
}
