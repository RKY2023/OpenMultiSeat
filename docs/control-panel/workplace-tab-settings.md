# Workplace Tab Settings

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_termviewsettings

## What ASTER does

The "Workplace Tab Settings" window configures how workplaces and their connected devices are visually presented in the ASTER control panel. Options include showing displays that aren't currently linked to any adapter, showing devices shared across workplaces, adjusting device icon size, distributing workplace tiles evenly, and a slider (2–20 seconds) controlling how long a purple highlight frame appears around a device icon when it's newly detected.

## OpenMultiSeat status

🚧 **Stub** — placeholder only today. See [Known Issues](../known-issues.md).

This is part of the **Seats** page, which today is only a heading, a one-line description, and a "Configure Seats" button wired to a generic `MessageBox.Show(...)` dialog — there is no seat-tile layout, device iconography, or view-settings UI yet.

A real implementation would need:

- A visual seat/workplace layout view (tiles or cards, one per `Seat`) as the foundation these settings would customize.
- A settings panel or flyout with toggles for showing unlinked displays and shared devices, an icon-size control, and a layout-distribution toggle.
- A duration slider for "newly detected device" highlighting, tied to device-added notifications coming from `OpenMultiSeat.Devices.HidDeviceEnumerator` over the IPC channel (`OpenMultiSeat.IPC`) from the Service to the GUI.

None of this UI exists today; it depends on the Seats page first getting a real bound layout (see [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md) for the underlying assignment model).

![Seats page stub](../images/screenshots/seats-page-stub.png)
