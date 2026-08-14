using OpenMultiSeat.Core;

namespace OpenMultiSeat.Displays;

public interface IDisplayEnumerator
{
    Task<IReadOnlyList<Display>> EnumerateDisplaysAsync();
}

public class DisplayEnumerator : IDisplayEnumerator
{
    public async Task<IReadOnlyList<Display>> EnumerateDisplaysAsync()
    {
        return await Task.FromResult<IReadOnlyList<Display>>([]);
    }
}
