using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenMultiSeat.Audio;

public interface IAudioPersistence
{
    Task<Dictionary<string, AudioDeviceAssignment>> GetAllAudioAssignmentsAsync();
    Task SaveAudioAssignmentAsync(string seatId, AudioDeviceAssignment assignment);
    Task<bool> GetRoutingStatusAsync();
    Task SetRoutingStatusAsync(bool enabled);
}

public class AudioPersistence : IAudioPersistence
{
    private readonly string _configDir;
    private readonly string _assignmentsFile;
    private readonly string _statusFile;
    private readonly ILogger<AudioPersistence> _logger;
    private Dictionary<string, AudioDeviceAssignment> _assignmentCache = [];

    public AudioPersistence(ILogger<AudioPersistence> logger)
    {
        _logger = logger;
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMultiSeat");
        _assignmentsFile = Path.Combine(_configDir, "audio-assignments.json");
        _statusFile = Path.Combine(_configDir, "audio-routing-status.json");
        Directory.CreateDirectory(_configDir);
    }

    public async Task<Dictionary<string, AudioDeviceAssignment>> GetAllAudioAssignmentsAsync()
    {
        if (_assignmentCache.Count > 0)
            return _assignmentCache;

        await LoadAssignmentsAsync();
        return _assignmentCache;
    }

    public async Task SaveAudioAssignmentAsync(string seatId, AudioDeviceAssignment assignment)
    {
        await LoadAssignmentsAsync();
        _assignmentCache[seatId] = assignment;
        await PersistAssignmentsAsync();
    }

    public async Task<bool> GetRoutingStatusAsync()
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
            _logger.LogError(ex, "Error reading routing status");
            return false;
        }
    }

    public async Task SetRoutingStatusAsync(bool enabled)
    {
        try
        {
            var data = new Dictionary<string, bool> { { "enabled", enabled } };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_statusFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing routing status");
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        if (_assignmentCache.Count > 0)
            return;

        try
        {
            if (!File.Exists(_assignmentsFile))
                return;

            var json = await File.ReadAllTextAsync(_assignmentsFile);
            var data = JsonSerializer.Deserialize<Dictionary<string, AudioDeviceAssignment>>(json) ?? [];
            _assignmentCache = data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audio assignments");
            _assignmentCache = [];
        }
    }

    private async Task PersistAssignmentsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_assignmentCache, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_assignmentsFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting audio assignments");
        }
    }
}
