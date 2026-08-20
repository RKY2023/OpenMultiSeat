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

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains(StartSeatsArg))
        {
            App.RunHeadlessStartupAsync().GetAwaiter().GetResult();
            return 0;
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
