using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;
using OpenMultiSeat.InputIsolation;
using OpenMultiSeat.Sessions;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// System overview. Every number and action here reads or acts on the same real backends the
/// other pages use. Found by live testing: this page used to be a leftover stub. All four stat
/// tiles were hard-coded to "0" (on a machine with 4 seats and 55 registered devices),
/// "Validate Configuration" always reported "All configurations are valid" without checking
/// anything, and "Export Configuration" only showed a message box repeating its own label.
/// </summary>
public partial class DashboardPage : Page
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenMultiSeat");

    public DashboardPage()
    {
        InitializeComponent();
        LoadSystemInfo();
        Loaded += async (_, _) => await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        var factory = GuiLoggerFactory.Instance;

        // Each tile is loaded independently: one failing source (e.g. WTS session enumeration)
        // shouldn't blank out the others, and "?" is more honest than a made-up 0.
        DeviceCountText.Text = await CountOrUnknownAsync(async () =>
            (await new DevicePersistence(factory.CreateLogger<DevicePersistence>()).GetAllDevicesAsync()).Count);

        SeatCountText.Text = await CountOrUnknownAsync(async () =>
            (await new SeatPersistence(factory.CreateLogger<SeatPersistence>()).GetAllSeatsAsync()).Count);

        DisplayCountText.Text = await CountOrUnknownAsync(async () =>
            (await new DisplayEnumerator(factory.CreateLogger<DisplayEnumerator>()).EnumerateDisplaysAsync()).Count);

        SessionCountText.Text = await CountOrUnknownAsync(async () =>
            (await new SessionEnumerator(factory.CreateLogger<SessionEnumerator>()).EnumerateSessionsAsync())
                .Count(s => s.State == SessionState.Active));
    }

    private static async Task<string> CountOrUnknownAsync(Func<Task<int>> count)
    {
        try
        {
            return (await count()).ToString();
        }
        catch
        {
            return "?";
        }
    }

    private void LoadSystemInfo()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        SystemInfoList.Items.Clear();
        foreach (var line in new[]
                 {
                     $"OpenMultiSeat {version.Split('+')[0]}",
                     RuntimeInformation.FrameworkDescription,
                     RuntimeInformation.OSDescription,
                     $"Configuration folder: {ConfigDirectory}"
                 })
        {
            SystemInfoList.Items.Add(new ListBoxItem { Content = line, Padding = new Thickness(10) });
        }
    }

    private async void OnRefreshStatus(object sender, RoutedEventArgs e)
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>Zips every configuration file (*.json) in %AppData%\OpenMultiSeat into one
    /// archive the user picks a location for. Seat passwords travel in their DPAPI-encrypted
    /// form, never plaintext, and only decrypt on this PC.</summary>
    private void OnExportConfig(object sender, RoutedEventArgs e)
    {
        var files = Directory.Exists(ConfigDirectory)
            ? Directory.GetFiles(ConfigDirectory, "*.json")
            : [];

        if (files.Length == 0)
        {
            MessageBox.Show($"There's no configuration to export yet ({ConfigDirectory} has no .json files).",
                "Export Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export OpenMultiSeat configuration",
            FileName = $"OpenMultiSeat-config-{DateTime.Now:yyyy-MM-dd}.zip",
            Filter = "Zip archive (*.zip)|*.zip",
            AddExtension = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        try
        {
            if (File.Exists(dialog.FileName))
                File.Delete(dialog.FileName);

            using (var archive = ZipFile.Open(dialog.FileName, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                    archive.CreateEntryFromFile(file, Path.GetFileName(file));
            }

            MessageBox.Show(
                $"Exported {files.Length} configuration file(s) to:\n{dialog.FileName}\n\n" +
                "Saved seat passwords are included only in their encrypted form, and only decrypt on this PC.",
                "Export Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't export the configuration: {ex.Message}",
                "Export Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Runs the real configuration validator (the same one the Input Isolation page's
    /// "Validate Configuration" uses). It checks keyboard/mouse bindings: bindings pointing at a
    /// deleted seat, a device bound twice, and seats left with no keyboard or mouse. That's the
    /// only configuration validator OpenMultiSeat has, so the message says what was checked
    /// rather than claiming everything is valid.</summary>
    private async void OnValidateConfig(object sender, RoutedEventArgs e)
    {
        try
        {
            var factory = GuiLoggerFactory.Instance;
            var devicePersistence = new DevicePersistence(factory.CreateLogger<DevicePersistence>());
            var seatManager = new SeatManager(factory.CreateLogger<SeatManager>(),
                new SeatPersistence(factory.CreateLogger<SeatPersistence>()), devicePersistence);
            var isolationService = new InputIsolationService(factory.CreateLogger<InputIsolationService>(),
                seatManager, devicePersistence,
                new InputIsolationPersistence(factory.CreateLogger<InputIsolationPersistence>()));

            var errors = await isolationService.ValidateIsolationConfigAsync();
            const string scope = "Checked: keyboard/mouse bindings (bindings to deleted seats, devices bound twice, seats with no keyboard or mouse).";

            if (errors.Count == 0)
            {
                MessageBox.Show($"No issues found.\n\n{scope}", "Validate Configuration",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                $"{errors.Count} issue(s) found:\n\n" +
                string.Join("\n", errors.Select(err => $"[{err.Severity}] {err.Message}")) +
                $"\n\n{scope}",
                "Validate Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't validate the configuration: {ex.Message}", "Validate Configuration",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
