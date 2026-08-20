# Assigning Video Outputs

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_monitorconnection

## What ASTER does

The "Assigning Video Outputs" window lets an administrator manually map each video surface (workplace display) to a specific output on the graphics card, for setups where the automatic detection doesn't match the desired layout. It also exposes a software-vs-hardware mouse pointer setting and an "Allow cloning of displays" option that lets one video output mirror its image to multiple monitors.

## OpenMultiSeat status

🚧 **Partially implemented.**

The **Displays** page now does a real scan (`OpenMultiSeat.Displays.DisplayEnumerator`, via the Windows Display Configuration APIs) and lists every detected display — name, resolution, refresh rate, connection type, primary flag — with a working **"Assign to Seat…"**/**Unassign** action bound to `ISeatManager.AssignDisplayToSeatAsync`/`UnassignDisplayFromSeatAsync`. An "Assigned To" column shows current assignment, computed the same way as the Devices page (cross-referencing each `Seat.DisplayIds`, since `ISeatManager` has no direct "which seat owns this display" query).

What's different from ASTER's version of this window, and from OpenMultiSeat's own Devices assignment:

- **No persisted display registry.** `OpenMultiSeat.Displays` has nothing analogous to `IDevicePersistence` — the grid reflects a live scan, not a saved list, and starts empty until "Scan Displays" runs.
- **`Display.DisplayId` isn't a generated stable ID.** It's derived from the current Windows display topology (`path.TargetInfo.Id`), unlike devices' `GenerateStableIdAsync`. Unplugging/replugging a monitor or moving it to a different port can change this ID, which could orphan an existing assignment — not solved here; a real fix would likely hash `Display.EdidData` (a field that already exists on the model but isn't populated by `DisplayEnumerator` today) into a stable identifier instead.
- **No manual output-to-surface remapping.** ASTER's version lets an admin override automatic detection when it doesn't match the desired layout; this just assigns whatever `DisplayEnumerator` detects.
- **No clone/extend toggle** — no equivalent of ASTER's "Allow cloning of displays" checkbox.
- Still direct-persistence for the *seat* side, not IPC (see [Known Issues](../known-issues.md)) — same caveat as every other real page today.

Pointer-type (software/hardware cursor) selection has no current equivalent in OpenMultiSeat and would need to be added as new configuration if implemented.
