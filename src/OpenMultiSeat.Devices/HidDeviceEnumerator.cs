namespace OpenMultiSeat.Devices;

public interface IHidDeviceEnumerator
{
    Task<IReadOnlyList<InputDevice>> EnumerateKeyboardsAsync();
    Task<IReadOnlyList<InputDevice>> EnumerateMiceAsync();
    Task<IReadOnlyList<InputDevice>> EnumerateAllDevicesAsync();
}

public class HidDeviceEnumerator : IHidDeviceEnumerator
{
    public async Task<IReadOnlyList<InputDevice>> EnumerateKeyboardsAsync()
    {
        return await EnumerateDevicesOfTypeAsync(InputDeviceType.Keyboard);
    }
    
    public async Task<IReadOnlyList<InputDevice>> EnumerateMiceAsync()
    {
        return await EnumerateDevicesOfTypeAsync(InputDeviceType.Mouse);
    }
    
    public async Task<IReadOnlyList<InputDevice>> EnumerateAllDevicesAsync()
    {
        return await Task.FromResult<IReadOnlyList<InputDevice>>([]);
    }
    
    private async Task<IReadOnlyList<InputDevice>> EnumerateDevicesOfTypeAsync(InputDeviceType type)
    {
        return await Task.FromResult<IReadOnlyList<InputDevice>>([]);
    }
}

using OpenMultiSeat.Core;
