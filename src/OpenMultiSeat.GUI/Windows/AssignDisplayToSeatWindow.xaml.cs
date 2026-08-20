using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>Assigns one enumerated display to a seat via ISeatManager.AssignDisplayToSeatAsync.</summary>
public partial class AssignDisplayToSeatWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly Display _display;

    public AssignDisplayToSeatWindow(ISeatManager seatManager, Display display, IReadOnlyList<Seat> seats)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _display = display;

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
