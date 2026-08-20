using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Sessions;

public sealed class StartupTriggerResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static StartupTriggerResult Ok() => new() { Success = true };
    public static StartupTriggerResult Failed(string error) => new() { Success = false, Error = error };
}

public interface IStartupTriggerManager
{
    /// <summary>
    /// Registers whichever Windows Scheduled Task <paramref name="mode"/> needs and removes the
    /// other — so switching modes never leaves a stale trigger behind. <paramref name="triggerSeat"/>
    /// (the seat treated as "Workplace 1") is required only for <see cref="SeatStartMode.AtFirstLogin"/>.
    /// </summary>
    Task<StartupTriggerResult> ApplyAsync(SeatStartMode mode, Seat? triggerSeat);
}

/// <summary>
/// Registers/removes the Windows Scheduled Tasks behind the two automatic "Workplace Start Mode"
/// options, via schtasks.exe (no extra package — it's a stock Windows tool, and text-based
/// arguments are easy to verify/log, unlike the Task Scheduler COM API). Both tasks invoke this
/// same executable with "--start-seats" (see App.xaml.cs), which runs headless and calls
/// <see cref="SeatStartupOrchestrator"/>.
///
/// Registering an ONSTART/SYSTEM task requires the caller to be running elevated; a non-elevated
/// caller gets a failed <see cref="StartupTriggerResult"/> back with schtasks' own error text,
/// not a silent no-op.
/// </summary>
public class WindowsStartupTriggerManager : IStartupTriggerManager
{
    public const string AtSystemStartupTaskName = "OpenMultiSeat_AutoStartAtBoot";
    public const string AtFirstLoginTaskName = "OpenMultiSeat_AutoStartAtFirstLogin";

    private readonly ILogger<WindowsStartupTriggerManager> _logger;

    public WindowsStartupTriggerManager(ILogger<WindowsStartupTriggerManager> logger)
    {
        _logger = logger;
    }

    public async Task<StartupTriggerResult> ApplyAsync(SeatStartMode mode, Seat? triggerSeat)
    {
        // Clear both first, unconditionally, so switching to any mode (including Manual) always
        // starts from a clean slate rather than layering a new task on top of a stale one.
        await DeleteTaskAsync(AtSystemStartupTaskName);
        await DeleteTaskAsync(AtFirstLoginTaskName);

        switch (mode)
        {
            case SeatStartMode.Manual:
                return StartupTriggerResult.Ok();

            case SeatStartMode.AtSystemStartup:
                return await CreateTaskAsync(AtSystemStartupTaskName, "/SC ONSTART /RU SYSTEM /RL HIGHEST");

            case SeatStartMode.AtFirstLogin:
                if (triggerSeat == null || string.IsNullOrWhiteSpace(triggerSeat.WindowsUser))
                {
                    return StartupTriggerResult.Failed(
                        "\"Via Workplace 1\" needs Workplace 1 (the first seat in your list) to have a Windows account configured first — see the User Account dialog.");
                }

                return await CreateTaskAsync(AtFirstLoginTaskName, $"/SC ONLOGON /RU \"{ResolveAccountName(triggerSeat)}\"");

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    /// <summary>Formats a seat's Windows account for schtasks' /RU — DOMAIN\user for a domain
    /// account, bare username for a local one. Pure/static so it's testable without invoking
    /// schtasks.exe.</summary>
    public static string ResolveAccountName(Seat seat) =>
        seat.WindowsDomain != null ? $"{seat.WindowsDomain}\\{seat.WindowsUser}" : seat.WindowsUser!;

    private async Task<StartupTriggerResult> CreateTaskAsync(string taskName, string triggerArgs)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
            return StartupTriggerResult.Failed("Couldn't resolve the current executable's path.");

        var args = $"/Create /TN \"{taskName}\" {triggerArgs} /TR \"\\\"{exePath}\\\" --start-seats\" /F";
        var (exitCode, output) = await RunSchtasksAsync(args);

        if (exitCode != 0)
        {
            _logger.LogError($"schtasks /Create failed for '{taskName}' (exit {exitCode}): {output}");
            return StartupTriggerResult.Failed($"schtasks failed (exit {exitCode}): {output.Trim()}");
        }

        _logger.LogInformation($"Registered scheduled task '{taskName}': {triggerArgs}");
        return StartupTriggerResult.Ok();
    }

    private async Task DeleteTaskAsync(string taskName)
    {
        // A missing task makes schtasks exit non-zero — that's fine, there's nothing to remove;
        // the result is deliberately not surfaced as an error.
        await RunSchtasksAsync($"/Delete /TN \"{taskName}\" /F");
    }

    private async Task<(int ExitCode, string Output)> RunSchtasksAsync(string arguments)
    {
        var psi = new ProcessStartInfo("schtasks.exe", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Couldn't start schtasks.exe.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, string.IsNullOrWhiteSpace(stdout) ? stderr : stdout);
    }
}
