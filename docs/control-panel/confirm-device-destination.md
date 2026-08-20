# Confirm Device Destination

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_selectdevdialog

## What ASTER does

The "Confirm Device Destination" window appears after an administrator drags and drops a device onto a different workplace in ASTER's control panel. It shows a table of the affected devices with a before/after column comparing their current and proposed workplace assignment, and lets the user check which of the listed devices should actually be reassigned before clicking OK to apply the change (or Cancel to discard it).

## OpenMultiSeat status

🚧 **Stub** — placeholder only today. See [Known Issues](../known-issues.md).

This is part of the device-to-seat assignment flow that would live across OpenMultiSeat's **Seats** and **Devices** pages. The Devices page itself is fully implemented (data grid with Device Name, Hardware ID, Vendor ID, Product ID, First Seen columns, plus Scan/Refresh/Export Report), but there is no drag-and-drop reassignment or confirmation step yet — the Seats page is currently just a heading, description, and a "Configure Seats" button that pops a generic `MessageBox.Show(...)` dialog.

A real implementation would need:

- A drag-and-drop or context-menu action on the Devices grid to initiate a device-to-seat move (see also [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md)).
- A confirmation dialog listing the moved `InputDevice`/`DeviceRecord` entries with "current seat" and "new seat" columns.
- Per-row checkboxes so only confirmed devices are actually reassigned.
- OK/Cancel wiring that calls into `IDevicePersistence` (implemented by `OpenMultiSeat.Devices.DevicePersistence`) to persist the new seat assignment.

![Seats page stub](../images/screenshots/seats-page-stub.png)
