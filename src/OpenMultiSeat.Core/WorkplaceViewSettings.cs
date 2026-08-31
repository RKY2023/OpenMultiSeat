namespace OpenMultiSeat.Core;

/// <summary>
/// View-presentation preferences for the Workplaces/Seats area — the subset of ASTER's
/// "Workplace Tab Settings" window that has a real equivalent in OpenMultiSeat's current
/// DataGrid-based (not tile-based) Seats/Devices/Displays pages. Icon size and "distribute tiles
/// evenly" are deliberately not modeled here: there's no tile/icon layout to size or distribute
/// yet, so a setting for either would control nothing. See
/// docs/control-panel/workplace-tab-settings.md for the full comparison.
/// </summary>
public sealed class WorkplaceViewSettings
{
    /// <summary>When false, the Displays page hides displays not currently assigned to any seat.
    /// Maps to ASTER's "show displays not linked to any adapter" toggle (inverted: ASTER's
    /// default shows them, so does this one, at true).</summary>
    public bool ShowUnassignedDisplays { get; set; } = true;

    /// <summary>How many seconds after a device's first registration
    /// (<c>DeviceRecord.FirstSeen</c>) it still counts as "newly detected" and gets a highlighted
    /// row on the Devices page. Maps to ASTER's 2–20 second highlight-duration slider — same range
    /// enforced by <see cref="ClampHighlightSeconds"/>.</summary>
    public int NewDeviceHighlightSeconds { get; set; } = 5;

    public const int MinHighlightSeconds = 2;
    public const int MaxHighlightSeconds = 20;

    public static int ClampHighlightSeconds(int seconds) =>
        Math.Clamp(seconds, MinHighlightSeconds, MaxHighlightSeconds);

    /// <summary>
    /// Whether a device counts as "newly detected" right now, given when it was first registered
    /// and the configured highlight window. Pure/static so it's unit-testable without a live
    /// clock or a GUI grid — the Devices page's row highlighting calls this directly.
    /// </summary>
    public static bool IsRecentlyAdded(DateTime firstSeenUtc, DateTime nowUtc, int highlightSeconds) =>
        nowUtc - firstSeenUtc <= TimeSpan.FromSeconds(ClampHighlightSeconds(highlightSeconds));
}
