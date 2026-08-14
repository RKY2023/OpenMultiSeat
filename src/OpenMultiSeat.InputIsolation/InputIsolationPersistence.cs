using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.InputIsolation;

public interface IInputIsolationPersistence
{
    Task<IReadOnlyList<InputDeviceBinding>> GetAllBindingsAsync();
    Task<InputDeviceBinding?> GetBindingByDeviceAsync(string deviceId);
    Task SaveBindingAsync(InputDeviceBinding binding);
    Task DeleteBindingAsync(string deviceId);
    Task<bool> GetIsolationStatusAsync();
    Task SetIsolationStatusAsync(bool enabled);
}

public class InputIsolationPersistence : IInputIsolationPersistence
{
    private readonly string _configDir;
    private readonly string _bindingsFile;
    private readonly string _statusFile;
    private readonly ILogger<InputIsolationPersistence> _logger;

    private Dictionary<string, InputDeviceBinding> _bindingCache = [];

    public InputIsolationPersistence(ILogger<InputIsolationPersistence> logger)
    {
        _logger = logger;
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat");
        _bindingsFile = Path.Combine(_configDir, "input-bindings.json");
        _statusFile = Path.Combine(_configDir, "isolation-status.json");

        Directory.CreateDirectory(_configDir);
    }

    public async Task<IReadOnlyList<InputDeviceBinding>> GetAllBindingsAsync()
    {
        if (_bindingCache.Count > 0)
            return _bindingCache.Values.ToList();

        await LoadBindingsAsync();
        return _bindingCache.Values.ToList();
    }

    public async Task<InputDeviceBinding?> GetBindingByDeviceAsync(string deviceId)
    {
        await LoadBindingsAsync();
        return _bindingCache.TryGetValue(deviceId, out var binding) ? binding : null;
    }

    public async Task SaveBindingAsync(InputDeviceBinding binding)
    {
        await LoadBindingsAsync();
        _bindingCache[binding.DeviceId] = binding;
        await PersistBindingsAsync();
    }

    public async Task DeleteBindingAsync(string deviceId)
    {
        await LoadBindingsAsync();
        _bindingCache.Remove(deviceId);
        await PersistBindingsAsync();
    }

    public async Task<bool> GetIsolationStatusAsync()
    {
        try
        {
            if (!File.Exists(_statusFile))
                return false;

            var json = await File.ReadAllTextAsync(_statusFile);
            var data = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            return data?.TryGetValue("enabled", out var enabled) == true && enabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading isolation status");
            return false;
        }
    }

    public async Task SetIsolationStatusAsync(bool enabled)
    {
        try
        {
            var data = new Dictionary<string, bool> { { "enabled", enabled } };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_statusFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing isolation status");
        }
    }

    private async Task LoadBindingsAsync()
    {
        if (_bindingCache.Count > 0)
            return;

        try
        {
            if (!File.Exists(_bindingsFile))
                return;

            var json = await File.ReadAllTextAsync(_bindingsFile);
            var bindings = JsonSerializer.Deserialize<List<InputDeviceBinding>>(json) ?? [];

            _bindingCache = bindings.ToDictionary(b => b.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading input bindings");
            _bindingCache = [];
        }
    }

    private async Task PersistBindingsAsync()
    {
        try
        {
            var bindings = _bindingCache.Values.ToList();
            var json = JsonSerializer.Serialize(bindings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_bindingsFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting input bindings");
        }
    }
}
