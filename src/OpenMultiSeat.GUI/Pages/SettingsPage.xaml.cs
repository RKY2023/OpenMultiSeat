using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.GUI.Windows;
using OpenMultiSeat.Sessions;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// ASTER's General Settings tab equivalent: "Assign CPU Cores..." (unchanged, already real) plus
/// the real "Workplace Start Mode" setting — Manual / At System Startup / Via Workplace 1 — wired
/// to actual Windows Scheduled Tasks via WindowsStartupTriggerManager, and a real
/// "Start Workplaces Now" manual-trigger button (with a confirm prompt, standing in for ASTER's
/// "Confirming Starting of Workplaces" dialog) backed by SeatStartupOrchestrator. Replaces the
/// former no-op "Configure Settings" stub outright — see
/// docs/control-panel/general-settings-tab.md for what's real vs. still limited (notably the
/// DPAPI LocalMachine-scope trade-off At System Startup relies on).
///
/// Changing Workplace Start Mode always needs OpenMultiSeat running elevated (see
/// WindowsStartupTriggerManager). Rather than dead-ending on "run as Administrator yourself and
/// try again," picking a mode while not elevated offers to relaunch the whole app elevated via
/// UAC with "--apply-start-mode=&lt;mode&gt;" on the command line (see Program.Main / App.PendingApplyStartMode /
/// MainWindow's constructor) so the choice the user just made is carried across the relaunch and
/// applied automatically, instead of asking them to make the same selection twice.
/// </summary>
public partial class SettingsPage : Page
{
    private readonly IGeneralSettingsPersistence _settingsPersistence;
    private readonly ISeatPersistence _seatPersistence;
    private readonly IStartupTriggerManager _startupTriggerManager;
    private bool _suppressStartModeChanged;

    public SettingsPage()
    {
        InitializeComponent();
        _settingsPersistence = new GeneralSettingsPersistence(GuiLoggerFactory.Instance.CreateLogger<GeneralSettingsPersistence>());
        _seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        _startupTriggerManager = new WindowsStartupTriggerManager(GuiLoggerFactory.Instance.CreateLogger<WindowsStartupTriggerManager>());
        Loaded += async (_, _) => await LoadStartModeAsync();
    }

    /// <summary>Called by MainWindow right after navigating here, when this instance was launched
    /// specifically to apply a start mode chosen before an elevated relaunch (see the class doc
    /// comment). Runs after Loaded's own LoadStartModeAsync so the combo box reflects the
    /// (pre-relaunch) persisted mode first, then immediately applies the pending one — now while
    /// actually elevated, so it goes through instead of bouncing back to the same dead end.</summary>
    public void ApplyPendingStartModeOnLoad(SeatStartMode mode)
    {
        Loaded += async (_, _) => await ApplyStartModeAsync(mode, alreadyElevated: true);
    }

    private async Task LoadStartModeAsync()
    {
        var settings = await _settingsPersistence.LoadAsync();

        _suppressStartModeChanged = true;
        StartModeComboBox.SelectedIndex = settings.StartMode switch
        {
            SeatStartMode.Manual => 0,
            SeatStartMode.AtSystemStartup => 1,
            SeatStartMode.AtFirstLogin => 2,
            _ => 0
        };
        _suppressStartModeChanged = false;

        UpdateStartModeStatus(settings.StartMode);
    }

    private void UpdateStartModeStatus(SeatStartMode mode)
    {
        StartModeStatusText.Text = mode switch
        {
            SeatStartMode.Manual =>
                "No scheduled task is registered — nothing starts automatically. Use \"Start Workplaces Now\" below.",
            SeatStartMode.AtSystemStartup =>
                "A Windows Scheduled Task now runs at boot (as SYSTEM) and starts every seat with a saved login. " +
                "Check %AppData%\\OpenMultiSeat\\startup-log.txt after a reboot to confirm. Note: seat passwords " +
                "are protected with DPAPI LocalMachine scope so SYSTEM can decrypt them — meaning any local " +
                "account/process on this machine could, in principle, decrypt a saved seat password too, not just " +
                "the account that set it.",
            SeatStartMode.AtFirstLogin =>
                "A Windows Scheduled Task now fires when Workplace 1's (the first seat's) Windows account logs in " +
                "and starts every seat with a saved login. Check %AppData%\\OpenMultiSeat\\startup-log.txt after a " +
                "logon to confirm.",
            _ => string.Empty
        };
    }

