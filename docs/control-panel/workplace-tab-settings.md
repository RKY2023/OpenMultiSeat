# Workplace Tab Settings

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_termviewsettings

## What ASTER does

The "Workplace Tab Settings" window configures how workplaces and their connected devices are visually presented in the ASTER control panel. Options include showing displays that aren't currently linked to any adapter, showing devices shared across workplaces, adjusting device icon size, distributing workplace tiles evenly, and a slider (2–20 seconds) controlling how long a purple highlight frame appears around a device icon when it's newly detected.

## OpenMultiSeat status

🚧 **Partially implemented** — the two options with a real equivalent are wired up. Icon size and tile distribution still have nothing to control: there is now a real tile layout ([Tile Layout window](workplaces-tab.md), opened from the Seats page), but its columns are a fixed width sized to the seat count, not a resizable canvas with icons that could be made bigger/smaller or redistributed.

A new **"View Settings…"** button on the Seats page opens `WorkplaceTabSettingsWindow`, backed by a real `WorkplaceViewSettings` record (`%AppData%\OpenMultiSeat\view-settings.json`, via `WorkplaceViewSettingsPersistence`):

- **"Show displays not linked to any seat"** (checkbox, default on) — maps directly to ASTER's "show unlinked displays" toggle. When off, the Displays page hides displays with no seat assignment from its grid, and the status line reports how many were hidden.
- **Highlight newly detected devices, 2–20 seconds** (slider, default 5) — maps to ASTER's highlight-duration slider. The Devices page highlights (light-yellow row background) any device whose `DeviceRecord.FirstSeen` timestamp falls within that many seconds of "now," recomputed every time the grid loads or is refreshed. This isn't a live, self-expiring highlight the way ASTER's is (no timer ticks it away while you watch — it just won't be highlighted the *next* time you load or refresh after the window passes) since the Devices page only updates on Scan/Refresh, not a live device-added feed; the actual *value* being highlighted (a genuinely-recently-registered device, via the real `FirstSeen` timestamp `DevicePersistence` already sets on first registration) is real, though.

What has **no equivalent** here, and why:

- **"Show devices shared across workplaces."** OpenMultiSeat's assignment model is exclusive-only (see [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md)) — there's no "shared across workplaces" concept to toggle visibility of yet.
- **Device icon size, "distribute workplace tiles evenly."** The Seats page is a `DataGrid` (rows and columns), not a tile/card layout with device icons — there's nothing for either setting to resize or redistribute. Building these for real would mean building the tile layout first.

See [Known Issues](../known-issues.md) for where the code lives.
