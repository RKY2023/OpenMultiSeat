using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI;

/// <summary>
/// Hand-written entry point, replacing the Main WPF would otherwise auto-generate from
/// App.xaml's ApplicationDefinition build item (disabled for that reason in the .csproj — see the
/// comment there). Needed so "--start-seats" (the scheduled-task headless path — see
/// WindowsStartupTriggerManager and App.RunHeadlessStartupAsync) can run and exit before any WPF
/// Application/Dispatcher is created, avoiding the deadlock a blocking async call hits if it's
/// run from inside Application.OnStartup instead (see App.xaml.cs's doc comment for why).
/// </summary>
internal static class Program
{
    private const string StartSeatsArg = "--start-seats";

    /// <summary>Carries a Workplace Start Mode chosen just before an elevated UAC relaunch (see
    /// SettingsPage.TryRelaunchElevated) across to this new elevated process, so MainWindow can
    /// apply it automatically instead of asking the user to pick it again now that they're
    /// elevated. Not "--start-seats": this instance still shows the normal GUI.</summary>
    private const string ApplyStartModeArgPrefix = "--apply-start-mode=";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains(StartSeatsArg))
        {
            App.RunHeadlessStartupAsync().GetAwaiter().GetResult();
            return 0;
        }

        var applyModeArg = args.FirstOrDefault(a => a.StartsWith(ApplyStartModeArgPrefix, StringComparison.Ordinal));
        if (applyModeArg != null &&
            Enum.TryParse<SeatStartMode>(applyModeArg[ApplyStartModeArgPrefix.Length..], out var pendingMode))
        {
            App.PendingApplyStartMode = pendingMode;
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
