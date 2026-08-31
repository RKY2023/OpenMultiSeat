using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>Assigns one enumerated display to a seat via ISeatManager.AssignDisplayToSeatAsync.</summary>
public partial class AssignDisplayToSeatWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly Display _display;
    private readonly string? _currentSeatName;

    /// <param name="currentSeatName">The seat this display is already assigned to, if any — see
    /// AssignDeviceToSeatWindow's matching parameter for the confirm-before-move behavior.</param>
    public AssignDisplayToSeatWindow(ISeatManager seatManager, Display display, IReadOnlyList<Seat> seats, string? currentSeatName = null)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _display = display;
        _currentSeatName = currentSeatName;

        TitleText.Text = $"Assign \"{display.DeviceName}\" to a seat";
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

        if (_currentSeatName != null && !string.Equals(_currentSeatName, seat.Name, StringComparison.Ordinal))
        {
            var confirm = new ConfirmDeviceDestinationWindow(_display.DeviceName, _currentSeatName, seat.Name) { Owner = this };
            if (confirm.ShowDialog() != true)
                return;

            await _seatManager.UnassignDisplayFromSeatAsync(_display.DisplayId);
        }

        try
        {
            await _seatManager.AssignDisplayToSeatAsync(seat.Id, _display.DisplayId);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            // Most likely InvalidOperationException — display already assigned to another seat.
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