    private async void OnStartModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressStartModeChanged || StartModeComboBox.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        var mode = Enum.Parse<SeatStartMode>(tag);
        await ApplyStartModeAsync(mode, alreadyElevated: false);
    }

    /// <summary>Shared by the combo box's own change handler and by the post-elevated-relaunch
    /// path (<see cref="ApplyPendingStartModeOnLoad"/>). <paramref name="alreadyElevated"/> skips
    /// the up-front elevation check/offer-to-relaunch entirely — set true only when this call is
    /// itself already running as the result of that relaunch, so a second (impossible: already
    /// elevated) relaunch prompt can never loop.</summary>
    private async Task ApplyStartModeAsync(SeatStartMode mode, bool alreadyElevated)
    {
        Seat? triggerSeat = null;
        if (mode == SeatStartMode.AtFirstLogin)
        {
            var seats = await _seatPersistence.GetAllSeatsAsync();
            triggerSeat = seats.FirstOrDefault();

            if (triggerSeat == null)
            {
                MessageBox.Show(
                    "Create at least one seat first — the first seat in your list is treated as \"Workplace 1\" for this trigger.",
                    "Workplace Start Mode", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadStartModeAsync();
                return;
            }
        }

        // Changing this setting always needs to be elevated — even switching back to Manual has
        // to delete whichever task is currently registered (see WindowsStartupTriggerManager).
        // Rather than let that call fail and dead-end on "now go run this as Administrator
        // yourself," offer to do it: relaunch the whole app elevated via UAC with the mode the
        // user just chose carried on the command line, so it's applied automatically once the
        // elevated instance comes up — no need to make the same selection twice.
        if (!alreadyElevated && !ElevationHelper.IsRunningElevated())
        {
            var relaunch = MessageBox.Show(
                "Changing the Workplace Start Mode needs OpenMultiSeat to be running as Administrator " +
                "(it registers/removes a Windows Scheduled Task, even when switching back to Manual).\n\n" +
                "Restart OpenMultiSeat as Administrator now and apply this choice automatically?",
                "Workplace Start Mode", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (relaunch != MessageBoxResult.Yes || !TryRelaunchElevated(mode))
                await LoadStartModeAsync();

            return;
        }

        StartModeComboBox.IsEnabled = false;
        try
        {
            var result = await _startupTriggerManager.ApplyAsync(mode, triggerSeat);
            if (!result.Success)
            {
                MessageBox.Show(
                    $"Couldn't apply this start mode: {result.Error}",
                    "Workplace Start Mode", MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadStartModeAsync();
                return;
            }

            await _settingsPersistence.SaveAsync(new GeneralSettings { StartMode = mode });
            UpdateStartModeStatus(mode);
        }
        finally
        {
            StartModeComboBox.IsEnabled = true;
        }
    }

    /// <summary>Relaunches this same executable elevated (UAC prompt) with
    /// "--apply-start-mode=&lt;mode&gt;" on the command line, then shuts down this (non-elevated)
    /// instance. Returns false — leaving this instance running, combo box reverted by the caller
    /// — if the user declines/cancels the UAC prompt (Win32Exception 1223, ERROR_CANCELLED) or the
    /// relaunch otherwise fails to start.</summary>
    private bool TryRelaunchElevated(SeatStartMode mode)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            MessageBox.Show("Couldn't resolve the current executable's path to relaunch it.",
                "Workplace Start Mode", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(exePath, $"--apply-start-mode={mode}")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED — user clicked "No" on the UAC prompt
        {
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't restart elevated: {ex.Message}",
                "Workplace Start Mode", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        Application.Current.Shutdown();
        return true;
    }

    /// <summary>Real manual-start action, with a confirm prompt standing in for ASTER's
    /// "Confirming Starting of Workplaces" dialog (see docs/control-panel/confirming-starting-of-workplaces.md
    /// for how this differs from ASTER's own boot-time version of that prompt).</summary>
    private async void OnStartWorkplaces(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Start all configured workplaces now?\n\n" +
            "This launches a session for every enabled seat that has a saved Windows login " +
            "(seats set to \"Display login dialog\" are skipped — there's no stored password to use).",
            "Confirm Starting of Workplaces", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        StartWorkplacesButton.IsEnabled = false;
        StartWorkplacesButton.Content = "Starting...";

        try
        {
            var seats = await _seatPersistence.GetAllSeatsAsync();

            var loggerFactory = GuiLoggerFactory.Instance;
            var sessionEnumerator = new SessionEnumerator(loggerFactory.CreateLogger<SessionEnumerator>());
            var sessionManager = new SessionManager(
                loggerFactory.CreateLogger<SessionManager>(), sessionEnumerator, new CpuAffinityProvider());
            var orchestrator = new SeatStartupOrchestrator(
                loggerFactory.CreateLogger<SeatStartupOrchestrator>(), sessionManager);

            var summary = await orchestrator.StartAllSeatsAsync(seats);

            var failed = summary.Results.Where(r => !r.Success).ToList();
            var message = $"{summary.SucceededCount} seat(s) started, {summary.SkippedCount} skipped (no saved login).";
            if (failed.Count > 0)
                message += "\n\nFailed:\n" + string.Join("\n", failed.Select(f => $"- {f.SeatName}: {f.Error}"));

            MessageBox.Show(message, "Start Workplaces", MessageBoxButton.OK,
                failed.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't start workplaces: {ex.Message}", "Start Workplaces", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StartWorkplacesButton.IsEnabled = true;
            StartWorkplacesButton.Content = "Start Workplaces Now";
        }
    }

    private void OnAssignCpuCores(object sender, RoutedEventArgs e)
    {
        var window = new AssignCpuCoresWindow
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }
}
