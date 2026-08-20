# Devices to Workplace(s) Assignment

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_settermtodev

## What ASTER does

The "Devices to Workplace(s) Assignment" window, opened via "Workplace appointment…" on a device's context menu, lets an administrator choose which workplace(s) may use a given device. It offers per-workplace checkboxes plus two convenience modes: "To All" for devices meant to be shared by every workplace simultaneously (e.g., speakers), and "To None" for devices that must be exclusive to a single workplace (e.g., monitors). The same context menu carries a few smaller related actions on each device slot: **Indicate device** (flashes/highlights the physical device so an administrator can confirm which one it is), **Info**, and **Set custom icon**.

## OpenMultiSeat status

🚧 **Partially implemented.**

The Devices page has a working **"Assign to Seat…"** action (select a device row → pick a seat and a Keyboard/Mouse type → Assign), plus **Unassign**, both bound to `ISeatManager.AssignDeviceToSeatAsync`/`UnassignDeviceFromSeatAsync`. An "Assigned To" column on the same grid shows current assignment, computed by cross-referencing each `Seat`'s `KeyboardIds`/`MouseIds` (there's no dedicated "which seat owns this device" query on `ISeatManager`, so the GUI builds that lookup itself from the seat list).

What's different from ASTER's version of this window:

- **Exclusive-only, no "To All" shared mode.** `SeatManager`'s assignment model is exclusive by design — a device already assigned to one seat is rejected if you try to assign it to another, with no equivalent of ASTER's "To All" (share this device across every workplace simultaneously). Building that would mean changing the underlying `Seat.KeyboardIds`/`MouseIds` model, not just the GUI.
- **No reassignment flow.** Moving a device from Seat A to Seat B currently means unassigning it first, then assigning it again — there's no drag-and-drop or single-step "move" action (that's really what ASTER's separate [Confirm Device Destination](confirm-device-destination.md) window covers).
- **No "Indicate device" / "Set custom icon" / per-device "Info".** Only the assignment action itself is built.
- Still direct-persistence, not IPC (see [Known Issues](../known-issues.md)) — same caveat as every other real page today.
