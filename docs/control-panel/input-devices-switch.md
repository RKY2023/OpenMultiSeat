# Input Devices Switch

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_inputdevswitcher

## What ASTER does

The "Input Devices Switch" window, opened from a system image's context menu on the Workplace tab, lets an administrator configure a keyboard hotkey (default "Ctrl + F12") for switching a keyboard/mouse between workplaces on demand. Users can rebind the combination by focusing the field and pressing new keys, disable it entirely with "Reset," and confirm with OK or discard with Cancel.

## OpenMultiSeat status

🚧 **Stub** — placeholder only today. See [Known Issues](../known-issues.md).

This maps to OpenMultiSeat's **Input Isolation** page, which today shows only a heading, a one-line description, and a single "Configure Input Isolation" button that pops a generic `MessageBox.Show(...)` info dialog — there are no real bound controls.

`OpenMultiSeat.InputIsolation` already implements the underlying mechanism: it routes each keyboard/mouse's input events to the correct seat only, so input never leaks between seats. A hotkey-based manual switch is a natural extension of that routing logic. A real implementation of this page would need:

- A hotkey-capture field (records the next key combination pressed, like ASTER's "Ctrl+" field), defaulting to a chosen combination.
- OK/Cancel/Reset buttons, with Reset disabling the hotkey switch entirely.
- Wiring the captured combination into `OpenMultiSeat.InputIsolation`'s routing logic so it can reassign a device's active seat target at runtime.
- Persistence of the configured hotkey per device or globally, likely alongside the other per-seat isolation settings on this page.

![Input Isolation page stub](../images/screenshots/input-isolation-page-stub.png)
