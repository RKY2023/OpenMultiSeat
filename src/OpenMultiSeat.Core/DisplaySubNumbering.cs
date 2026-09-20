namespace OpenMultiSeat.Core;

/// <summary>
/// Computes ASTER-style "workplace.display-index" labels (e.g. "1.1", "1.4") for a display
/// assigned to a seat, used by the Tile Layout window's on-monitor "Indicate device" overlay
/// (see docs/control-panel/workplaces-tab.md).
///
/// There's no persisted "workplace number" field on <see cref="Seat"/> -- seats have an arbitrary
/// admin-given Id/Name, not a numbered slot the way ASTER's own workplaces are numbered. The
/// number used here is the seat's 1-based position within the caller-supplied seat list, which
/// the Tile Layout window already orders left-to-right by column -- so the computed label matches
/// what's on screen, but isn't a stable identifier that survives seats being reordered/recreated.
/// The display index is the display's 1-based position within that seat's own DisplayIds list.
/// </summary>
public static class DisplaySubNumbering
{
    /// <returns>A label like "1.1" or "2.4", or null if <paramref name="displayId"/> isn't
    /// assigned to any seat in <paramref name="seatsInColumnOrder"/>.</returns>
    public static string? Compute(IReadOnlyList<Seat> seatsInColumnOrder, string displayId)
    {
        for (var i = 0; i < seatsInColumnOrder.Count; i++)
        {
            var index = seatsInColumnOrder[i].DisplayIds.IndexOf(displayId);
            if (index >= 0)
                return $"{i + 1}.{index + 1}";
        }

        return null;
    }
}
