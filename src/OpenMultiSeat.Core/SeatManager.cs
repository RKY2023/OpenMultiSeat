using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Core;

public interface ISeatManager
{
    Task<Seat> CreateSeatAsync(string seatId, string name, string windowsUser);
    Task<Seat?> GetSeatAsync(string seatId);
    Task<IReadOnlyList<Seat>> GetAllSeatsAsync();
    Task UpdateSeatAsync(Seat seat);
    Task DeleteSeatAsync(string seatId);
    Task AssignDeviceToSeatAsync(string seatId, string deviceId, InputDeviceType type);
    Task UnassignDeviceFromSeatAsync(string deviceId);
    Task AssignDisplayToSeatAsync(string seatId, string displayId);
    Task UnassignDisplayFromSeatAsync(string displayId);
    Task AssignOtherDeviceToSeatAsync(string seatId, string deviceId);
    Task UnassignOtherDeviceFromSeatAsync(string deviceId);
    Task<IReadOnlyList<ValidationError>> ValidateConfigurationAsync();
    Task<SeatConfiguration> ExportConfigurationAsync();
    Task ImportConfigurationAsync(SeatConfiguration config);
}

public class ValidationError
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public string? AffectedSeat { get; set; }
    public string? AffectedDevice { get; set; }
    public ValidationSeverity Severity { get; set; }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class SeatManager : ISeatManager
{
    private readonly ILogger<SeatManager> _logger;
    private readonly ISeatPersistence _persistence;
    private readonly IDevicePersistence _devicePersistence;

    private Dictionary<string, Seat> _seats = [];
    private Dictionary<string, string> _deviceToSeatMap = []; // deviceId -> seatId
    private Dictionary<string, string> _displayToSeatMap = []; // displayId -> seatId
    private Dictionary<string, string> _otherDeviceToSeatMap = []; // otherDeviceId -> seatId

    public SeatManager(
        ILogger<SeatManager> logger,
        ISeatPersistence persistence,
        IDevicePersistence devicePersistence)
    {
        _logger = logger;
        _persistence = persistence;
        _devicePersistence = devicePersistence;
    }

    public async Task<Seat> CreateSeatAsync(string seatId, string name, string windowsUser)
    {
        if (string.IsNullOrWhiteSpace(seatId))
            throw new ArgumentException("Seat ID cannot be empty", nameof(seatId));

        if (_seats.ContainsKey(seatId))
            throw new InvalidOperationException($"Seat '{seatId}' already exists");

        var seat = new Seat
        {
            Id = seatId,
            Name = name,
            WindowsUser = windowsUser,
            Status = SeatStatus.Configured,
            Enabled = true
        };

        _seats[seatId] = seat;
        await _persistence.SaveSeatAsync(seat);
        _logger.LogInformation($"Seat created: {seatId} ({name}) for user {windowsUser}");

        return seat;
    }

    public async Task<Seat?> GetSeatAsync(string seatId)
    {
        if (_seats.TryGetValue(seatId, out var seat))
            return seat;

        var persisted = await _persistence.GetSeatAsync(seatId);
        if (persisted != null)
            _seats[seatId] = persisted;

        return persisted;
    }

    public async Task<IReadOnlyList<Seat>> GetAllSeatsAsync()
    {
        var persisted = await _persistence.GetAllSeatsAsync();
        foreach (var seat in persisted)
        {
            _seats[seat.Id] = seat;

            // _deviceToSeatMap only ever grows via AssignDeviceToSeatAsync calls made within
            // this instance's own lifetime — it isn't otherwise derived from persisted state.
            // A fresh SeatManager (e.g. one instantiated per GUI page load) would start with an
            // empty map even though seats.json already has real assignments on disk, silently
            // defeating AssignDeviceToSeatAsync's "already assigned elsewhere" check and letting
            // the same device end up in two seats' KeyboardIds/MouseIds. Rebuild it here so any
            // call that loads seats keeps the map in sync with what's actually persisted.
            foreach (var deviceId in seat.KeyboardIds.Concat(seat.MouseIds))
            {
                _deviceToSeatMap[deviceId] = seat.Id;
            }

            foreach (var displayId in seat.DisplayIds)
            {
                _displayToSeatMap[displayId] = seat.Id;
            }

            foreach (var otherId in seat.OtherDeviceIds)
            {
                _otherDeviceToSeatMap[otherId] = seat.Id;
            }
        }
        return persisted;
    }

    public async Task UpdateSeatAsync(Seat seat)
    {
        if (!_seats.ContainsKey(seat.Id))
            throw new InvalidOperationException($"Seat '{seat.Id}' not found");

        _seats[seat.Id] = seat;
        await _persistence.SaveSeatAsync(seat);
        _logger.LogInformation($"Seat updated: {seat.Id}");
    }

    public async Task DeleteSeatAsync(string seatId)
    {
        if (!_seats.ContainsKey(seatId))
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        var seat = _seats[seatId];

        // Unassign all devices and displays from this seat
        var devicesToUnassign = _deviceToSeatMap
            .Where(kvp => kvp.Value == seatId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var deviceId in devicesToUnassign)
        {
            _deviceToSeatMap.Remove(deviceId);
        }

        var displaysToUnassign = _displayToSeatMap
            .Where(kvp => kvp.Value == seatId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var displayId in displaysToUnassign)
        {
            _displayToSeatMap.Remove(displayId);
        }

        var otherDevicesToUnassign = _otherDeviceToSeatMap
            .Where(kvp => kvp.Value == seatId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var otherId in otherDevicesToUnassign)
        {
            _otherDeviceToSeatMap.Remove(otherId);
        }

        _seats.Remove(seatId);
        await _persistence.DeleteSeatAsync(seatId);
        _logger.LogInformation($"Seat deleted: {seatId}");
    }

    public async Task AssignDeviceToSeatAsync(string seatId, string deviceId, InputDeviceType type)
    {
        var seat = await GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        var device = await _devicePersistence.GetDeviceByStableIdAsync(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Device '{deviceId}' not registered");

        // Check if device already assigned to another seat
        if (_deviceToSeatMap.TryGetValue(deviceId, out var existingSeat))
        {
            if (existingSeat == seatId)
            {
                _logger.LogInformation($"Device {deviceId} already assigned to seat {seatId}");
                return;
            }

            throw new InvalidOperationException(
                $"Device '{deviceId}' is already assigned to seat '{existingSeat}'");
        }

        // Add to appropriate list
        switch (type)
        {
            case InputDeviceType.Keyboard:
                if (!seat.KeyboardIds.Contains(deviceId))
                    seat.KeyboardIds.Add(deviceId);
                break;
            case InputDeviceType.Mouse:
                if (!seat.MouseIds.Contains(deviceId))
                    seat.MouseIds.Add(deviceId);
                break;
            default:
                throw new ArgumentException($"Unsupported device type: {type}");
        }

        _deviceToSeatMap[deviceId] = seatId;
        await UpdateSeatAsync(seat);
        _logger.LogInformation($"Device {deviceId} ({type}) assigned to seat {seatId}");
    }

    public async Task UnassignDeviceFromSeatAsync(string deviceId)
    {
        if (!_deviceToSeatMap.TryGetValue(deviceId, out var seatId))
        {
            _logger.LogWarning($"Device {deviceId} not assigned to any seat");
            return;
        }

        var seat = await GetSeatAsync(seatId);
        if (seat != null)
        {
            seat.KeyboardIds.Remove(deviceId);
            seat.MouseIds.Remove(deviceId);
            await UpdateSeatAsync(seat);
        }

        _deviceToSeatMap.Remove(deviceId);
        _logger.LogInformation($"Device {deviceId} unassigned from seat {seatId}");
    }

    public async Task AssignDisplayToSeatAsync(string seatId, string displayId)
    {
        var seat = await GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        // Unlike devices, there's no persisted display registry to validate displayId against —
        // OpenMultiSeat.Displays.DisplayEnumerator has no persistence layer, so displayId is
        // trusted as coming straight from a live EnumerateDisplaysAsync() call.
        if (_displayToSeatMap.TryGetValue(displayId, out var existingSeat))
        {
            if (existingSeat == seatId)
            {
                _logger.LogInformation($"Display {displayId} already assigned to seat {seatId}");
                return;
            }

            throw new InvalidOperationException(
                $"Display '{displayId}' is already assigned to seat '{existingSeat}'");
        }

        if (!seat.DisplayIds.Contains(displayId))
            seat.DisplayIds.Add(displayId);

        _displayToSeatMap[displayId] = seatId;
        await UpdateSeatAsync(seat);
        _logger.LogInformation($"Display {displayId} assigned to seat {seatId}");
    }

    public async Task UnassignDisplayFromSeatAsync(string displayId)
    {
        if (!_displayToSeatMap.TryGetValue(displayId, out var seatId))
        {
            _logger.LogWarning($"Display {displayId} not assigned to any seat");
            return;
        }

        var seat = await GetSeatAsync(seatId);
        if (seat != null)
        {
            seat.DisplayIds.Remove(displayId);
            await UpdateSeatAsync(seat);
        }

        _displayToSeatMap.Remove(displayId);
        _logger.LogInformation($"Display {displayId} unassigned from seat {seatId}");
    }

    /// <summary>
    /// Records that a non-input, non-display, non-audio device (camera, USB device, Bluetooth
    /// device/radio — anything from GeneralDeviceEnumerator) belongs to a seat. This is an
    /// ownership/organizational record only: unlike AssignDeviceToSeatAsync, nothing actually
    /// restricts the device to that seat's session — there's no InputIsolation-equivalent routing
    /// subsystem for arbitrary peripherals.
    /// </summary>
    public async Task AssignOtherDeviceToSeatAsync(string seatId, string deviceId)
    {
        var seat = await GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        var device = await _devicePersistence.GetDeviceByStableIdAsync(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Device '{deviceId}' not registered");

        if (_otherDeviceToSeatMap.TryGetValue(deviceId, out var existingSeat))
        {
            if (existingSeat == seatId)
            {
                _logger.LogInformation($"Device {deviceId} already assigned to seat {seatId}");
                return;
            }

            throw new InvalidOperationException(
                $"Device '{deviceId}' is already assigned to seat '{existingSeat}'");
        }

        if (!seat.OtherDeviceIds.Contains(deviceId))
            seat.OtherDeviceIds.Add(deviceId);

        _otherDeviceToSeatMap[deviceId] = seatId;
        await UpdateSeatAsync(seat);
        _logger.LogInformation($"Device {deviceId} assigned to seat {seatId} (ownership record only)");
    }

    public async Task UnassignOtherDeviceFromSeatAsync(string deviceId)
    {
        if (!_otherDeviceToSeatMap.TryGetValue(deviceId, out var seatId))
        {
            _logger.LogWarning($"Device {deviceId} not assigned to any seat");
            return;
        }

        var seat = await GetSeatAsync(seatId);
        if (seat != null)
        {
            seat.OtherDeviceIds.Remove(deviceId);
            await UpdateSeatAsync(seat);
        }

        _otherDeviceToSeatMap.Remove(deviceId);
        _logger.LogInformation($"Device {deviceId} unassigned from seat {seatId}");
    }

    public async Task<IReadOnlyList<ValidationError>> ValidateConfigurationAsync()
    {
        var errors = new List<ValidationError>();
        var seats = await GetAllSeatsAsync();
        var deviceRegistry = await _devicePersistence.GetAllDevicesAsync();

        // Check for duplicate device assignments
        var allAssignedDevices = new HashSet<string>();

        foreach (var seat in seats)
        {
            if (string.IsNullOrWhiteSpace(seat.WindowsUser))
            {
                errors.Add(new ValidationError
                {
                    Code = "NO_USER",
                    Message = $"Seat '{seat.Id}' has no Windows user assigned",
                    AffectedSeat = seat.Id,
                    Severity = ValidationSeverity.Error
                });
            }

            // Check keyboards
            foreach (var keyboardId in seat.KeyboardIds)
            {
                if (allAssignedDevices.Contains(keyboardId))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DUPLICATE_ASSIGNMENT",
                        Message = $"Keyboard '{keyboardId}' assigned to multiple seats",
                        AffectedDevice = keyboardId,
                        Severity = ValidationSeverity.Critical
                    });
                }
                else
                {
                    allAssignedDevices.Add(keyboardId);
                }

                // Verify device exists in registry
                if (deviceRegistry.FirstOrDefault(d => d.StableId == keyboardId) == null)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DEVICE_NOT_FOUND",
                        Message = $"Keyboard '{keyboardId}' not found in device registry",
                        AffectedSeat = seat.Id,
                        AffectedDevice = keyboardId,
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            // Check mice
            foreach (var mouseId in seat.MouseIds)
            {
                if (allAssignedDevices.Contains(mouseId))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DUPLICATE_ASSIGNMENT",
                        Message = $"Mouse '{mouseId}' assigned to multiple seats",
                        AffectedDevice = mouseId,
                        Severity = ValidationSeverity.Critical
                    });
                }
                else
                {
                    allAssignedDevices.Add(mouseId);
                }

                // Verify device exists in registry
                if (deviceRegistry.FirstOrDefault(d => d.StableId == mouseId) == null)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DEVICE_NOT_FOUND",
                        Message = $"Mouse '{mouseId}' not found in device registry",
                        AffectedSeat = seat.Id,
                        AffectedDevice = mouseId,
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            // Warn if no devices assigned
            if (seat.KeyboardIds.Count == 0 && seat.MouseIds.Count == 0)
            {
                errors.Add(new ValidationError
                {
                    Code = "NO_DEVICES",
                    Message = $"Seat '{seat.Id}' has no input devices assigned",
                    AffectedSeat = seat.Id,
                    Severity = ValidationSeverity.Warning
                });
            }
        }

        // Check for critical issues
        var criticalErrors = errors.Where(e => e.Severity == ValidationSeverity.Critical).ToList();
        if (criticalErrors.Count > 0)
        {
            _logger.LogError($"Configuration validation failed: {criticalErrors.Count} critical error(s)");
        }
        else
        {
            _logger.LogInformation($"Configuration validation complete: {errors.Count} issue(s) found");
        }

        return errors;
    }

    public async Task<SeatConfiguration> ExportConfigurationAsync()
    {
        var seats = await GetAllSeatsAsync();
        return new SeatConfiguration
        {
            Version = 1,
            Seats = seats.ToList()
        };
    }

    public async Task ImportConfigurationAsync(SeatConfiguration config)
    {
        if (config.Version != 1)
            throw new InvalidOperationException($"Unsupported configuration version: {config.Version}");

        _seats.Clear();
        _deviceToSeatMap.Clear();
        _displayToSeatMap.Clear();
        _otherDeviceToSeatMap.Clear();

        foreach (var seat in config.Seats)
        {
            _seats[seat.Id] = seat;
            await _persistence.SaveSeatAsync(seat);

            // Rebuild device-to-seat, display-to-seat, and other-device-to-seat maps
            foreach (var deviceId in seat.KeyboardIds.Concat(seat.MouseIds))
            {
                _deviceToSeatMap[deviceId] = seat.Id;
            }

            foreach (var displayId in seat.DisplayIds)
            {
                _displayToSeatMap[displayId] = seat.Id;
            }

            foreach (var otherId in seat.OtherDeviceIds)
            {
                _otherDeviceToSeatMap[otherId] = seat.Id;
            }
        }

        _logger.LogInformation($"Configuration imported: {config.Seats.Count} seat(s)");
    }
}
