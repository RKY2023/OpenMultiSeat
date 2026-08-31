using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Tests;

[TestClass]
public class DisplaySubNumberingTests
{
    [TestMethod]
    public void Compute_FirstSeatFirstDisplay_ReturnsOneDotOne()
    {
        var seats = new List<Seat> { new() { Id = "s1", Name = "Seat 1", DisplayIds = ["d-1", "d-2"] } };
        Assert.AreEqual("1.1", DisplaySubNumbering.Compute(seats, "d-1"));
    }

    [TestMethod]
    public void Compute_FirstSeatSecondDisplay_ReturnsOneDotTwo()
    {
        var seats = new List<Seat> { new() { Id = "s1", Name = "Seat 1", DisplayIds = ["d-1", "d-2"] } };
        Assert.AreEqual("1.2", DisplaySubNumbering.Compute(seats, "d-2"));
    }

    [TestMethod]
    public void Compute_SecondSeatFirstDisplay_ReturnsTwoDotOne()
    {
        var seats = new List<Seat>
        {
            new() { Id = "s1", Name = "Seat 1", DisplayIds = ["d-1"] },
            new() { Id = "s2", Name = "Seat 2", DisplayIds = ["d-2"] }
        };
        Assert.AreEqual("2.1", DisplaySubNumbering.Compute(seats, "d-2"));
    }

    [TestMethod]
    public void Compute_DisplayNotAssignedToAnyListedSeat_ReturnsNull()
    {
        var seats = new List<Seat> { new() { Id = "s1", Name = "Seat 1", DisplayIds = ["d-1"] } };
        Assert.IsNull(DisplaySubNumbering.Compute(seats, "d-unassigned"));
    }

    [TestMethod]
    public void Compute_EmptySeatList_ReturnsNull()
    {
        Assert.IsNull(DisplaySubNumbering.Compute([], "d-1"));
    }
}
