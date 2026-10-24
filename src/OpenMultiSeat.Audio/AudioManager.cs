using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Audio;

public interface IAudioManager
{
    Task<IReadOnlyList<AudioDevice>> GetAudioDevicesAsync();
    Task<AudioDevice?> GetAudioDeviceAsync(string deviceId);
    Task<IReadOnlyList<AudioDevice>> GetDevicesForSeatAsync(string seatId);
    Task AssignAudioDeviceToSeatAsync(string seatId, string deviceId, AudioDeviceRole role);
    Task UnassignAudioDeviceFromSeatAsync(string deviceId);
    Task<AudioTopology> GetAudioTopologyAsync();
    Task<IReadOnlyList<ValidationError>> ValidateAudioConfigAsync();
    Task<bool> IsAudioRoutingEnabledAsync();
    Task<bool> EnableAudioRoutingAsync();
    Task<bool> DisableAudioRoutingAsync();

    /// <summary>
    /// Real-time peak level (0.0-1.0) for the audio endpoint currently identified by
    /// <paramref name="endpointId"/> (AudioDevice.EndpointId/DeviceId -- an NAudio/Core Audio
    /// endpoint ID), via Windows' own per-endpoint audio meter -- the same mechanism the Windows
    /// volume mixer's own level bars use. Used by the Tile Layout window's "Indicate device" for
    /// audio tiles: unlike a keyboard/mouse (identifiable by pressing it) or a display (identifiable
    /// by an on-screen overlay), there's no way to make an arbitrary speaker/microphone "announce
    /// itself" -- the closest real equivalent is showing which endpoint is *actually carrying
    /// sound right now*, so an admin can play a test tone and watch for the live tile that lights
    /// up. Returns 0 (not an exception) for a disconnected/removed endpoint or any other failure,
    /// since a polling caller shouldn't have to handle a fluctuating hardware condition as an error.
    /// </summary>
    Task<float> GetPeakLevelAsync(string endpointId);
}

public class AudioTopology
{
    public int TotalDevices { get; set; }
    public int ConnectedDevices { get; set; }
    public List<AudioDevice> DeviceList { get; set; } = [];
    public Dictionary<string, AudioDeviceAssignment> SeatAudioMapping { get; set; } = [];
}

public class AudioDeviceAssignment
{
    public List<string> PlaybackDevices { get; set; } = [];
    public List<string> RecordingDevices { get; set; } = [];
}

public class AudioManager : IAudioManager
{
    private readonly ILogger<AudioManager> _logger;
    private readonly IAudioDeviceEnumerator _enumerator;
    private readonly ISeatManager _seatManager;
    private readonly IAudioPersistence _persistence;
    private Dictionary<string, AudioDevice> _deviceCache = [];
    private Dictionary<string, AudioDeviceAssignment> _seatAudioMap = [];
    private bool _routingEnabled = false;

    public AudioManager(
        ILogger<AudioManager> logger,
        IAudioDeviceEnumerator enumerator,
        ISeatManager seatManager,
        IAudioPersistence persistence)
    {
        _logger = logger;
        _enumerator = enumerator;
        _seatManager = seatManager;
        _persistence = persistence;
    }

    public async Task<IReadOnlyList<AudioDevice>> GetAudioDevicesAsync()
    {
        await RefreshCacheAsync();
        return _deviceCache.Values.ToList();
    }

    public async Task<AudioDevice?> GetAudioDeviceAsync(string deviceId)
    {
        await RefreshCacheAsync();
        return _deviceCache.TryGetValue(deviceId, out var device) ? device : null;
    }

    public async Task<IReadOnlyList<AudioDevice>> GetDevicesForSeatAsync(string seatId)
    {
        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        await RefreshCacheAsync();
        var devices = new List<AudioDevice>();
        if (_seatAudioMap.TryGetValue(seatId, out var assignment))
        {
            foreach (var deviceId in assignment.PlaybackDevices.Concat(assignment.RecordingDevices))
            {
                if (_deviceCache.TryGetValue(deviceId, out var device))
                    devices.Add(device);
            }
        }
        return devices;
    }

