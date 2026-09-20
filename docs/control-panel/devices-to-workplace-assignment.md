# Devices to Workplace(s) Assignment

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_settermtodev

## What ASTER does

The "Devices to Workplace(s) Assignment" window, opened via "Workplace appointment…" on a device's context menu, lets an administrator choose which workplace(s) may use a given device. It offers per-workplace checkboxes plus two convenience modes: "To All" for devices meant to be shared by every workplace simultaneously (e.g., speakers), and "To None" for devices that must be exclusive to a single workplace (e.g., monitors). The same context menu carries a few smaller related actions on each device slot: **Indicate device** (flashes/highlights the physical device so an administrator can confirm which one it is), **Info**, and **Set custom icon**.

## OpenMultiSeat status

🚧 **Partially implemented.**

The Devices page has a working **"Assign to Seat…"** action (select a device row → pick a seat and a Keyboard/Mouse type → Assign), plus **Unassign**, both bound to `ISeatManager.AssignDeviceToSeatAsync`/`UnassignDeviceFromSeatAsync`. An "Assigned To" column on the same grid shows current assignment, computed by cross-referencing each `Seat`'s `KeyboardIds`/`MouseIds` (there's no dedicated "which seat owns this device" query on `ISeatManager`, so the GUI builds that lookup itself from the seat list).

Camera/USB/Bluetooth devices (from `GeneralDeviceEnumerator`) are assignable too now, via a separate `ISeatManager.AssignOtherDeviceToSeatAsync`/`UnassignOtherDeviceFromSeatAsync` pair and a new `Seat.OtherDeviceIds` list — the Devices page routes "Assign to Seat…" to a dedicated `AssignOtherDeviceToSeatWindow` for these device classes instead of the Keyboard/Mouse dialog. This is stated plainly in that dialog's own text: it's an **ownership record only**. Unlike keyboard/mouse assignment (which `InputIsolationService` actually acts on) or display/audio assignment (which real subsystems route), there's no isolation mechanism that restricts a camera or USB hub to one seat's session — assigning one here just records "this seat owns this device" for inventory/organizational purposes, the same exclusivity guarantee (rejects assigning an already-assigned device to a second seat) but no functional effect yet.

What's different from ASTER's version of this window:

- **Exclusive-only, no "To All" shared mode.** `SeatManager`'s assignment model is exclusive by design — a device already assigned to one seat is rejected if you try to assign it to another, with no equivalent of ASTER's "To All" (share this device across every workplace simultaneously). Building that would mean changing the underlying `Seat.KeyboardIds`/`MouseIds` model, not just the GUI.
- **Reassignment now confirms and moves in one action** — picking a different seat for an already-assigned device shows a before/after confirm (current seat → new seat) via `ConfirmDeviceDestinationWindow`, then unassigns and reassigns on confirmation, matching ASTER's separate [Confirm Device Destination](confirm-device-destination.md) window conceptually. Still no drag-and-drop (it's the existing "Assign to Seat…" dropdown flow) and still two `ISeatManager` calls under the hood, not one atomic move.
- **No "Indicate device" / "Set custom icon" / per-device "Info".** Only the assignment action itself is built.
- Still direct-persistence, not IPC (see [Known Issues](../known-issues.md)) — same caveat as every other real page today.

The Devices page also now lists cameras, general USB controllers/hubs, and Bluetooth devices/radios (via a WMI `Win32_PnPEntity` scan, `GeneralDeviceEnumerator`) alongside the keyboard/mouse devices Raw Input reports — real visibility into what's connected, matching what ASTER's device list would show, and (as above) these are now assignable as ownership records.
