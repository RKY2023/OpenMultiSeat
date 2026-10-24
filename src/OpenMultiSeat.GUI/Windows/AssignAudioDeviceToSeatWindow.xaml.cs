using System.Windows;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Assigns one enumerated audio endpoint to a seat via IAudioManager.AssignAudioDeviceToSeatAsync.
/// Unlike device assignment (keyboard/mouse), there's no type ambiguity to ask the user about —
/// Windows Core Audio endpoints are unambiguously Render (playback) or Capture (recording), so
/// the role is derived directly from the device's own AudioDevice.Type rather than offered as a
/// choice; AudioDeviceRole.BiDirectional is intentionally not exposed here since it would add a
/// pure-playback or pure-recording endpoint to both lists, which doesn't reflect what the
/// hardware can actually do.
/// </summary>
public partial class AssignAudioDeviceToSeatWindow : Window
{
    private readonly IAudioManager _audioManager;
    private readonly AudioDevice _device;
    private readonly string? _currentSeatName;

    /// <param name="currentSeatName">The seat this endpoint is already assigned to, if any — see
    /// AssignDeviceToSeatWindow's matching parameter for the confirm-before-move behavior.</param>
    public AssignAudioDeviceToSeatWindow(IAudioManager audioManager, AudioDevice device, IReadOnlyList<Seat> seats, string? currentSeatName = null)
    {
        InitializeComponent();
        _audioManager = audioManager;
        _device = device;
        _currentSeatName = currentSeatName;

        TitleText.Text = $"Assign \"{device.FriendlyName}\" ({device.Type}) to a seat";
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
            var confirm = new ConfirmDeviceDestinationWindow(_device.FriendlyName, _currentSeatName, seat.Name) { Owner = this };
            if (confirm.ShowDialog() != true)
                return;

            await _audioManager.UnassignAudioDeviceFromSeatAsync(_device.DeviceId);
        }

        var role = _device.Type == AudioDeviceType.Recording ? AudioDeviceRole.Recording : AudioDeviceRole.Playback;

        try
        {
            await _audioManager.AssignAudioDeviceToSeatAsync(seat.Id, _device.DeviceId, role);
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