    public async Task AssignAudioDeviceToSeatAsync(string seatId, string deviceId, AudioDeviceRole role)
    {
        var seat = await _seatManager.GetSeatAsync(seatId);
        if (seat == null)
            throw new InvalidOperationException($"Seat '{seatId}' not found");

        await RefreshCacheAsync();
        var device = await GetAudioDeviceAsync(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Audio device '{deviceId}' not found");

        foreach (var kvp in _seatAudioMap.Where(x => x.Key != seatId))
        {
            if (kvp.Value.PlaybackDevices.Contains(deviceId) || kvp.Value.RecordingDevices.Contains(deviceId))
                throw new InvalidOperationException($"Audio device '{deviceId}' already assigned to seat '{kvp.Key}'");
        }

        if (!_seatAudioMap.TryGetValue(seatId, out var assignment))
        {
            assignment = new AudioDeviceAssignment();
            _seatAudioMap[seatId] = assignment;
        }

        switch (role)
        {
            case AudioDeviceRole.Playback:
                if (!assignment.PlaybackDevices.Contains(deviceId))
                    assignment.PlaybackDevices.Add(deviceId);
                break;
            case AudioDeviceRole.Recording:
                if (!assignment.RecordingDevices.Contains(deviceId))
                    assignment.RecordingDevices.Add(deviceId);
                break;
            case AudioDeviceRole.BiDirectional:
                if (!assignment.PlaybackDevices.Contains(deviceId))
                    assignment.PlaybackDevices.Add(deviceId);
                if (!assignment.RecordingDevices.Contains(deviceId))
                    assignment.RecordingDevices.Add(deviceId);
                break;
        }

        await _persistence.SaveAudioAssignmentAsync(seatId, assignment);
        _logger.LogInformation($"Audio device {deviceId} assigned to seat {seatId} ({role})");
    }

    public async Task UnassignAudioDeviceFromSeatAsync(string deviceId)
    {
        await RefreshCacheAsync();
        string? targetSeat = null;

        foreach (var kvp in _seatAudioMap)
        {
            if (kvp.Value.PlaybackDevices.Remove(deviceId) || kvp.Value.RecordingDevices.Remove(deviceId))
            {
                targetSeat = kvp.Key;
                break;
            }
        }

        if (targetSeat != null)
        {
            await _persistence.SaveAudioAssignmentAsync(targetSeat, _seatAudioMap[targetSeat]);
            _logger.LogInformation($"Audio device {deviceId} unassigned from seat {targetSeat}");
        }
        else
        {
            _logger.LogWarning($"Audio device {deviceId} not assigned to any seat");
        }
    }

    public async Task<AudioTopology> GetAudioTopologyAsync()
    {
        await RefreshCacheAsync();
        return new AudioTopology
        {
            TotalDevices = _deviceCache.Count,
            ConnectedDevices = _deviceCache.Values.Count(d => d.IsConnected),
            DeviceList = _deviceCache.Values.ToList(),
            SeatAudioMapping = _seatAudioMap
        };
    }

    public async Task<IReadOnlyList<ValidationError>> ValidateAudioConfigAsync()
    {
        var errors = new List<ValidationError>();
        await RefreshCacheAsync();

        var seats = await _seatManager.GetAllSeatsAsync();
        var assignedDevices = new HashSet<string>();

        foreach (var kvp in _seatAudioMap)
        {
            var seat = seats.FirstOrDefault(s => s.Id == kvp.Key);
            if (seat == null)
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_SEAT",
                    Message = $"Audio device(s) assigned to non-existent seat '{kvp.Key}'",
                    AffectedSeat = kvp.Key,
                    Severity = ValidationSeverity.Warning
                });
            }

            foreach (var deviceId in kvp.Value.PlaybackDevices.Concat(kvp.Value.RecordingDevices))
            {
                if (assignedDevices.Contains(deviceId))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DUPLICATE_AUDIO_ASSIGNMENT",
                        Message = $"Audio device '{deviceId}' assigned to multiple seats",
                        AffectedDevice = deviceId,
                        Severity = ValidationSeverity.Critical
                    });
                }
                else
                {
                    assignedDevices.Add(deviceId);
                }

                if (!_deviceCache.ContainsKey(deviceId))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "DEVICE_NOT_FOUND",
                        Message = $"Audio device '{deviceId}' not found in system",
                        AffectedSeat = kvp.Key,
                        AffectedDevice = deviceId,
                        Severity = ValidationSeverity.Warning
                    });
                }
            }
        }

        foreach (var seat in seats)
        {
            var hasMapping = _seatAudioMap.TryGetValue(seat.Id, out var map);
            if (!hasMapping || (map.PlaybackDevices.Count == 0 && map.RecordingDevices.Count == 0))
            {
                errors.Add(new ValidationError
                {
                    Code = "NO_AUDIO_DEVICES",
                    Message = $"Seat '{seat.Id}' has no audio devices assigned",
                    AffectedSeat = seat.Id,
                    Severity = ValidationSeverity.Info
                });
            }
        }

        return errors;
    }

    public async Task<bool> IsAudioRoutingEnabledAsync()
    {
        _routingEnabled = await _persistence.GetRoutingStatusAsync();
        return _routingEnabled;
    }

    public async Task<bool> EnableAudioRoutingAsync()
    {
        if (_routingEnabled)
        {
            _logger.LogInformation("Audio routing already enabled");
            return true;
        }

        var errors = await ValidateAudioConfigAsync();
        if (errors.Any(e => e.Severity == ValidationSeverity.Critical))
        {
            _logger.LogError("Cannot enable audio routing: critical error(s) present");
            return false;
        }

        await _persistence.SetRoutingStatusAsync(true);
        _routingEnabled = true;
        _logger.LogInformation("✓ Audio routing enabled");
        return true;
    }

    public async Task<bool> DisableAudioRoutingAsync()
    {
        if (!_routingEnabled)
        {
            _logger.LogInformation("Audio routing already disabled");
            return true;
        }

        await _persistence.SetRoutingStatusAsync(false);
        _routingEnabled = false;
        _logger.LogInformation("✓ Audio routing disabled");
        return true;
    }

    public Task<float> GetPeakLevelAsync(string endpointId)
    {
        return Task.Run(() =>
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDevice(endpointId);
                return device.AudioMeterInformation.MasterPeakValue;
            }
            catch (Exception ex)
            {
                // Most commonly: the endpoint was unplugged/disabled since it was last enumerated.
                // Not worth logging at Error/Warning -- a polling caller hits this constantly for
                // any endpoint that's momentarily gone, and 0 is exactly the right answer either way.
                _logger.LogDebug(ex, $"Couldn't read peak level for audio endpoint {endpointId}");
                return 0f;
            }
        });
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            var devices = await _enumerator.EnumerateAudioDevicesAsync();
            _deviceCache = devices.ToDictionary(d => d.DeviceId);
            var assignments = await _persistence.GetAllAudioAssignmentsAsync();
            _seatAudioMap = assignments;
            _routingEnabled = await _persistence.GetRoutingStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing audio cache");
        }
    }
}
