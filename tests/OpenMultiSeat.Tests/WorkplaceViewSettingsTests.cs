using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.Tests;

[TestClass]
public class WorkplaceViewSettingsTests
{
    [TestMethod]
    public void IsRecentlyAdded_WithinWindow_ReturnsTrue()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstSeen = now.AddSeconds(-3);

        Assert.IsTrue(WorkplaceViewSettings.IsRecentlyAdded(firstSeen, now, highlightSeconds: 5));
    }

    [TestMethod]
    public void IsRecentlyAdded_ExactlyAtWindowBoundary_ReturnsTrue()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstSeen = now.AddSeconds(-5);

        Assert.IsTrue(WorkplaceViewSettings.IsRecentlyAdded(firstSeen, now, highlightSeconds: 5));
    }

    [TestMethod]
    public void IsRecentlyAdded_PastWindow_ReturnsFalse()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstSeen = now.AddSeconds(-6);

        Assert.IsFalse(WorkplaceViewSettings.IsRecentlyAdded(firstSeen, now, highlightSeconds: 5));
    }

    [TestMethod]
    public void IsRecentlyAdded_HighlightSecondsBelowMinimum_ClampsToMinimum()
    {
        // ASTER's own slider is 2-20 seconds; a caller passing 0 (or a negative value) should be
        // treated as the 2-second floor, not "never highlight" or a negative window.
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstSeenOneSecondAgo = now.AddSeconds(-1);

        Assert.IsTrue(WorkplaceViewSettings.IsRecentlyAdded(firstSeenOneSecondAgo, now, highlightSeconds: 0));
    }

    [TestMethod]
    public void IsRecentlyAdded_HighlightSecondsAboveMaximum_ClampsToMaximum()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstSeen25SecondsAgo = now.AddSeconds(-25);

        Assert.IsFalse(WorkplaceViewSettings.IsRecentlyAdded(firstSeen25SecondsAgo, now, highlightSeconds: 100));
    }

    [TestMethod]
    public void ClampHighlightSeconds_WithinRange_Unchanged()
    {
        Assert.AreEqual(10, WorkplaceViewSettings.ClampHighlightSeconds(10));
    }

    [TestMethod]
    public void ClampHighlightSeconds_BelowMinimum_ClampsToMinimum()
    {
        Assert.AreEqual(WorkplaceViewSettings.MinHighlightSeconds, WorkplaceViewSettings.ClampHighlightSeconds(-5));
    }

    [TestMethod]
    public void ClampHighlightSeconds_AboveMaximum_ClampsToMaximum()
    {
        Assert.AreEqual(WorkplaceViewSettings.MaxHighlightSeconds, WorkplaceViewSettings.ClampHighlightSeconds(999));
    }

    [TestMethod]
    public void DefaultSettings_ShowUnassignedDisplaysTrueAndFiveSecondHighlight()
    {
        var settings = new WorkplaceViewSettings();

        Assert.IsTrue(settings.ShowUnassignedDisplays);
        Assert.AreEqual(5, settings.NewDeviceHighlightSeconds);
    }
}
