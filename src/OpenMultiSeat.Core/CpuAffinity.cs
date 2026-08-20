namespace OpenMultiSeat.Core;

/// <summary>
/// Computes Windows process-affinity bitmasks from a seat's assigned logical CPU cores.
/// Pure bitmask math — no OS calls — so it's usable from both the Sessions project
/// (which owns the actual process handle and calls SetProcessAffinityMask) and tests.
/// </summary>
public interface ICpuAffinityProvider
{
    /// <summary>Number of logical CPU cores visible to this process.</summary>
    int LogicalCoreCount { get; }

    /// <summary>
    /// Builds a Windows affinity mask for the given core indices.
    /// An empty list means "no restriction" and returns a mask covering every available core.
    /// </summary>
    nint ComputeAffinityMask(IReadOnlyList<int> coreIndices);
}

public sealed class CpuAffinityProvider : ICpuAffinityProvider
{
    public int LogicalCoreCount => Environment.ProcessorCount;

    public nint ComputeAffinityMask(IReadOnlyList<int> coreIndices)
    {
        ArgumentNullException.ThrowIfNull(coreIndices);

        var coreCount = LogicalCoreCount;

        if (coreIndices.Count == 0)
        {
            // No restriction: every available core, up to the 64 bits a single
            // Windows processor group's affinity mask can address.
            return coreCount >= 64
                ? unchecked((nint)(-1L))
                : (nint)((1L << coreCount) - 1);
        }

        long mask = 0;
        foreach (var core in coreIndices)
        {
            if (core < 0 || core >= coreCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coreIndices),
                    core,
                    $"CPU core index {core} is outside the available range (0-{coreCount - 1}).");
            }

            mask |= 1L << core;
        }

        return (nint)mask;
    }
}
