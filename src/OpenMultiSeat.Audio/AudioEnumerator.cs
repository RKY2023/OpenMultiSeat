using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Audio;

public interface IAudioDeviceEnumerator
{
    Task<IReadOnlyList<AudioDevice>> EnumerateAudioDevicesAsync();
    Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync();
    Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync();
}

/// <summary>
/// Enumerates real Windows Core Audio endpoints (speakers, headsets, Bluetooth audio,
/// microphones) via NAudio's MMDeviceEnumerator, rather than hand-rolled COM interop against
/// IMMDeviceEnumerator directly. This session already hit two real, crash-causing P/Invoke
/// struct-layout bugs writing raw Win32 interop by hand (see DisplayEnumerator's history) — COM
/// interop has its own, different set of sharp edges (vtable/interface declarations, reference
/// counting), and NAudio is a widely-used, well-tested wrapper around exactly this API, so this
/// uses it for the enumeration itself rather than repeat that risk a third time.
/// </summary>
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

    public Task<IReadOnlyList<AudioDevice>> EnumeratePlaybackDevicesAsync()
        => Task.Run(() => Enumerate(DataFlow.Render, AudioDeviceType.Playback));

    public Task<IReadOnlyList<AudioDevice>> EnumerateRecordingDevicesAsync()
        => Task.Run(() => Enumerate(DataFlow.Capture, AudioDeviceType.Recording));

    private IReadOnlyList<AudioDevice> Enumerate(DataFlow flow, AudioDeviceType type)
    {
        var devices = new List<AudioDevice>();

        try
        {
            using var enumerator = new MMDeviceEnumerator();

            string? defaultId = null;
            try
            {
                using var defaultDevice = enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
                defaultId = defaultDevice.ID;
            }
            catch (Exception)
            {
                // No default endpoint configured for this flow (e.g. no microphone at all) —
                // not fatal, IsDefault just stays false for every device below.
            }

            foreach (var mmDevice in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            {
                using (mmDevice)
                {
                    devices.Add(new AudioDevice
                    {
                        DeviceId = mmDevice.ID,
                        EndpointId = mmDevice.ID,
                        FriendlyName = mmDevice.FriendlyName,
                        Type = type,
                        IsDefault = mmDevice.ID == defaultId,
                        IsConnected = mmDevice.State == DeviceState.Active
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enumerating {flow} audio devices");
        }

        return devices;
    }
}
