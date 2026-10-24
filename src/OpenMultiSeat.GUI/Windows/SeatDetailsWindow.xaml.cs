using System.Windows;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// Consolidated per-seat view — the "unified Workplaces view" requested as a follow-up: today
/// device/display/audio assignment each live on their own page (Devices, Displays, Audio); this
/// shows and manages all three for one seat in a single screen instead, closer to ASTER's
/// per-workplace column layout. Reuses the same ISeatManager/IAudioManager assignment methods
/// those pages call — no new backend, just a different, seat-centric front door onto them.
/// Assign/unassign happens inline here rather than opening the per-resource Assign*Window dialogs
/// those other pages use, since this window is already scoped to one seat.
/// </summary>
public partial class SeatDetailsWindow : Window
{
    private readonly ISeatManager _seatManager;
    private readonly IDevicePersistence _devicePersistence;
    private readonly IDisplayEnumerator _displayEnumerator;
    private readonly IAudioManager _audioManager;
    private Seat _seat;

    private IReadOnlyList<DeviceRecord> _allDevices = [];
    private IReadOnlyList<Display> _allDisplays = [];
    private IReadOnlyList<AudioDevice> _allAudioDevices = [];
    private Dictionary<string, string> _deviceAssignedTo = [];
    private Dictionary<string, string> _displayAssignedTo = [];
    private Dictionary<string, string> _audioAssignedTo = [];

    public SeatDetailsWindow(
        ISeatManager seatManager,
        IDevicePersistence devicePersistence,
        IDisplayEnumerator displayEnumerator,
        IAudioManager audioManager,
        Seat seat)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _devicePersistence = devicePersistence;
        _displayEnumerator = displayEnumerator;
        _audioManager = audioManager;
        _seat = seat;

        TitleText.Text = seat.Name;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Re-fetch the seat itself so a change made via "User Account..." (a separate window on
        // top of this one) is reflected when it closes.
        _seat = await _seatManager.GetSeatAsync(_seat.Id) ?? _seat;
        AccountText.Text = _seat.DisplayLoginDialog || string.IsNullOrWhiteSpace(_seat.WindowsUser)
            ? "Windows account: (login prompt)"
            : $"Windows account: {(_seat.WindowsDomain != null ? $"{_seat.WindowsDomain}\\{_seat.WindowsUser}" : _seat.WindowsUser)}";

        var seats = await _seatManager.GetAllSeatsAsync();
        var seatNameById = seats.ToDictionary(s => s.Id, s => s.Name);

        // Devices
        _allDevices = await _devicePersistence.GetAllDevicesAsync();
        _deviceAssignedTo = [];
        foreach (var seat in seats)
        {
            foreach (var deviceId in seat.KeyboardIds.Concat(seat.MouseIds))
                _deviceAssignedTo[deviceId] = seat.Name;
        }

        var assignableDevices = _allDevices
            .Where(d => !GeneralDeviceEnumerator.GeneralDeviceClasses.Contains(d.DeviceType))
            .ToList();

        DevicesListBox.ItemsSource = assignableDevices
            .Where(d => _deviceAssignedTo.GetValueOrDefault(d.StableId) == _seat.Name)
            .Select(d => $"{d.ProductName} ({d.DeviceType})")
            .ToList();
        AvailableDevicesComboBox.ItemsSource = assignableDevices
            .Where(d => !_deviceAssignedTo.ContainsKey(d.StableId))
            .ToList();

        // Displays
        _allDisplays = await _displayEnumerator.EnumerateDisplaysAsync();
        _displayAssignedTo = [];
        foreach (var seat in seats)
        {
            foreach (var displayId in seat.DisplayIds)
                _displayAssignedTo[displayId] = seat.Name;
        }

        DisplaysListBox.ItemsSource = _allDisplays
            .Where(d => _displayAssignedTo.GetValueOrDefault(d.DisplayId) == _seat.Name)
            .Select(d => $"{d.DeviceName} ({d.Width}x{d.Height})")
            .ToList();
        AvailableDisplaysComboBox.ItemsSource = _allDisplays
            .Where(d => !_displayAssignedTo.ContainsKey(d.DisplayId))
            .ToList();

        // Audio
        _allAudioDevices = await _audioManager.GetAudioDevicesAsync();
        var topology = await _audioManager.GetAudioTopologyAsync();
        _audioAssignedTo = [];
        foreach (var kvp in topology.SeatAudioMapping)
        {
            if (!seatNameById.TryGetValue(kvp.Key, out var seatName))
                continue;
            foreach (var deviceId in kvp.Value.PlaybackDevices.Concat(kvp.Value.RecordingDevices))
                _audioAssignedTo[deviceId] = seatName;
        }

        AudioListBox.ItemsSource = _allAudioDevices
            .Where(d => _audioAssignedTo.GetValueOrDefault(d.DeviceId) == _seat.Name)
            .Select(d => $"{d.FriendlyName} ({d.Type})")
            .ToList();
        AvailableAudioComboBox.ItemsSource = _allAudioDevices
            .Where(d => !_audioAssignedTo.ContainsKey(d.DeviceId))
            .ToList();

        StatusText.Text = $"Loaded {DateTime.Now:t}";
    }

