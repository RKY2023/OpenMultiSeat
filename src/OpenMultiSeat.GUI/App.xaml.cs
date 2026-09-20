using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMultiSeat.Core;
using OpenMultiSeat.Sessions;

namespace OpenMultiSeat.GUI;

public partial class App : Application
{
    /// <summary>Set by Program.Main from "--apply-start-mode=&lt;mode&gt;" when this process was
    /// launched by SettingsPage.TryRelaunchElevated to carry a just-chosen Workplace Start Mode
    /// across a UAC relaunch. MainWindow's constructor reads and clears this once, right after
    /// construction — never read again afterward, so a later normal launch never picks up a stale
    /// value some other way.</summary>
    public static SeatStartMode? PendingApplyStartMode { get; set; }

    /// <summary>
    /// Body of the "--start-seats" headless entry point (see <see cref="Program"/> for why this
    /// runs from a hand-written Main rather than an OnStartup override).
    ///
    /// Uses <see cref="NullLogger{T}"/>, not <see cref="GuiLoggerFactory"/>: this process is
    /// launched headless by a Scheduled Task with no attached console, so a console logger has
    /// nothing useful to write to. AppendStartupLog below (a plain file) is the real log for this
    /// path — check %AppData%\OpenMultiSeat\startup-log.txt after a scheduled run.
    /// </summary>
    internal static async Task RunHeadlessStartupAsync()
    {
        AppendStartupLog("--start-seats invoked");

        try
        {
            var seatPersistence = new SeatPersistence(NullLogger<SeatPersistence>.Instance);
            var seats = await seatPersistence.GetAllSeatsAsync();

            var sessionEnumerator = new SessionEnumerator(NullLogger<SessionEnumerator>.Instance);
            var sessionManager = new SessionManager(
                NullLogger<SessionManager>.Instance, sessionEnumerator, new CpuAffinityProvider());
            var orchestrator = new SeatStartupOrchestrator(
                NullLogger<SeatStartupOrchestrator>.Instance, sessionManager);

            var summary = await orchestrator.StartAllSeatsAsync(seats);

            AppendStartupLog(
                $"{summary.SucceededCount} started, {summary.SkippedCount} skipped, {summary.FailedCount} failed (of {seats.Count} seat(s)).");

            foreach (var result in summary.Results.Where(r => !r.Success))
                AppendStartupLog($"  FAILED: {result.SeatName} ({result.SeatId}): {result.Error}");

            foreach (var result in summary.Results.Where(r => r.Success))
                AppendStartupLog($"  started: {result.SeatName} ({result.SeatId}) — PID {result.ProcessId}");
        }
        catch (Exception ex)
        {
            AppendStartupLog($"UNHANDLED FAILURE: {ex}");
        }
    }

    private static void AppendStartupLog(string line)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMultiSeat", "startup-log.txt");
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort: a logging failure shouldn't take down the startup attempt itself.
        }
    }
}
