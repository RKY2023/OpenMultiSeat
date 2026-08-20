# Reset Settings

> ASTER reference: observed in the live ASTER v2.70.5 desktop app (Workplaces tab → hamburger menu → "Reset settings") — not covered by the public wiki pages this doc set otherwise cites.

## What ASTER does

"Reset settings" discards the current Workplaces-tab configuration (device/display/audio assignments, workplace names, hotkeys, and the other per-installation settings covered by this section of the control panel) and restores ASTER's defaults, as an escape hatch for a configuration that's gotten into a broken or confusing state.

## OpenMultiSeat status

📋 **Planned** — no current implementation.

This would be a single action in the Settings page that clears the persisted `SeatConfiguration` (and any device/display/audio assignments stored through `IDevicePersistence`) back to an empty/default state, requiring a confirmation prompt given how destructive it is. It has no dependencies beyond the seat-assignment model itself ([Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md)) existing first — there's nothing to reset until there's real configuration state to reset.
