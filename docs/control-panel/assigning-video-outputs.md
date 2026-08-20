# Assigning Video Outputs

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_monitorconnection

## What ASTER does

The "Assigning Video Outputs" window lets an administrator manually map each video surface (workplace display) to a specific output on the graphics card, for setups where the automatic detection doesn't match the desired layout. It also exposes a software-vs-hardware mouse pointer setting and an "Allow cloning of displays" option that lets one video output mirror its image to multiple monitors.

## OpenMultiSeat status

🚧 **Partially implemented.**

The **Displays** page now does a real scan (`OpenMultiSeat.Displays.DisplayEnumerator`) and lists every detected display — name, resolution, refresh rate, position, primary flag — with a working **"Assign to Seat…"**/**Unassign** action bound to `ISeatManager.AssignDisplayToSeatAsync`/`UnassignDisplayFromSeatAsync`. An "Assigned To" column shows current assignment, computed the same way as the Devices page (cross-referencing each `Seat.DisplayIds`, since `ISeatManager` has no direct "which seat owns this display" query).

`DisplayEnumerator` uses the classic `EnumDisplayMonitors`/`GetMonitorInfo`/`EnumDisplaySettings` GDI APIs, not the newer DisplayConfig API family (`QueryDisplayConfig` etc.) it was originally built on. That first version turned out to be doubly broken: a P/Invoke struct-size mismatch caused genuine heap corruption on every call (a 12-byte-undersized struct relative to the real Win32 layout — `QueryDisplayConfig` wrote past the end of a CLR-allocated array), and after fixing that, `QueryDisplayConfig` still returned all-zeroed path data even though Windows correctly reported an active display existed. Rather than keep chasing a second marshaling bug in a more complex, less-documented API family, it was replaced with the simpler GDI approach — verified directly against a real machine (`System.Windows.Forms.Screen.AllScreens`, a thin wrapper over these same calls, correctly found the real monitor where `QueryDisplayConfig` returned nothing usable). Monitor friendly names come from a WMI `Win32_DesktopMonitor` lookup, matched by enumeration order (not a reliable key, see below).

What's different from ASTER's version of this window, and from OpenMultiSeat's own Devices assignment:

- **No connection/output type** (HDMI, DisplayPort, etc.). GDI doesn't expose this — only the DisplayConfig API family does, and that's the one just removed for reliability reasons. `Display.ConnectionType` is always `null` now.
- **Friendly-name matching is positional, not keyed.** WMI's `Win32_DesktopMonitor` has no property that reliably correlates to a `\\.\DISPLAYn` device name, so names are matched by enumeration order — fine for one or two monitors, not guaranteed correct for larger setups.
- **No persisted display registry.** `OpenMultiSeat.Displays` has nothing analogous to `IDevicePersistence` — the grid reflects a live scan, not a saved list, and starts empty until "Scan Displays" runs.
- **`Display.DisplayId` isn't a generated stable ID.** It's the raw `\\.\DISPLAYn` device path GDI assigns, which can be reassigned to a different physical monitor across reboots or cable changes on multi-monitor setups. Unlike devices' `GenerateStableIdAsync`, there's no hardware-derived stable identifier — a real fix would likely hash `Display.EdidData` (a field that already exists on the model but isn't populated today) into one instead.
- **No manual output-to-surface remapping.** ASTER's version lets an admin override automatic detection when it doesn't match the desired layout; this just assigns whatever `DisplayEnumerator` detects.
- **No clone/extend toggle** — no equivalent of ASTER's "Allow cloning of displays" checkbox.
- Still direct-persistence for the *seat* side, not IPC (see [Known Issues](../known-issues.md)) — same caveat as every other real page today.

Pointer-type (software/hardware cursor) selection has no current equivalent in OpenMultiSeat and would need to be added as new configuration if implemented.
