using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;
using OpenMultiSeat.GUI.Windows;
using OpenMultiSeat.InputIsolation;

namespace OpenMultiSeat.GUI.Pages;

/// <summary>
/// Wires to the real <see cref="IInputIsolationService"/> backend instead of the former
/// MessageBox-only stub — matches ASTER's Input Devices Switch conceptually (keyboard/mouse
/// routed exclusively to one seat) but as a bind/unbind list + isolation toggle rather than
/// ASTER's specific hotkey-rebind UI; see docs/control-panel/input-devices-switch.md for what a
/// literal hotkey-capture field would still need on top of this.
///
/// Known overlap, not resolved here: <see cref="ISeatManager.AssignDeviceToSeatAsync"/> (used by
/// the Devices page, writing <c>Seat.KeyboardIds</c>/<c>MouseIds</c>) and
/// <see cref="IInputIsolationService.BindDeviceToSeatAsync"/> (used here, writing a separate
/// <c>input-bindings.json</c> via <see cref="InputIsolationPersistence"/>) are two independent
/// stores that both mean "this keyboard/mouse belongs to this seat" and can disagree — assigning a
/// device on the Devices page does not bind it here, and vice versa. See
/// docs/known-issues.md for the full note; reconciling them is a separate, bigger decision than
/// wiring this page up.
/// </summary>
public partial class InputPage : Page
{
    private readonly IDevicePersistence _devicePersistence;
    private readonly ISeatManager _seatManager;
    private readonly IInputIsolationService _isolationService;
    private IReadOnlyList<Seat> _seats = [];

    public InputPage()
    {
        InitializeComponent();

        _devicePersistence = new DevicePersistence(GuiLoggerFactory.Instance.CreateLogger<DevicePersistence>());
        var seatPersistence = new SeatPersistence(GuiLoggerFactory.Instance.CreateLogger<SeatPersistence>());
        _seatManager = new SeatManager(GuiLoggerFactory.Instance.CreateLogger<SeatManager>(), seatPersistence, _devicePersistence);
        var isolationPersistence = new InputIsolationPersistence(GuiLoggerFactory.Instance.CreateLogger<InputIsolationPersistence>());
        _isolationService = new InputIsolationService(
            GuiLoggerFactory.Instance.CreateLogger<InputIsolationService>(), _seatManager, _devicePersistence, isolationPersistence);

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var devices = await _devicePersistence.GetAllDevicesAsync();
        _seats = await _seatManager.GetAllSeatsAsync();
        var bindings = await _isolationService.GetInputBindingsAsync();
        var bindingByDevice = bindings.ToDictionary(b => b.DeviceId);

        // Isolation routes keyboard/mouse input only — camera/USB/Bluetooth etc. (GeneralDeviceEnumerator's
        // classes) have no isolation concept and aren't shown here.
        var rows = devices
            .Where(d => d.DeviceType is "Keyboard" or "Mouse")
            .Select(d => new InputDeviceRow(d, bindingByDevice.GetValueOrDefault(d.StableId), _seats))
            .ToList();

        DevicesGrid.ItemsSource = rows;

        var enabled = await _isolationService.IsIsolationEnabledAsync();
        IsolationToggleButton.Content = enabled ? "Disable Isolation" : "Enable Isolation";
        StatusText.Text = rows.Count == 0
            ? "No keyboard/mouse devices registered yet — scan for devices on the Devices page first."
            : $"Isolation {(enabled ? "ENABLED" : "disabled")} — {bindings.Count} of {rows.Count} keyboard/mouse device(s) bound.";
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void OnToggleIsolation(object sender, RoutedEventArgs e)
    {
        if (await _isolationService.IsIsolationEnabledAsync())
        {
            await _isolationService.DisableIsolationAsync();
            await LoadAsync();
            return;
        }

        if (!await _isolationService.EnableIsolationAsync())
        {
            var errors = await _isolationService.ValidateIsolationConfigAsync();
            var critical = errors.Where(x => x.Severity == ValidationSeverity.Critical).ToList();
            MessageBox.Show(
                "Couldn't enable isolation — critical configuration problem(s):\n\n" +
                string.Join("\n", critical.Select(c => $"- {c.Message}")),
                "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await LoadAsync();
    }

    private async void OnBindToSeat(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InputDeviceRow row)
        {
            MessageBox.Show("Select a keyboard or mouse to bind first.", "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seats.Count == 0)
        {
            MessageBox.Show("No seats exist yet. Create one on the Seats page first.", "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var picker = new SeatPickerWindow($"Bind {row.ProductName} to Seat", _seats) { Owner = Window.GetWindow(this) };
        if (picker.ShowDialog() != true || picker.SelectedSeat == null)
            return;

        try
        {
            await _isolationService.BindDeviceToSeatAsync(row.Device.StableId, picker.SelectedSeat.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't bind: {ex.Message}", "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnUnbind(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InputDeviceRow row)
        {
            MessageBox.Show("Select a keyboard or mouse to unbind first.", "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.BoundSeatName == null)
        {
            MessageBox.Show("This device isn't bound to a seat.", "Input Isolation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _isolationService.UnbindDeviceFromSeatAsync(row.Device.StableId);
        await LoadAsync();
    }

    private async void OnValidate(object sender, RoutedEventArgs e)
    {
        var errors = await _isolationService.ValidateIsolationConfigAsync();
        if (errors.Count == 0)
        {
            MessageBox.Show("No issues found.", "Input Isolation — Validate", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var message = string.Join("\n", errors.Select(err => $"[{err.Severity}] {err.Message}"));
        MessageBox.Show(message, "Input Isolation — Validate", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>Wraps a keyboard/mouse DeviceRecord with its resolved isolation binding for grid
    /// binding — separate from DevicesPage's own DeviceRow, since "bound" here means an
    /// InputIsolation binding, not a SeatManager assignment (see the class doc comment).</summary>
    private sealed class InputDeviceRow(DeviceRecord device, InputDeviceBinding? binding, IReadOnlyList<Seat> seats)
    {
        public DeviceRecord Device { get; } = device;
        public string? ProductName => Device.ProductName;
        public string? DeviceType => Device.DeviceType;
        public string? HardwareId => Device.HardwareId;
        public string? BoundSeatName => binding == null ? null : seats.FirstOrDefault(s => s.Id == binding.SeatId)?.Name ?? binding.SeatId;
        public string BoundSeatDisplay => BoundSeatName ?? "(not bound)";
        public string BoundAtDisplay => binding == null ? string.Empty : binding.BoundAt.ToLocalTime().ToString("g");
    }
}
