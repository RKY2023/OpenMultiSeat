using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;
using OpenMultiSeat.Devices;

namespace OpenMultiSeat.InputIsolation;

public interface IInputIsolationService
{
    Task<bool> IsIsolationEnabledAsync();
    Task<bool> EnableIsolationAsync();
    Task<bool> DisableIsolationAsync();
    Task<IReadOnlyList<InputDeviceBinding>> GetInputBindingsAsync();
    Task<InputDeviceBinding?> GetBindingForDeviceAsync(string deviceId);
    Task<IReadOnlyList<InputDeviceBinding>> GetBindingsForSeatAsync(string seatId);
    Task BindDeviceToSeatAsync(string deviceId, string seatId);
    Task UnbindDeviceFromSeatAsync(string deviceId);
    Task<IReadOnlyList<ValidationError>> ValidateIsolationConfigAsync();
    Task<InputIsolationStatus> GetIsolationStatusAsync();
}

public class InputDeviceBinding
{
    public required string DeviceId { get; set; }
    public required string SeatId { get; set; }
    public required string DeviceName { get; set; }
    public required InputDeviceType DeviceType { get; set; }
    public bool IsActive { get; set; }
    public DateTime BoundAt { get; set; }
}

public enum InputDeviceType
{
    Keyboard,
    Mouse,
    Touchpad,
    Gamepad,
    Unknown
}

public class InputIsolationStatus
{
    public bool IsEnabled { get; set; }
    public int TotalDevices { get; set; }
    public int BoundDevices { get; set; }
    public int UnboundDevices { get; set; }
    public Dictionary<string, int> DevicesPerSeat { get; set; } = [];
    public List<InputDeviceBinding> ActiveBindings { get; set; } = [];
}

public class InputIsolationService : IInputIsolationService
{
    private readonly ILogger<InputIsolationService> _logger;
    private readonly ISeatManager _seatManager;
    private readonly IDevicePersistence _devicePersistence;
    private readonly IInputIsolationPersistence _persistence;

    private bool _isolationEnabled = false;
    private Dictionary<string, InputDeviceBinding> _bindingCache = [];

    public InputIsolationService(
        ILogger<InputIsolationService> logger,
        ISeatManager seatManager,
        IDevicePersistence devicePersistence,
        IInputIsolationPersistence persistence)
    {
        _logger = logger;
        _seatManager = seatManager;
        _devicePersistence = devicePersistence;
        _persistence = persistence;
    }

    public async Task<bool> IsIsolationEnabledAsync()
    {
        var status = await _persistence.GetIsolationStatusAsync();
        _isolationEnabled = status;
        return _isolationEnabled;
    }

    public async Task<bool> EnableIsolationAsync()
    {
        if (_isolationEnabled)
        {
            _logger.LogInformation("Input isolation already enabled");
            return true;
        }

        var errors = await ValidateIsolationConfigAsync();
        var criticalErrors = errors.Where(e => e.Severity == ValidationSeverity.Critical).ToList();

        if (criticalErrors.Count > 0)
        {
            _logger.LogError($"Cannot enable isolation: {criticalErrors.Count} critical error(s)");
            foreach (var error in criticalErrors)
            {
                _logger.LogError($"  • {error.Message}");
            }
            return false;
        }

        await _persistence.SetIsolationStatusAsync(true);
        _isolationEnabled = true;
        _logger.LogInformation("✓ Input isolation enabled");
        return true;
    }

    public async Task<bool> DisableIsolationAsync()
    {
        if (!_isolationEnabled)
        {
            _logger.LogInformation("Input isolation already disabled");
            return true;
        }

        await _persistence.SetIsolationStatusAsync(false);
        _isolationEnabled = false;
        _logger.LogInformation("✓ Input isolation disabled");
        return true;
    }

    public async Task<IReadOnlyList<InputDeviceBinding>> GetInputBindingsAsync()
    {
        await RefreshBindingCacheAsync();
        return _bindingCache.Values.ToList();
    }

    public async Task<InputDeviceBinding?> GetBindingForDeviceAsync(string deviceId)
    {
        await RefreshBindingCacheAsync();
        return _bindingCache.TryGetValue(deviceId, out var binding) ? binding : null;
    }

    public async Task<IReadOnlyList<InputDeviceBinding>> GetBindingsForSeatAsync(string seatId)
    {
        await RefreshBindingCacheAsync();
        return _bindingCache.Values
            .Where(b => b.SeatId == seatId)
            .ToList();
    }

