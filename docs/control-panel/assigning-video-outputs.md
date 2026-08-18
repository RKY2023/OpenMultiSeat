# Assigning Video Outputs

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_monitorconnection

## What ASTER does

The "Assigning Video Outputs" window lets an administrator manually map each video surface (workplace display) to a specific output on the graphics card, for setups where the automatic detection doesn't match the desired layout. It also exposes a software-vs-hardware mouse pointer setting and an "Allow cloning of displays" option that lets one video output mirror its image to multiple monitors.

## OpenMultiSeat status

🚧 **Stub** — placeholder only today. See [Known Issues](../known-issues.md).

This maps to the **Displays** page in the OpenMultiSeat Administration Console. Currently the page shows only a heading, a one-line description, and a single "Configure Displays" button that pops a `MessageBox.Show(...)` info dialog — there is no real bound UI yet.

The Core layer already has the pieces this page would need: `OpenMultiSeat.Displays.DisplayEnumerator` (implementing Core's `IDisplayEnumerator`) enumerates physical displays and their video outputs via the Windows Display Configuration APIs, and the `Display` domain model in `OpenMultiSeat.Core` represents each one. A real implementation of this page would need:

- A grid or list of detected displays/video outputs (adapter, output id, connector type, current resolution, connected/disconnected state).
- A per-seat assignment control (dropdown or drag-and-drop) binding each display to a `Seat`, backed by `SeatConfiguration`.
- A clone/extend toggle per output, equivalent to ASTER's "Allow cloning of displays" checkbox.
- Visual indication of the currently assigned output per seat, similar to ASTER's bold/highlighted checkbox label.

Pointer-type (software/hardware cursor) selection has no current equivalent in OpenMultiSeat and would need to be added as new configuration if implemented.

![Displays page stub](../images/screenshots/displays-page-stub.png)
