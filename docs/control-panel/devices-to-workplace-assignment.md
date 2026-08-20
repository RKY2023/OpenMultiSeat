# Devices to Workplace(s) Assignment

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_settermtodev

## What ASTER does

The "Devices to Workplace(s) Assignment" window, opened via "Workplace appointment…" on a device's context menu, lets an administrator choose which workplace(s) may use a given device. It offers per-workplace checkboxes plus two convenience modes: "To All" for devices meant to be shared by every workplace simultaneously (e.g., speakers), and "To None" for devices that must be exclusive to a single workplace (e.g., monitors). The same context menu carries a few smaller related actions on each device slot: **Indicate device** (flashes/highlights the physical device so an administrator can confirm which one it is), **Info**, and **Set custom icon**.

## OpenMultiSeat status

🚧 **Stub** — placeholder only today. See [Known Issues](../known-issues.md).

This is the core missing piece of OpenMultiSeat's **Seats** page. The page currently shows only a heading, a one-line description, and a single "Configure Seats" button that pops a generic `MessageBox.Show(...)` info dialog with no real bound controls — even though the underlying device enumeration is fully working on the Devices page.

A real implementation would need:

- A context-menu action ("Assign to seat") on rows in the Devices grid, or an equivalent control on the Seats page.
- Per-seat checkboxes reflecting the `Seat` list from `SeatConfiguration`, allowing a device to be enabled for one, several, or all seats.
- A shared/exclusive toggle equivalent to ASTER's "To All" / "To None" shortcuts.
- Persistence through `IDevicePersistence` (implemented by `OpenMultiSeat.Devices.DevicePersistence`), which already stores device records (`DeviceRecord`) and would need a seat-assignment field wired to this UI.

![Seats page stub](../images/screenshots/seats-page-stub.png)
