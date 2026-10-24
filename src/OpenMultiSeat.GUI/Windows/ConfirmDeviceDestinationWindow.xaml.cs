using System.Windows;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// ASTER's "Confirm Device Destination" dialog, single-resource version: shown before an
/// already-assigned resource (device/display/audio endpoint) actually moves to a different seat,
/// so the admin sees the before/after instead of the move happening silently. ASTER's own version
/// batches multiple drag-and-dropped devices into one table with per-row checkboxes; this covers
/// one resource at a time, matching how assignment already works everywhere else in this GUI (one
/// resource, one seat, per action) rather than introducing multi-select/drag-and-drop alongside it.
/// </summary>
public partial class ConfirmDeviceDestinationWindow : Window
{
    public ConfirmDeviceDestinationWindow(string resourceName, string currentSeatName, string newSeatName)
    {
        InitializeComponent();
        TitleText.Text = $"Move \"{resourceName}\" to a different seat?";
        CurrentSeatText.Text = currentSeatName;
        NewSeatText.Text = newSeatName;
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
