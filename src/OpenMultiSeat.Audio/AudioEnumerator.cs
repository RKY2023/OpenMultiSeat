using Microsoft.Extensions.Logging;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Audio;

public interface IAudioDeviceEnumerator
{
    Task<IReadOnlyList<AudioDevice>> EnumerateAudioDevicesAsync();
    Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync();
    Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync();
}

public class AudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private readonly ILogger<AudioDeviceEnumerator> _logger;

    public AudioDeviceEnumerator(ILogger<AudioDeviceEnumerator> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<AudioDevice>> EnumerateAudioDevicesAsync()
    {
        var combined = new List<AudioDevice>();
        var playback = await EnumeratePlaybackDevicesAsync();
        var recording = await EnumerateRecordingDevicesAsync();

        combined.AddRange(playback);
        combined.AddRange(recording.Where(r => !playback.Any(p => p.DeviceId == r.DeviceId)));

        return combined;
    }

    public async Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync()
    {
        try
        {
            return await Task.FromResult<IReadOnlyList<AudioDevice>>([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating playback devices");
            return [];
        }
    }

    public async Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync()
    {
        try
        {
            return await Task.FromResult<IReadOnlyList<AudioDevice>>([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating recording devices");
            return [];
        }
    }
}
