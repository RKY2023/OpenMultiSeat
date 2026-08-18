# "About" Tab

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform_tabcontrol_tababout

## What ASTER does

The About tab is split in two: the left half shows program information (version number, license particulars, copyright), and the right half shows client information (registered name and email, number of licensed workstations, and current ASTER status). It links out to the ASTER website and user manual in the default browser, offers an update-check link when run as administrator, and includes a Support Request button.

## OpenMultiSeat status

**✅ Implemented** — maps to the About page reachable from the main window's left nav.

OpenMultiSeat's About page carries the parts of ASTER's About tab that still apply to a free, open-source project: application name and version, and license information (MIT — see the repository's `LICENSE` file). It drops everything tied to ASTER's commercial model:

- No **client information** panel (registered name/email, licensed workstation count) — there is nothing to license.
- No **update-check-for-administrators** flow gated on running elevated — OpenMultiSeat has no license server to check against; version currency is tracked through the project's GitHub releases instead.
- The outbound links point at the OpenMultiSeat project repository and documentation rather than an ASTER-style commercial website, and there is no Support Request button — GitHub Issues fills that role.

See [Activation Dialog](activation-dialog.md) for why OpenMultiSeat has no licensing concepts at all.
