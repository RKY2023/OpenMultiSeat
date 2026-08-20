using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Real display list + seat assignment. Unlike Devices, there's no persistence layer for
/// displays — OpenMultiSeat.Displays.DisplayEnumerator has nothing analogous to
/// IDevicePersistence, so this page's grid reflects a live scan, not a saved registry, and
/// starts empty until "Scan Displays" is clicked. Display.DisplayId is derived from the current
/// Windows display topology (path.TargetInfo.Id) rather than a generated stable ID the way
/// devices get one — it can change if a monitor is unplugged/replugged or moved to a different
/// port, which could orphan an existing assignment. That's a real limitation, not fixed here.
/// </summary>
public partial class DisplaysPage : Page
{
    private readonly IDisplayEnumerator _enumerator;
    private readonly ISeatManager _seatManager;
    private IReadOnlyList<Display> _displays = [];
    private IReadOnlyList<Seat> _seats = [];

    public DisplaysPage()
    {
        InitializeComponent();

        _enumerator = new DisplayEnumerator(GuiLoggerFactory.Instance.CreateLogger<DisplayEnumerator>());
        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        var devicePersistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        _seatManager = new SeatManager(GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, devicePersistence);

        Loaded += async (_, _) => await ScanAsync();
    }

    private async Task ScanAsync()
    {
        _displays = await _enumerator.EnumerateDisplaysAsync();
        _seats = await _seatManager.GetAllSeatsAsync();

        var assignedTo = new Dictionary<string, string>();
        foreach (var seat in _seats)
        {
            foreach (var displayId in seat.DisplayIds)
                assignedTo[displayId] = seat.Name;
        }

        if (_displays.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            DisplaysGrid.Visibility = Visibility.Collapsed;
            StatusText.Text = string.Empty;
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;
        DisplaysGrid.Visibility = Visibility.Visible;
        DisplaysGrid.ItemsSource = _displays
            .Select(d => new DisplayRow(d, assignedTo.GetValueOrDefault(d.DisplayId)))
            .ToList();
        StatusText.Text = $"{_displays.Count} display(s) detected.";
    }

    private async void OnScanDisplays(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning...";
        await ScanAsync();
    }

    private async void OnAssignToSeat(object sender, RoutedEventArgs e)
    {
        if (DisplaysGrid.SelectedItem is not DisplayRow row)
        {
            MessageBox.Show("Select a display to assign first.", "Displays", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seats.Count == 0)
        {
            MessageBox.Show("No seats exist yet. Create one on the Seats page first.", "Displays", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new AssignDisplayToSeatWindow(_seatManager, row.Display, _seats)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await ScanAsync();
        }
    }

    private async void OnUnassign(object sender, RoutedEventArgs e)
    {
        if (DisplaysGrid.SelectedItem is not DisplayRow row)
        {
            MessageBox.Show("Select a display to unassign first.", "Displays", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.AssignedSeatName == null)
        {
            MessageBox.Show("This display isn't assigned to a seat.", "Displays", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _seatManager.UnassignDisplayFromSeatAsync(row.Display.DisplayId);
        await ScanAsync();
    }

    /// <summary>Wraps a Display with its resolved seat assignment for grid binding.</summary>
    private sealed class DisplayRow(Display display, string? assignedSeatName)
    {
        public Display Display { get; } = display;
        public string DeviceName => Display.DeviceName;
        public string Resolution => $"{Display.Width}x{Display.Height}";
        public string RefreshRateDisplay => $"{Display.RefreshRate} Hz";
        public string? ConnectionType => Display.ConnectionType;
        public bool IsPrimary => Display.IsPrimary;
        public string? AssignedSeatName { get; } = assignedSeatName;
        public string AssignedSeatDisplay => AssignedSeatName ?? "(unassigned)";
    }
}
