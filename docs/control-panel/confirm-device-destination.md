# Confirm Device Destination

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_selectdevdialog

## What ASTER does

The "Confirm Device Destination" window appears after an administrator drags and drops a device onto a different workplace in ASTER's control panel. It shows a table of the affected devices with a before/after column comparing their current and proposed workplace assignment, and lets the user check which of the listed devices should actually be reassigned before clicking OK to apply the change (or Cancel to discard it).

## OpenMultiSeat status

📋 **Planned** — no current implementation. See [Known Issues](../known-issues.md) for what the surrounding pages now do.

The Devices and Seats pages are both real today, and [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md) covers the *first* assignment of a device to a seat (a real "Assign to Seat…" action exists on the Devices page). What this specific window covers — *reassigning* an already-assigned device from one seat to another, with a before/after confirmation step — is still missing. Today, moving a device means unassigning it first (a separate action, no confirmation dialog) and assigning it again; there's no drag-and-drop, no single "move" action, and no batched multi-device confirmation table.

A real implementation would need:

- A drag-and-drop or context-menu action on the Devices grid to initiate a device-to-seat move (rather than requiring an explicit unassign step first).
- A confirmation dialog listing the moved `DeviceRecord` entries with "current seat" and "new seat" columns.
- Per-row checkboxes so only confirmed devices are actually reassigned.
- Wiring into `ISeatManager` (already exists: `UnassignDeviceFromSeatAsync` + `AssignDeviceToSeatAsync`, just not called as a single atomic "move" today).
