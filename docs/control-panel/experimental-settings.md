# Experimental Settings

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_unsupportedsettings

## What ASTER does

The "Unsupported/Experimental Settings" window exposes two opt-in checkboxes for advanced, potentially unstable behavior: "Support for special graphical modes" (can improve full-screen game performance but may cause BSODs) and "Support for display devices" (improves compatibility with Display-Only-Devices such as DisplayLink monitors, reducing visual artifacts). Both require a system restart to take effect and are explicitly framed as experimental, unsupported settings.

## OpenMultiSeat status

📋 **Planned** — OpenMultiSeat has no experimental-settings panel yet, but the concept is a good fit for it. Once OpenMultiSeat has more than one or two genuinely risky/opt-in behaviors, a dedicated "Experimental" section on the Settings page would be the natural home for them, following the same pattern ASTER uses: clearly labeled, off by default, with a warning that they may be unstable and require a restart.

Concrete candidates that would belong there as they mature:

- Any future full-screen exclusive-mode or GPU-passthrough tuning for `OpenMultiSeat.Displays`, analogous to ASTER's "special graphical modes" checkbox.
- DisplayLink / USB-graphics adapter support in `OpenMultiSeat.Displays.DisplayEnumerator`, analogous to ASTER's Display-Only-Devices option — OpenMultiSeat's current display enumeration targets standard GPU outputs and does not yet special-case USB display adapters.
- Early input-isolation drivers or kernel-level hooks in `OpenMultiSeat.InputIsolation` that aren't yet considered stable enough to enable by default.

There is no existing feature-flag infrastructure in the GUI or Service today (no config schema entries, no toggle plumbing over IPC) — this would need to be built alongside whichever first experimental feature justifies it, rather than as a standalone empty settings page.
