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

    public nint ComputeAffinityMask(IReadOnlyList<int> coreIndices) => ComputeAffinityMask(coreIndices, LogicalCoreCount);

    /// <summary>
    /// Same computation as <see cref="ComputeAffinityMask(IReadOnlyList{int})"/> but with the
    /// available core count passed in explicitly, rather than read from the current machine via
    /// <see cref="LogicalCoreCount"/>. Exists so the &gt;=64-core processor-group boundary can be
    /// exercised by tests on any machine, not just one that actually has 64+ logical cores.
    /// </summary>
    public static nint ComputeAffinityMask(IReadOnlyList<int> coreIndices, int coreCount)
    {
        ArgumentNullException.ThrowIfNull(coreIndices);

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

            // SetProcessAffinityMask (the single-group API OpenMultiSeat.Sessions calls) can
            // only address cores 0-63 of the calling process's own processor group, regardless
            // of how many logical cores the machine has in total. `1L << core` for core >= 64
            // wraps silently in C# (the shift count is masked to its low 6 bits) rather than
            // failing loudly, so reject it explicitly instead of computing a wrong mask.
            if (core >= 64)
            {
                throw new NotSupportedException(
                    $"CPU core index {core} is beyond core 63. Systems with more than 64 logical " +
                    "cores use Windows processor groups, which SetProcessAffinityMask cannot address " +
                    "— that needs the group-aware SetProcessAffinityMask/SetThreadGroupAffinity APIs, " +
                    "not implemented here.");
            }

            mask |= 1L << core;
        }

        return (nint)mask;
    }
}
