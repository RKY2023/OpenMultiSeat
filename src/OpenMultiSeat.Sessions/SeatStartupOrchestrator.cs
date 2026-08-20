using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Sessions;

public sealed class SeatStartupResult
{
    public required string SeatId { get; set; }
    public required string SeatName { get; set; }
    public bool Success { get; set; }
    public uint ProcessId { get; set; }
    public string? Error { get; set; }
}

public sealed class SeatStartupSummary
{
    public List<SeatStartupResult> Results { get; } = [];

    /// <summary>Seats intentionally not attempted — disabled, or no saved auto-login credentials
    /// (either "Display login dialog" is on, or no password was ever saved). Not failures.</summary>
    public int SkippedCount { get; set; }

    public int SucceededCount => Results.Count(r => r.Success);
    public int FailedCount => Results.Count(r => !r.Success);
}

public interface ISeatStartupOrchestrator
{
    Task<SeatStartupSummary> StartAllSeatsAsync(IReadOnlyList<Seat> seats);
}

/// <summary>
/// Starts every eligible seat's session by launching Explorer as that seat's saved Windows
/// account via <see cref="ISessionManager.LaunchProcessWithCredentialsAsync"/> — the same
/// verified CreateProcessWithLogonW path already exercised by the "Test Launch" button on the
/// User Account dialog. Backs both the manual "Start Workplaces Now" button and the headless
/// "--start-seats" entry point the At-System-Startup / Via-Workplace-1 scheduled tasks invoke.
///
/// A seat is skipped (not a failure) when it's disabled, set to "Display login dialog" (no
/// stored password to auto-login with), or has never had a password saved. See
/// docs/control-panel/general-settings-tab.md for the DPAPI/SYSTEM caveat this hits under the
/// At System Startup trigger specifically.
/// </summary>
public class SeatStartupOrchestrator : ISeatStartupOrchestrator
{
    /// <summary>Explorer, not an arbitrary "seat app" — there's no per-seat launch-target concept
    /// in OpenMultiSeat today. This starts a normal desktop session for the seat's account, same
    /// caveat as CreateProcessWithLogonW itself: it doesn't bind that session to the seat's own
    /// hardware (see SessionManager.CreateProcessWithCredentials's doc comment).</summary>
    private const string LaunchTarget = @"C:\Windows\explorer.exe";

    private readonly ILogger<SeatStartupOrchestrator> _logger;
    private readonly ISessionManager _sessionManager;

    public SeatStartupOrchestrator(ILogger<SeatStartupOrchestrator> logger, ISessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
    }

    public async Task<SeatStartupSummary> StartAllSeatsAsync(IReadOnlyList<Seat> seats)
    {
        var summary = new SeatStartupSummary();

        foreach (var seat in seats)
        {
            if (!seat.Enabled)
            {
                summary.SkippedCount++;
                continue;
            }

            if (seat.DisplayLoginDialog || string.IsNullOrEmpty(seat.WindowsUser) || string.IsNullOrEmpty(seat.EncryptedPassword))
            {
                _logger.LogInformation(
                    $"Skipping seat {seat.Id} ({seat.Name}): no saved auto-login credentials (\"Display login dialog\" is on, or no password was saved).");
                summary.SkippedCount++;
                continue;
            }

            string? password;
            try
            {
                password = SeatCredentialProtector.Unprotect(seat.EncryptedPassword);
            }
            catch (Exception ex)
            {
                // The realistic cause: this process isn't running as the same Windows account
                // that originally saved the password — DPAPI CurrentUser scope refuses to
                // decrypt for anyone else. Hit by design under At System Startup (runs as
                // SYSTEM); see the class doc comment.
                _logger.LogError(ex, $"Couldn't decrypt the saved password for seat {seat.Id} ({seat.Name})");
                summary.Results.Add(new SeatStartupResult
                {
                    SeatId = seat.Id,
                    SeatName = seat.Name,
                    Success = false,
                    Error = $"Couldn't decrypt the saved password ({ex.GetType().Name}): {ex.Message}"
                });
                continue;
            }

            if (string.IsNullOrEmpty(password))
            {
                summary.SkippedCount++;
                continue;
            }

            try
            {
                var pid = await _sessionManager.LaunchProcessWithCredentialsAsync(
                    seat.WindowsUser!, seat.WindowsDomain, password, LaunchTarget,
                    cpuCoreAffinity: seat.CpuCoreAffinity);

                summary.Results.Add(new SeatStartupResult
                {
                    SeatId = seat.Id,
                    SeatName = seat.Name,
                    Success = true,
                    ProcessId = pid
                });
                _logger.LogInformation($"Started seat {seat.Id} ({seat.Name}) as {seat.WindowsUser} — PID {pid}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start seat {seat.Id} ({seat.Name})");
                summary.Results.Add(new SeatStartupResult
                {
                    SeatId = seat.Id,
                    SeatName = seat.Name,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return summary;
    }
}
