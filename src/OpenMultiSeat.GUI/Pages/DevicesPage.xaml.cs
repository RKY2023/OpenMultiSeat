using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Reads and writes the real device registry directly through IDevicePersistence /
/// IHidDeviceEnumerator — the same pattern used by AssignCpuCoresWindow, since no
/// GUI page has IPC wiring to the Service today. Previously this page's buttons only
/// popped a MessageBox and DevicesGrid.ItemsSource was never set, despite the XAML
/// looking fully built out.
/// </summary>
public partial class DevicesPage : Page
{
    private readonly IDevicePersistence _persistence;
    private readonly IHidDeviceEnumerator _enumerator;

    public DevicesPage()
    {
        InitializeComponent();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _persistence = new DevicePersistence(loggerFactory.CreateLogger<DevicePersistence>());
        _enumerator = new HidDeviceEnumerator(loggerFactory.CreateLogger<HidDeviceEnumerator>(), _persistence);

        Loaded += async (_, _) => await LoadFromRegistryAsync();
    }

    private async Task LoadFromRegistryAsync()
    {
        var devices = await _persistence.GetAllDevicesAsync();
        DevicesGrid.ItemsSource = devices;
        StatusText.Text = devices.Count == 0
            ? "No devices registered yet — click Scan Devices."
            : $"{devices.Count} device(s) in registry.";
    }

    private async void OnScanDevices(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning...";
        var found = await _enumerator.EnumerateAllDevicesAsync();
        await LoadFromRegistryAsync();
        MessageBox.Show(
            $"Scan complete: {found.Count} keyboard/mouse input device(s) detected via Windows Raw Input.",
            "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void OnRefresh(object sender, RoutedEventArgs e)
    {
        await LoadFromRegistryAsync();
    }

    private async void OnExportReport(object sender, RoutedEventArgs e)
    {
        var devices = await _persistence.GetAllDevicesAsync();
        if (devices.Count == 0)
        {
            MessageBox.Show("No devices to export yet. Scan for devices first.", "Devices", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"OpenMultiSeat-devices-{DateTime.Now:yyyy-MM-dd}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Device Name,Hardware ID,Vendor ID,Product ID,First Seen,Last Seen,Connection Count");
        foreach (var d in devices)
        {
            sb.AppendLine(string.Join(",",
                CsvField(d.ProductName),
                CsvField(d.HardwareId),
                CsvField(d.VendorId),
                CsvField(d.ProductId),
                CsvField(d.FirstSeen.ToString("u")),
                CsvField(d.LastSeen.ToString("u")),
                d.ConnectionCount));
        }

        await File.WriteAllTextAsync(dialog.FileName, sb.ToString());
        MessageBox.Show($"Exported {devices.Count} device(s) to:\n{dialog.FileName}", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string CsvField(string? value)
        => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
