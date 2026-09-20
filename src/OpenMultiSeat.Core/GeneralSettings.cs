namespace OpenMultiSeat.Core;

/// <summary>
/// How seats' sessions start, mirroring ASTER General Settings tab's "how workplaces start"
/// choice. This is a single machine-wide setting, not per-seat — same as ASTER's own design.
/// </summary>
public enum SeatStartMode
{
    /// <summary>Nothing starts automatically. Sessions are started by hand from the "Start
    /// Workplaces Now" button on the Settings page.</summary>
    Manual,

    /// <summary>Every eligible seat's session is started automatically as soon as Windows boots,
    /// via a Windows Scheduled Task (Task Scheduler trigger "At startup", running as SYSTEM).
    /// See docs/control-panel/general-settings-tab.md for a real limitation this mode runs into:
    /// SYSTEM cannot decrypt a seat password that was DPAPI-protected under a different account.</summary>
    AtSystemStartup,

    /// <summary>Every eligible seat's session is started automatically as soon as the seat
    /// designated "Workplace 1" (the first seat in the seat list) logs into Windows, via a
    /// Scheduled Task logon trigger bound to that seat's Windows account.</summary>
    AtFirstLogin
}

/// <summary>Machine-wide OpenMultiSeat settings outside any one seat's own configuration.</summary>
public sealed class GeneralSettings
{
    public SeatStartMode StartMode { get; set; } = SeatStartMode.Manual;
}
