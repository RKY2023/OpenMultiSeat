using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Assigns one already-scanned device (a DeviceRecord from the Devices page) to a seat, via
/// ISeatManager.AssignDeviceToSeatAsync — which already existed and was never called from any
/// GUI page. AssignDeviceToSeatAsync only accepts Keyboard/Mouse (anything else throws), so this
/// asks explicitly rather than trusting the device's own Raw-Input-detected type: many real HID
/// devices (see docs/known-issues.md) get classified as "Other" by Windows Raw Input even when
/// WMI's friendly name clearly identifies them as a mouse or keyboard.
/// </summary>
public partial class AssignDeviceToSeatWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly DeviceRecord _device;
    private readonly string? _currentSeatName;

    /// <param name="currentSeatName">The seat this device is already assigned to, if any — pass
    /// null for a first-time assignment. When set and the admin picks a *different* seat, a
    /// Confirm Device Destination prompt shows before the device actually moves (see
    /// ConfirmDeviceDestinationWindow); picking the same seat is a no-op either way.</param>
    public AssignDeviceToSeatWindow(ISeatManager seatManager, DeviceRecord device, IReadOnlyList<Seat> seats, string? currentSeatName = null)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _device = device;
        _currentSeatName = currentSeatName;

        TitleText.Text = $"Assign \"{device.ProductName}\" to a seat";
        SeatComboBox.ItemsSource = seats;
        if (seats.Count > 0)
            SeatComboBox.SelectedIndex = 0;

        // Default the type toggle to whatever Windows Raw Input already guessed, when it's one
        // of the two types this assignment actually supports.
        if (string.Equals(device.DeviceType, nameof(InputDeviceType.Mouse), StringComparison.OrdinalIgnoreCase))
            MouseRadio.IsChecked = true;
    }

    private async void OnAssign(object sender, RoutedEventArgs e)
    {
        if (SeatComboBox.SelectedItem is not Seat seat)
        {
            ShowError("Select a seat.");
            return;
        }

        if (_currentSeatName != null && !string.Equals(_currentSeatName, seat.Name, StringComparison.Ordinal))
        {
            var confirm = new ConfirmDeviceDestinationWindow(_device.ProductName ?? "this device", _currentSeatName, seat.Name) { Owner = this };
            if (confirm.ShowDialog() != true)
                return;

            await _seatManager.UnassignDeviceFromSeatAsync(_device.StableId);
        }

        var type = MouseRadio.IsChecked == true ? InputDeviceType.Mouse : InputDeviceType.Keyboard;

        try
        {
            await _seatManager.AssignDeviceToSeatAsync(seat.Id, _device.StableId, type);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            // Most likely InvalidOperationException — device already assigned to another seat.
            ShowError(ex.Message);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
