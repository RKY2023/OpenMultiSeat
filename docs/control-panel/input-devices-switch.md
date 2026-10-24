# Input Devices Switch

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_inputdevswitcher

## What ASTER does

The "Input Devices Switch" window, opened from a system image's context menu on the Workplace tab, lets an administrator configure a keyboard hotkey (default "Ctrl + F12") for switching a keyboard/mouse between workplaces on demand. Users can rebind the combination by focusing the field and pressing new keys, disable it entirely with "Reset," and confirm with OK or discard with Cancel.

## OpenMultiSeat status

🚧 **Partially implemented** — real bind/unbind + isolation toggle, not ASTER's specific hotkey-rebind UI.

The **Input Isolation** page (left nav) is wired to the real `IInputIsolationService` backend, not a `MessageBox` stub: it lists every registered keyboard/mouse (from the device registry, same source as the Devices page), shows which seat each one is bound to, and offers **Enable/Disable Isolation**, **Bind to Seat…**, **Unbind**, and **Validate Configuration** — the last of which surfaces real problems (a binding pointing at a deleted seat, a device bound twice, a seat with no keyboard/mouse) via `InputIsolationService.ValidateIsolationConfigAsync`. Enabling isolation refuses to proceed if there are critical validation errors, same as the service was already designed to do.

What's still missing relative to ASTER's own window specifically:

- **No hotkey capture or rebinding.** ASTER's actual UI is a single "press a key combination" field (default Ctrl+F12) that lets an admin *reassign a device's active seat at runtime* with a keystroke. `IInputIsolationService` has no concept of a hotkey at all — binding a device to a seat here is a persistent configuration change (like the Devices page's own assignment), not a runtime on-the-fly switch. Building the literal ASTER feature would mean: a global low-level keyboard hook to detect the combo, a hotkey-capture control (records the next key combo pressed, like ASTER's field), and wiring a detected press to `BindDeviceToSeatAsync` for whichever device triggered it — none of that exists.
- **No OK/Cancel/Reset dialog shape.** This is a full page with immediate-effect buttons (matching the rest of OpenMultiSeat's GUI), not a modal dialog with a Reset-to-disable option.

**A pre-existing architecture note, surfaced by wiring this page up (not introduced by it):** `ISeatManager.AssignDeviceToSeatAsync` (used by the Devices page, writing `Seat.KeyboardIds`/`MouseIds`) and `IInputIsolationService.BindDeviceToSeatAsync` (used by this page, writing a separate `input-bindings.json`) are two **independent** stores that both mean "this keyboard/mouse belongs to this seat" — assigning a device on the Devices page does not bind it here, and vice versa, so the two pages can show a device as owned by different seats (or one assigned/unbound and the other not) at the same time. See [Known Issues](../known-issues.md) for the full note; reconciling the two into one store is a separate decision from getting this page off the `MessageBox` stub.