    public async Task BindDeviceToSeatAsync(string deviceId, string seatId)
    {
        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        var device = await _devicePersistence.GetDeviceByStableIdAsync(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Device '{deviceId}' not found");

        var deviceType = DetermineDeviceType(device);

        if (_bindingCache.TryGetValue(deviceId, out var existing))
        {
            if (existing.SeatId == seatId)
            {
                _logger.LogInformation($"Device {deviceId} already bound to seat {seatId}");
                return;
            }

            throw new InvalidOperationException(
                $"Device '{deviceId}' already bound to seat '{existing.SeatId}'");
        }

        var binding = new InputDeviceBinding
        {
            DeviceId = deviceId,
            SeatId = seatId,
            DeviceName = device.ProductName ?? "Unknown Device",
            DeviceType = deviceType,
            IsActive = true,
            BoundAt = DateTime.UtcNow
        };

        await _persistence.SaveBindingAsync(binding);
        _bindingCache[deviceId] = binding;

        _logger.LogInformation($"Device {deviceId} bound to seat {seatId}");
    }

    public async Task UnbindDeviceFromSeatAsync(string deviceId)
    {
        if (!_bindingCache.TryGetValue(deviceId, out var binding))
        {
            _logger.LogWarning($"Device {deviceId} not bound to any seat");
            return;
        }

        await _persistence.DeleteBindingAsync(deviceId);
        _bindingCache.Remove(deviceId);

        _logger.LogInformation($"Device {deviceId} unbound from seat {binding.SeatId}");
    }

    public async Task<IReadOnlyList<ValidationError>> ValidateIsolationConfigAsync()
    {
        var errors = new List<ValidationError>();
        await RefreshBindingCacheAsync();

        var devices = await _devicePersistence.GetAllDevicesAsync();
        var seats = await _seatManager.GetAllSeatsAsync();

        // Check for devices bound to non-existent seats
        var allBoundDevices = new HashSet<string>();

        foreach (var binding in _bindingCache.Values)
        {
            var seat = seats.FirstOrDefault(s => s.Id == binding.SeatId);
            if (seat == null)
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_SEAT",
                    Message = $"Input device '{binding.DeviceName}' bound to non-existent seat '{binding.SeatId}'",
                    AffectedSeat = binding.SeatId,
                    AffectedDevice = binding.DeviceId,
                    Severity = ValidationSeverity.Critical
                });
            }
            else
            {
                allBoundDevices.Add(binding.DeviceId);
            }
        }

        // Check for duplicate bindings (same device bound multiple times)
        var deviceBindingCounts = _bindingCache.Values
            .GroupBy(b => b.DeviceId)
            .Where(g => g.Count() > 1);

        foreach (var group in deviceBindingCounts)
        {
            errors.Add(new ValidationError
            {
                Code = "DUPLICATE_DEVICE_BINDING",
                Message = $"Device '{group.Key}' bound to multiple seats",
                AffectedDevice = group.Key,
                Severity = ValidationSeverity.Critical
            });
        }

        // Warn about keyboards and mice per seat
        var seatsWithoutKeyboard = new List<string>();
        var seatsWithoutMouse = new List<string>();

        foreach (var seat in seats)
        {
            var seatBindings = _bindingCache.Values.Where(b => b.SeatId == seat.Id).ToList();
            var hasKeyboard = seatBindings.Any(b => b.DeviceType == InputDeviceType.Keyboard);
            var hasMouse = seatBindings.Any(b => b.DeviceType == InputDeviceType.Mouse);

            if (!hasKeyboard)
                seatsWithoutKeyboard.Add(seat.Id);
            if (!hasMouse)
                seatsWithoutMouse.Add(seat.Id);
        }

        foreach (var seatId in seatsWithoutKeyboard)
        {
            errors.Add(new ValidationError
            {
                Code = "NO_KEYBOARD",
                Message = $"Seat '{seatId}' has no keyboard assigned",
                AffectedSeat = seatId,
                Severity = ValidationSeverity.Warning
            });
        }

        foreach (var seatId in seatsWithoutMouse)
        {
            errors.Add(new ValidationError
            {
                Code = "NO_MOUSE",
                Message = $"Seat '{seatId}' has no mouse assigned",
                AffectedSeat = seatId,
                Severity = ValidationSeverity.Warning
            });
        }

        return errors;
    }

    public async Task<InputIsolationStatus> GetIsolationStatusAsync()
    {
        await RefreshBindingCacheAsync();
        var devices = await _devicePersistence.GetAllDevicesAsync();
        var seats = await _seatManager.GetAllSeatsAsync();

        var devicesPerSeat = new Dictionary<string, int>();
        foreach (var seat in seats)
        {
            devicesPerSeat[seat.Id] = _bindingCache.Values.Count(b => b.SeatId == seat.Id);
        }

        return new InputIsolationStatus
        {
            IsEnabled = _isolationEnabled,
            TotalDevices = devices.Count,
            BoundDevices = _bindingCache.Count,
            UnboundDevices = devices.Count - _bindingCache.Count,
            DevicesPerSeat = devicesPerSeat,
            ActiveBindings = _bindingCache.Values.ToList()
        };
    }

    private InputDeviceType DetermineDeviceType(Device device)
    {
        var name = (device.ProductName ?? "").ToLower();

        if (name.Contains("keyboard") || device.HardwareId?.Contains("Keyboard") == true)
            return InputDeviceType.Keyboard;
        if (name.Contains("mouse") || device.HardwareId?.Contains("Mouse") == true)
            return InputDeviceType.Mouse;
        if (name.Contains("touchpad") || name.Contains("trackpad"))
            return InputDeviceType.Touchpad;
        if (name.Contains("gamepad") || device.HardwareId?.Contains("Gamepad") == true)
            return InputDeviceType.Gamepad;

        return InputDeviceType.Unknown;
    }

    private async Task RefreshBindingCacheAsync()
    {
        try
        {
            var bindings = await _persistence.GetAllBindingsAsync();
            _bindingCache = bindings.ToDictionary(b => b.DeviceId);
            _isolationEnabled = await _persistence.GetIsolationStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing binding cache");
        }
    }
}
