namespace OpenMultiSeat.Tests;

[TestClass]
public class SeatConfigurationTests
{
    [TestMethod]
    public void Seat_ShouldHaveId()
    {
        var seat = new Seat { Id = "seat-1", Name = "Workstation 1" };
        Assert.AreEqual("seat-1", seat.Id);
    }
    
    [TestMethod]
    public void Seat_ShouldStartEnabled()
    {
        var seat = new Seat { Id = "seat-1", Name = "Workstation 1" };
        Assert.IsTrue(seat.Enabled);
    }
}

using OpenMultiSeat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
