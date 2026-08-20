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
}
