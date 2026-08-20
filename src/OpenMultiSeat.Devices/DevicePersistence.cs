using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Devices;

public class DevicePersistence : IDevicePersistence
{
    private const string DeviceRegistryFileName = "device-registry.json";
    private readonly string _registryPath;
    private readonly ILogger<DevicePersistence> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false
    };

    private Dictionary<string, DeviceRecord> _cache = [];
    private bool _isLoaded;

    public DevicePersistence(ILogger<DevicePersistence> logger)
    {
        _logger = logger;
        _registryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat",
            DeviceRegistryFileName);
    }

    public async Task<DeviceRecord?> GetDeviceByHardwareIdAsync(string hardwareId)
    {
        await EnsureLoadedAsync();
        return _cache.Values.FirstOrDefault(d => d.HardwareId == hardwareId);
    }

    public async Task<DeviceRecord?> GetDeviceByStableIdAsync(string stableId)
    {
        await EnsureLoadedAsync();
        return _cache.TryGetValue(stableId, out var device) ? device : null;
    }

    public async Task SaveDeviceAsync(DeviceRecord device)
    {
        await EnsureLoadedAsync();
        device.LastSeen = DateTime.UtcNow;
        device.ConnectionCount++;

        if (!_cache.ContainsKey(device.StableId))
        {
            device.FirstSeen = DateTime.UtcNow;
            _logger.LogInformation($"New device registered: {device.ProductName} ({device.StableId})");
        }

        _cache[device.StableId] = device;
        await PersistAsync();
    }

    public async Task<IReadOnlyList<DeviceRecord>> GetAllDevicesAsync()
    {
        await EnsureLoadedAsync();
        return _cache.Values.ToList();
    }

    public async Task DeleteDeviceAsync(string stableId)
    {
        await EnsureLoadedAsync();
        if (_cache.Remove(stableId))
        {
            _logger.LogInformation($"Device removed: {stableId}");
            await PersistAsync();
        }
    }

    public async Task<string> GenerateStableIdAsync(string hardwareId, string? serialNumber = null)
    {
        await EnsureLoadedAsync();

        var existing = _cache.Values.FirstOrDefault(d => d.HardwareId == hardwareId);
        if (existing != null)
        {
            return existing.StableId;
        }

        return GenerateNewStableId(hardwareId, serialNumber);
    }

    private string GenerateNewStableId(string hardwareId, string? serialNumber)
    {
        var baseId = ExtractBaseId(hardwareId);

        if (!string.IsNullOrEmpty(serialNumber))
        {
            return $"DEVICE_{baseId}_{serialNumber}".ToUpperInvariant();
        }

        var hash = hardwareId.GetHashCode().ToString("X8");
        return $"DEVICE_{baseId}_{hash}".ToUpperInvariant();
    }

    private string ExtractBaseId(string hardwareId)
    {
        var parts = hardwareId.Split('&');
        if (parts.Length >= 2)
        {
            return parts[1]; // VID_XXXX or PID_XXXX
        }
        return "UNKNOWN";
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
            return;

        try
        {
            if (File.Exists(_registryPath))
            {
                var json = await File.ReadAllTextAsync(_registryPath);
                var records = JsonSerializer.Deserialize<List<DeviceRecord>>(json, _jsonOptions) ?? [];
                _cache = records.ToDictionary(r => r.StableId);
                _logger.LogInformation($"Loaded {_cache.Count} device records from registry");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading device registry");
        }

        _isLoaded = true;
    }

    private async Task PersistAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_registryPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_cache.Values.ToList(), _jsonOptions);
            await File.WriteAllTextAsync(_registryPath, json);
            _logger.LogDebug($"Device registry persisted ({_cache.Count} devices)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting device registry");
        }
    }
}
