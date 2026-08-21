namespace OpenMultiSeat.Core;

/// <summary>
/// What a drag-and-drop of one resource (device/display/audio endpoint) tile onto a column in the
/// tile layout view should actually do, given where it currently lives and where it was dropped.
/// Extracted as a pure function so the branching that four separate Assign*ToSeatWindow classes
/// already duplicate inline (see AssignDeviceToSeatWindow's OnAssign, for example) has one
/// unit-tested home for the tile view specifically, rather than a fifth copy embedded in
/// WorkplaceTileLayoutWindow's drop handler.
/// </summary>
public enum TileDropAction
{
    /// <summary>Dropped back onto the column it already belongs to (including System-to-System
    /// for an already-unassigned resource) — nothing to do.</summary>
    NoOp,

    /// <summary>Was unassigned (in "System"), now dropped onto a seat — assign directly, no
    /// confirmation needed since nothing is being taken away from another seat.</summary>
    DirectAssign,

    /// <summary>Was assigned to one seat, dropped onto a *different* seat — show a "Confirm
    /// Device Destination" prompt before actually moving it, matching every other reassignment
    /// entry point in this GUI.</summary>
    ConfirmMove,

    /// <summary>Was assigned to a seat, dropped back onto "System" — unassign it.</summary>
    Unassign
}

public static class TileAssignmentDecision
{
    /// <param name="currentSeatId">The seat the resource is presently assigned to, or null if it's
    /// currently unassigned ("System").</param>
    /// <param name="targetSeatId">The column the resource was dropped onto, or null for "System".</param>
    public static TileDropAction Decide(string? currentSeatId, string? targetSeatId)
    {
        if (string.Equals(currentSeatId, targetSeatId, StringComparison.Ordinal))
            return TileDropAction.NoOp;

        if (currentSeatId == null)
            return TileDropAction.DirectAssign;

        if (targetSeatId == null)
            return TileDropAction.Unassign;

        return TileDropAction.ConfirmMove;
    }
}
