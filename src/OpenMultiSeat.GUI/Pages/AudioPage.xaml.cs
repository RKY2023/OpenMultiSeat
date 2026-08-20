using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Real audio device list + seat assignment. AudioManager/AudioPersistence already existed,
/// fully built and correct (unlike SeatManager, AudioManager's duplicate-assignment guard is
/// reloaded from persistence on every call, not just within one instance's lifetime) — only
/// AudioDeviceEnumerator was a stub returning empty lists for both playback and recording.
/// </summary>
public partial class AudioPage : Page
{
    private readonly IAudioManager _audioManager;
    private readonly ISeatManager _seatManager;
    private IReadOnlyList<AudioDevice> _devices = [];
    private IReadOnlyList<Seat> _seats = [];

    public AudioPage()
    {
        InitializeComponent();

        var audioEnumerator = new AudioDeviceEnumerator(GuiLoggerFactory.Instance.CreateLogger<AudioDeviceEnumerator>());
        var audioPersistence = new AudioPersistence(GuiLoggerFactory.Instance.CreateLogger<AudioPersistence>());
        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        var devicePersistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        _seatManager = new SeatManager(GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, devicePersistence);
        _audioManager = new AudioManager(GuiLoggerFactory.Instance.CreateLogger<AudioManager>(), audioEnumerator, _seatManager, audioPersistence);

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _devices = await _audioManager.GetAudioDevicesAsync();
        _seats = await _seatManager.GetAllSeatsAsync();
        var topology = await _audioManager.GetAudioTopologyAsync();

        var assignedTo = new Dictionary<string, string>();
        foreach (var seat in _seats)
        {
            if (!topology.SeatAudioMapping.TryGetValue(seat.Id, out var assignment))
                continue;

            foreach (var deviceId in assignment.PlaybackDevices.Concat(assignment.RecordingDevices))
                assignedTo[deviceId] = seat.Name;
        }

        if (_devices.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            AudioGrid.Visibility = Visibility.Collapsed;
            StatusText.Text = string.Empty;
            return;
        }

        EmptyStateText.Visibility = Visibility.Collapsed;
        AudioGrid.Visibility = Visibility.Visible;
        AudioGrid.ItemsSource = _devices
            .Select(d => new AudioRow(d, assignedTo.GetValueOrDefault(d.DeviceId)))
            .ToList();
        StatusText.Text = $"{_devices.Count} audio device(s).";
    }

    private async void OnScanDevices(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning...";
        await LoadAsync();
    }

    private async void OnAssignToSeat(object sender, RoutedEventArgs e)
    {
        if (AudioGrid.SelectedItem is not AudioRow row)
        {
            MessageBox.Show("Select an audio device to assign first.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seats.Count == 0)
        {
            MessageBox.Show("No seats exist yet. Create one on the Seats page first.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new AssignAudioDeviceToSeatWindow(_audioManager, row.Device, _seats)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async void OnUnassign(object sender, RoutedEventArgs e)
    {
        if (AudioGrid.SelectedItem is not AudioRow row)
        {
            MessageBox.Show("Select an audio device to unassign first.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.AssignedSeatName == null)
        {
            MessageBox.Show("This device isn't assigned to a seat.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _audioManager.UnassignAudioDeviceFromSeatAsync(row.Device.DeviceId);
        await LoadAsync();
    }

    /// <summary>Wraps an AudioDevice with its resolved seat assignment for grid binding.</summary>
    private sealed class AudioRow(AudioDevice device, string? assignedSeatName)
    {
        public AudioDevice Device { get; } = device;
        public string FriendlyName => Device.FriendlyName;
        public AudioDeviceType Type => Device.Type;
        public bool IsDefault => Device.IsDefault;
        public string? AssignedSeatName { get; } = assignedSeatName;
        public string AssignedSeatDisplay => AssignedSeatName ?? "(unassigned)";
    }
}
