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

    /// <summary>Last-resort handler for exceptions that reach the UI thread unhandled -- including
    /// those thrown from async void event handlers, which WPF re-raises on the dispatcher. Without
    /// it, any one of them silently terminates the whole process: found by live testing when a
    /// Tile Layout audio-meter race killed the app with nothing shown to the user. Logs the full
    /// exception to %AppData%\OpenMultiSeat\crash-log.txt, tells the user, and keeps running.</summary>
    internal static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMultiSeat", "crash-log.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging is best-effort; never let it turn a handled error back into a crash.
        }

        MessageBox.Show(
            $"Something went wrong, but OpenMultiSeat is still running:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\n" +
            "Details were saved to %AppData%\\OpenMultiSeat\\crash-log.txt.",
            "OpenMultiSeat", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
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
