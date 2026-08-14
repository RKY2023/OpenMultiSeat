using OpenMultiSeat.Core;

namespace OpenMultiSeat.Audio;

public interface IAudioEnumerator
{
    Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync();
    Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync();
}

public class AudioEnumerator : IAudioEnumerator
{
    public async Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync()
    {
        return await Task.FromResult<IReadOnlyList<AudioDevice>>([]);
    }
    
    public async Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync()
    {
        return await Task.FromResult<IReadOnlyList<AudioDevice>>([]);
    }
}