    private async void OnAssignDevice(object sender, RoutedEventArgs e)
    {
        if (AvailableDevicesComboBox.SelectedItem is not DeviceRecord device)
            return;

        var type = string.Equals(device.DeviceType, nameof(InputDeviceType.Mouse), StringComparison.OrdinalIgnoreCase)
            ? InputDeviceType.Mouse
            : InputDeviceType.Keyboard;

        try
        {
            await _seatManager.AssignDeviceToSeatAsync(_seat.Id, device.StableId, type);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Assign Device", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnUnassignDevice(object sender, RoutedEventArgs e)
    {
        var assigned = _allDevices.Where(d => _deviceAssignedTo.GetValueOrDefault(d.StableId) == _seat.Name).ToList();
        var index = DevicesListBox.SelectedIndex;
        if (index < 0 || index >= assigned.Count)
        {
            MessageBox.Show("Select a device to unassign first.", "Unassign", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _seatManager.UnassignDeviceFromSeatAsync(assigned[index].StableId);
        await LoadAsync();
    }

    private async void OnAssignDisplay(object sender, RoutedEventArgs e)
    {
        if (AvailableDisplaysComboBox.SelectedItem is not Display display)
            return;

        try
        {
            await _seatManager.AssignDisplayToSeatAsync(_seat.Id, display.DisplayId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Assign Display", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnUnassignDisplay(object sender, RoutedEventArgs e)
    {
        var assigned = _allDisplays.Where(d => _displayAssignedTo.GetValueOrDefault(d.DisplayId) == _seat.Name).ToList();
        var index = DisplaysListBox.SelectedIndex;
        if (index < 0 || index >= assigned.Count)
        {
            MessageBox.Show("Select a display to unassign first.", "Unassign", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _seatManager.UnassignDisplayFromSeatAsync(assigned[index].DisplayId);
        await LoadAsync();
    }

    private async void OnAssignAudio(object sender, RoutedEventArgs e)
    {
        if (AvailableAudioComboBox.SelectedItem is not AudioDevice device)
            return;

        var role = device.Type == AudioDeviceType.Recording ? AudioDeviceRole.Recording : AudioDeviceRole.Playback;

        try
        {
            await _audioManager.AssignAudioDeviceToSeatAsync(_seat.Id, device.DeviceId, role);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Assign Audio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnUnassignAudio(object sender, RoutedEventArgs e)
    {
        var assigned = _allAudioDevices.Where(d => _audioAssignedTo.GetValueOrDefault(d.DeviceId) == _seat.Name).ToList();
        var index = AudioListBox.SelectedIndex;
        if (index < 0 || index >= assigned.Count)
        {
            MessageBox.Show("Select an audio device to unassign first.", "Unassign", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _audioManager.UnassignAudioDeviceFromSeatAsync(assigned[index].DeviceId);
        await LoadAsync();
    }

    private async void OnUserAccount(object sender, RoutedEventArgs e)
    {
        var window = new UserAccountWindow(_seatManager, _seat)
        {
            Owner = this
        };
        window.ShowDialog();
        await LoadAsync();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
