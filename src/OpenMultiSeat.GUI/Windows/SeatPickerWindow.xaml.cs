using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>Minimal reusable "pick a seat" dialog, used by SystemPage's generic assign action
/// (which resource type doesn't require its own type-specific chooser like AssignDeviceToSeatWindow's
/// Keyboard/Mouse toggle does).</summary>
public partial class SeatPickerWindow : Window
{
    public Seat? SelectedSeat { get; private set; }

    public SeatPickerWindow(string title, IReadOnlyList<Seat> seats)
    {
        InitializeComponent();
        TitleText.Text = title;
        SeatComboBox.ItemsSource = seats;
        if (seats.Count > 0)
            SeatComboBox.SelectedIndex = 0;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (SeatComboBox.SelectedItem is not Seat seat)
            return;

        SelectedSeat = seat;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
