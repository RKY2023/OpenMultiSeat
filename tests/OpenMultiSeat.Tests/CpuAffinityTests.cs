using OpenMultiSeat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenMultiSeat.Tests;

[TestClass]
public class CpuAffinityTests
{
    [TestMethod]
    public void Seat_CpuCoreAffinity_DefaultsToEmpty()
    {
        var seat = new Seat { Id = "seat-1", Name = "Workstation 1" };
        Assert.AreEqual(0, seat.CpuCoreAffinity.Count);
    }

    [TestMethod]
    public void ComputeAffinityMask_SpecificCores_SetsOnlyThoseBits()
    {
        var provider = new CpuAffinityProvider();
        // Cores 0 and 2 -> bits 0 and 2 set -> 0b101 = 5, regardless of machine core count
        // (as long as the test machine has at least 3 logical cores, which every CI/dev box does).
        var mask = provider.ComputeAffinityMask([0, 2]);
        Assert.AreEqual((nint)0b101, mask);
    }

    [TestMethod]
    public void ComputeAffinityMask_SingleCore_SetsOnlyThatBit()
    {
        var provider = new CpuAffinityProvider();
        var mask = provider.ComputeAffinityMask([1]);
        Assert.AreEqual((nint)0b10, mask);
    }

    [TestMethod]
    public void ComputeAffinityMask_EmptyList_CoversEveryAvailableCore()
    {
        var provider = new CpuAffinityProvider();
        var mask = provider.ComputeAffinityMask([]);
        var expected = (nint)((1L << provider.LogicalCoreCount) - 1);
        Assert.AreEqual(expected, mask);
    }

    [TestMethod]
    public void ComputeAffinityMask_DuplicateCores_DoNotDoubleCount()
    {
        var provider = new CpuAffinityProvider();
        var mask = provider.ComputeAffinityMask([0, 0, 0]);
        Assert.AreEqual((nint)0b1, mask);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ComputeAffinityMask_CoreIndexBeyondAvailableCount_Throws()
    {
        var provider = new CpuAffinityProvider();
        provider.ComputeAffinityMask([provider.LogicalCoreCount]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void ComputeAffinityMask_NegativeCoreIndex_Throws()
    {
        var provider = new CpuAffinityProvider();
        provider.ComputeAffinityMask([-1]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ComputeAffinityMask_NullList_Throws()
    {
        var provider = new CpuAffinityProvider();
        provider.ComputeAffinityMask(null!);
    }

    [TestMethod]
    public void ComputeAffinityMask_CoresBelow64_ExplicitCoreCount_SetsCorrectBits()
    {
        // Uses the (coreIndices, coreCount) overload so this doesn't depend on the test
        // machine's real core count — exercises the same path a >64-core machine would take
        // for its first 64 cores.
        var mask = CpuAffinityProvider.ComputeAffinityMask([0, 63], coreCount: 128);
        Assert.AreEqual(unchecked((nint)((1L << 63) | 1L)), mask);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void ComputeAffinityMask_CoreAt64OrBeyond_Throws()
    {
        // A machine with >64 logical cores is exactly when core index 64 becomes a valid,
        // in-range index (passes the ArgumentOutOfRangeException check) but SetProcessAffinityMask
        // still can't address it without processor-group support — must fail loudly, not silently
        // wrap the bit position (1L << 64 == 1L << 0 in C#'s shift semantics).
        CpuAffinityProvider.ComputeAffinityMask([64], coreCount: 128);
    }
}
