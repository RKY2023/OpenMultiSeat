using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Real seat list + create/delete, backed directly by ISeatManager/ISeatPersistence — same
/// direct-persistence pattern as DevicesPage and AssignCpuCoresWindow, since no GUI page has IPC
/// wiring to the Service yet. Previously this page was a heading and a single button that popped
/// a MessageBox; there was no way to create a seat from the GUI at all, which every other
/// seat-dependent feature (device/display/audio assignment, CPU-core affinity) needs.
/// </summary>
public partial class SeatsPage : Page
{
    private readonly ISeatManager _seatManager;
    private IReadOnlyList<Seat> _seats = [];

    public SeatsPage()
    {
        InitializeComponent();

        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        var devicePersistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        _seatManager = new SeatManager(
            GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, devicePersistence);

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _seats = await _seatManager.GetAllSeatsAsync();

        if (_seats.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            SeatsGrid.Visibility = Visibility.Collapsed;
            StatusText.Text = string.Empty;
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;
        SeatsGrid.Visibility = Visibility.Visible;
        SeatsGrid.ItemsSource = _seats.Select(s => new SeatRow(s)).ToList();
        StatusText.Text = $"{_seats.Count} seat(s).";
    }

    private async void OnCreateSeat(object sender, RoutedEventArgs e)
    {
        var window = new CreateSeatWindow(_seatManager, _seats)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async void OnDeleteSeat(object sender, RoutedEventArgs e)
    {
        if (SeatsGrid.SelectedItem is not SeatRow row)
        {
            MessageBox.Show("Select a seat to delete first.", "Seats", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Delete seat \"{row.Name}\"? Any devices assigned to it will become unassigned.",
            "Delete Seat", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        await _seatManager.DeleteSeatAsync(row.Seat.Id);
        await LoadAsync();
    }

    private async void OnRefresh(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async void OnUserAccount(object sender, RoutedEventArgs e)
    {
        if (SeatsGrid.SelectedItem is not SeatRow row)
        {
            MessageBox.Show("Select a seat first.", "Seats", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new UserAccountWindow(_seatManager, row.Seat)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    /// <summary>Flattens a Seat's list-valued fields into display-ready properties for the grid.</summary>
    private sealed class SeatRow(Seat seat)
    {
        public Seat Seat { get; } = seat;
        public string Name => Seat.Name;
        public string WindowsUserDisplay
        {
            get
            {
                if (Seat.DisplayLoginDialog || string.IsNullOrWhiteSpace(Seat.WindowsUser))
                    return "(login prompt)";
                return Seat.WindowsDomain != null ? $"{Seat.WindowsDomain}\\{Seat.WindowsUser}" : Seat.WindowsUser;
            }
        }
        public int DeviceCount => Seat.KeyboardIds.Count + Seat.MouseIds.Count;
        public int DisplayCount => Seat.DisplayIds.Count;
        public string Status => Seat.Status.ToString();
        public bool Enabled => Seat.Enabled;
    }
}
