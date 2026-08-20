using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Audio;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.Displays;
using OpenMultiSeat.GUI.Windows;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// The real "unified Workplaces view" gap flagged earlier: Devices/Displays/Audio each got their
/// own assignment page, and Seats → Configure... consolidated those per-seat — but neither is
/// ASTER's actual "System" pool, which shows every device/display/audio endpoint across *all*
/// seats in one shared list. This page is that: one grid, every resource kind, with generic
/// assign/unassign reusing the exact same ISeatManager/IAudioManager calls the other pages call.
/// </summary>
public partial class SystemPage : Page
{
    private readonly ISeatManager _seatManager;
    private readonly IDevicePersistence _devicePersistence;
    private readonly IDisplayEnumerator _displayEnumerator;
    private readonly IAudioManager _audioManager;
    private IReadOnlyList<Seat> _seats = [];
    private List<SystemRow> _rows = [];

    public SystemPage()
    {
        InitializeComponent();

        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        _devicePersistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        _seatManager = new SeatManager(GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, _devicePersistence);
        _displayEnumerator = new DisplayEnumerator(GuiLoggerFactory.Instance.CreateLogger<DisplayEnumerator>());

        var audioEnumerator = new AudioDeviceEnumerator(GuiLoggerFactory.Instance.CreateLogger<AudioDeviceEnumerator>());
        var audioPersistence = new AudioPersistence(GuiLoggerFactory.Instance.CreateLogger<AudioPersistence>());
        _audioManager = new AudioManager(GuiLoggerFactory.Instance.CreateLogger<AudioManager>(), audioEnumerator, _seatManager, audioPersistence);

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        StatusText.Text = "Loading...";
        _seats = await _seatManager.GetAllSeatsAsync();
        var seatNameById = _seats.ToDictionary(s => s.Id, s => s.Name);

        var rows = new List<SystemRow>();

        // Devices (keyboard/mouse + camera/USB/Bluetooth)
        var devices = await _devicePersistence.GetAllDevicesAsync();
        var deviceAssignedTo = new Dictionary<string, string>();
        foreach (var seat in _seats)
            foreach (var id in seat.KeyboardIds.Concat(seat.MouseIds).Concat(seat.OtherDeviceIds))
                deviceAssignedTo[id] = seat.Name;

        foreach (var d in devices)
        {
            rows.Add(new SystemRow
            {
                Kind = SystemRowKind.Device,
                Name = d.ProductName ?? "(unknown device)",
                Detail = d.DeviceType,
                ResourceId = d.StableId,
                AssignedSeatName = deviceAssignedTo.GetValueOrDefault(d.StableId)
            });
        }

        // Displays
        var displays = await _displayEnumerator.EnumerateDisplaysAsync();
        var displayAssignedTo = new Dictionary<string, string>();
        foreach (var seat in _seats)
            foreach (var id in seat.DisplayIds)
                displayAssignedTo[id] = seat.Name;

        foreach (var d in displays)
        {
            rows.Add(new SystemRow
            {
                Kind = SystemRowKind.Display,
                Name = d.DeviceName,
                Detail = $"{d.Width}x{d.Height}",
                ResourceId = d.DisplayId,
                AssignedSeatName = displayAssignedTo.GetValueOrDefault(d.DisplayId)
            });
        }

        // Audio
        var audioDevices = await _audioManager.GetAudioDevicesAsync();
        var topology = await _audioManager.GetAudioTopologyAsync();
        var audioAssignedTo = new Dictionary<string, string>();
        foreach (var kvp in topology.SeatAudioMapping)
        {
            if (!seatNameById.TryGetValue(kvp.Key, out var seatName))
                continue;
            foreach (var id in kvp.Value.PlaybackDevices.Concat(kvp.Value.RecordingDevices))
                audioAssignedTo[id] = seatName;
        }

        foreach (var d in audioDevices)
        {
            rows.Add(new SystemRow
            {
                Kind = SystemRowKind.Audio,
                Name = d.FriendlyName,
                Detail = d.Type.ToString(),
                ResourceId = d.DeviceId,
                AssignedSeatName = audioAssignedTo.GetValueOrDefault(d.DeviceId)
            });
        }

        _rows = rows;
        SystemGrid.ItemsSource = rows;
        StatusText.Text = $"{rows.Count} resource(s) — {rows.Count(r => r.AssignedSeatName != null)} assigned, {rows.Count(r => r.AssignedSeatName == null)} unassigned.";
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void OnAssignSelected(object sender, RoutedEventArgs e)
    {
        if (SystemGrid.SelectedItem is not SystemRow row)
        {
            MessageBox.Show("Select a resource first.", "System", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seats.Count == 0)
        {
            MessageBox.Show("No seats exist yet. Create one on the Seats page first.", "System", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var picker = new SeatPickerWindow($"Assign \"{row.Name}\" to a seat", _seats) { Owner = Window.GetWindow(this) };
        if (picker.ShowDialog() != true || picker.SelectedSeat == null)
            return;

        try
        {
            switch (row.Kind)
            {
                case SystemRowKind.Device:
                    if (row.Detail != null && GeneralDeviceEnumerator.GeneralDeviceClasses.Contains(row.Detail))
                    {
                        await _seatManager.AssignOtherDeviceToSeatAsync(picker.SelectedSeat.Id, row.ResourceId);
                    }
                    else
                    {
                        var type = string.Equals(row.Detail, "Mouse", StringComparison.OrdinalIgnoreCase)
                            ? InputDeviceType.Mouse
                            : InputDeviceType.Keyboard;
                        await _seatManager.AssignDeviceToSeatAsync(picker.SelectedSeat.Id, row.ResourceId, type);
                    }
                    break;

                case SystemRowKind.Display:
                    await _seatManager.AssignDisplayToSeatAsync(picker.SelectedSeat.Id, row.ResourceId);
                    break;

                case SystemRowKind.Audio:
                    var role = string.Equals(row.Detail, "Recording", StringComparison.OrdinalIgnoreCase)
                        ? AudioDeviceRole.Recording
                        : AudioDeviceRole.Playback;
                    await _audioManager.AssignAudioDeviceToSeatAsync(picker.SelectedSeat.Id, row.ResourceId, role);
                    break;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Assign", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnUnassignSelected(object sender, RoutedEventArgs e)
    {
        if (SystemGrid.SelectedItem is not SystemRow row)
        {
            MessageBox.Show("Select a resource first.", "System", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.AssignedSeatName == null)
        {
            MessageBox.Show("This resource isn't assigned to a seat.", "System", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        switch (row.Kind)
        {
            case SystemRowKind.Device:
                if (row.Detail != null && GeneralDeviceEnumerator.GeneralDeviceClasses.Contains(row.Detail))
                    await _seatManager.UnassignOtherDeviceFromSeatAsync(row.ResourceId);
                else
                    await _seatManager.UnassignDeviceFromSeatAsync(row.ResourceId);
                break;

            case SystemRowKind.Display:
                await _seatManager.UnassignDisplayFromSeatAsync(row.ResourceId);
                break;

            case SystemRowKind.Audio:
                await _audioManager.UnassignAudioDeviceFromSeatAsync(row.ResourceId);
                break;
        }

        await LoadAsync();
    }

    private enum SystemRowKind { Device, Display, Audio }

    private sealed class SystemRow
    {
        public required SystemRowKind Kind { get; init; }
        public required string Name { get; init; }
        public string? Detail { get; init; }
        public required string ResourceId { get; init; }
        public string? AssignedSeatName { get; init; }
        public string AssignedSeatDisplay => AssignedSeatName ?? "(unassigned)";
    }
}
