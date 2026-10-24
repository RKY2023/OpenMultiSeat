using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Tests;

[TestClass]
public class TileAssignmentDecisionTests
{
    [TestMethod]
    public void Decide_BothNull_ReturnsNoOp()
    {
        Assert.AreEqual(TileDropAction.NoOp, TileAssignmentDecision.Decide(null, null));
    }

    [TestMethod]
    public void Decide_SameSeatBothEnds_ReturnsNoOp()
    {
        Assert.AreEqual(TileDropAction.NoOp, TileAssignmentDecision.Decide("seat-1", "seat-1"));
    }

    [TestMethod]
    public void Decide_UnassignedDroppedOnASeat_ReturnsDirectAssign()
    {
        Assert.AreEqual(TileDropAction.DirectAssign, TileAssignmentDecision.Decide(null, "seat-1"));
    }

    [TestMethod]
    public void Decide_AssignedDroppedBackOnSystem_ReturnsUnassign()
    {
        Assert.AreEqual(TileDropAction.Unassign, TileAssignmentDecision.Decide("seat-1", null));
    }

    [TestMethod]
    public void Decide_AssignedDroppedOnDifferentSeat_ReturnsConfirmMove()
    {
        Assert.AreEqual(TileDropAction.ConfirmMove, TileAssignmentDecision.Decide("seat-1", "seat-2"));
    }

    [TestMethod]
    public void Decide_SeatIdsAreOrdinalCaseSensitive()
    {
        // "Seat-1" vs "seat-1" -- seat IDs are opaque identifiers, not display text, so a
        // case-only difference is a different seat, not the same one.
        Assert.AreEqual(TileDropAction.ConfirmMove, TileAssignmentDecision.Decide("seat-1", "Seat-1"));
    }
}
