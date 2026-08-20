using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Assigns a camera/USB/Bluetooth device (from GeneralDeviceEnumerator) to a seat via
/// ISeatManager.AssignOtherDeviceToSeatAsync — an ownership record only, not functional routing.
/// </summary>
public partial class AssignOtherDeviceToSeatWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly DeviceRecord _device;

    public AssignOtherDeviceToSeatWindow(ISeatManager seatManager, DeviceRecord device, IReadOnlyList<Seat> seats)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _device = device;

        TitleText.Text = $"Assign \"{device.ProductName}\" ({device.DeviceType}) to a seat";
        SeatComboBox.ItemsSource = seats;
        if (seats.Count > 0)
            SeatComboBox.SelectedIndex = 0;
    }

    private async void OnAssign(object sender, RoutedEventArgs e)
    {
        if (SeatComboBox.SelectedItem is not Seat seat)
        {
            ShowError("Select a seat.");
            return;
        }

        try
        {
            await _seatManager.AssignOtherDeviceToSeatAsync(seat.Id, _device.StableId);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
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
