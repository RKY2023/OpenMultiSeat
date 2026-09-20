using System.Windows;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>Small modal for creating a new seat. The Windows-user assignment step (matching
/// ASTER's "User Account for Workstation" dialog) is intentionally not part of this form — that's
/// a separate, still-unbuilt page (see docs/control-panel/user-account-for-workstation.md); a seat
/// created here starts with no Windows user assigned, same as ValidateConfigurationAsync already
/// expects and flags as a warning until one is set.</summary>
public partial class CreateSeatWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly IReadOnlyList<Seat> _existingSeats;

    public CreateSeatWindow(ISeatManager seatManager, IReadOnlyList<Seat> existingSeats)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _existingSeats = existingSeats;
        Loaded += (_, _) => NameTextBox.Focus();
    }

    private async void OnCreate(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Enter a name for the seat.");
            return;
        }

        if (_existingSeats.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError($"A seat named \"{name}\" already exists.");
            return;
        }

        var seatId = $"seat-{Guid.NewGuid():N}"[..13];

        try
        {
            // No Windows user assigned yet — that's a separate, not-yet-built assignment step.
            await _seatManager.CreateSeatAsync(seatId, name, string.Empty);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't create the seat: {ex.Message}");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
