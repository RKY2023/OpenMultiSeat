namespace OpenMultiSeat.Core;

public interface IDisplayEnumerator
{
    Task<IReadOnlyList<Display>> EnumerateDisplaysAsync();
}
