using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Reads and writes the real device registry directly through IDevicePersistence /
/// IHidDeviceEnumerator, and now also seat assignment via ISeatManager — the same direct-
/// persistence pattern used by SeatsPage/AssignCpuCoresWindow, since no GUI page has IPC wiring
/// to the Service today. Previously this page's buttons only popped a MessageBox and
/// DevicesGrid.ItemsSource was never set, despite the XAML looking fully built out.
/// </summary>
public partial class DevicesPage : Page
{
    private readonly IDevicePersistence _persistence;
    private readonly IHidDeviceEnumerator _enumerator;
    private readonly ISeatManager _seatManager;
    private IReadOnlyList<Seat> _seats = [];

    public DevicesPage()
    {
        InitializeComponent();

        _persistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        _enumerator = new HidDeviceEnumerator(GuiLoggerFactory.Instance.CreateLogger<HidDeviceEnumerator>(), _persistence);
        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        _seatManager = new SeatManager(GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, _persistence);

        Loaded += async (_, _) => await LoadFromRegistryAsync();
    }

    private async Task LoadFromRegistryAsync()
    {
        var devices = await _persistence.GetAllDevicesAsync();
        _seats = await _seatManager.GetAllSeatsAsync();

        // ISeatManager has no "which seat owns this device" query — assignment truth lives on
        // each Seat's KeyboardIds/MouseIds lists, so build the reverse lookup here.
        var assignedTo = new Dictionary<string, string>();
        foreach (var seat in _seats)
        {
            foreach (var deviceId in seat.KeyboardIds.Concat(seat.MouseIds))
                assignedTo[deviceId] = seat.Name;
        }

        DevicesGrid.ItemsSource = devices
            .Select(d => new DeviceRow(d, assignedTo.GetValueOrDefault(d.StableId)))
            .ToList();

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

    private async void OnAssignToSeat(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRow row)
        {
            MessageBox.Show("Select a device to assign first.", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seats.Count == 0)
        {
            MessageBox.Show("No seats exist yet. Create one on the Seats page first.", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new AssignDeviceToSeatWindow(_seatManager, row.Device, _seats)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await LoadFromRegistryAsync();
        }
    }

    private async void OnUnassign(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRow row)
        {
            MessageBox.Show("Select a device to unassign first.", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.AssignedSeatName == null)
        {
            MessageBox.Show("This device isn't assigned to a seat.", "Devices", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _seatManager.UnassignDeviceFromSeatAsync(row.Device.StableId);
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

    /// <summary>Wraps a DeviceRecord with its resolved seat assignment for grid binding.</summary>
    private sealed class DeviceRow(DeviceRecord device, string? assignedSeatName)
    {
        public DeviceRecord Device { get; } = device;
        public string? ProductName => Device.ProductName;
        public string? HardwareId => Device.HardwareId;
        public string? VendorId => Device.VendorId;
        public string? ProductId => Device.ProductId;
        public DateTime FirstSeen => Device.FirstSeen;
        public string? AssignedSeatName { get; } = assignedSeatName;
        public string AssignedSeatDisplay => AssignedSeatName ?? "(unassigned)";
    }
}
