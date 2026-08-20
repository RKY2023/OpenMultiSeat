# "General Settings" Tab

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform_tabcontrol_tabcommon

## What ASTER does

The General Settings tab groups configuration into three areas: **Windows Settings** (shortcuts to Proxy Settings, Device Manager for creating the virtual network devices ASTER uses to hand out per-workplace IP addresses, and Network Connections); **ASTER Settings** (enabling/disabling the ASTER service, starting workstations, choosing how workplaces start — manually via a button, automatically at system startup, or automatically at first login — plus language, interface style, and color scheme); and **ASTER Activation Settings** (opening the Registration/activation dialog and reviewing past activation IDs).

## OpenMultiSeat status

**✅ Implemented** — maps to the Settings page reachable from the main window's left nav.

OpenMultiSeat's Settings page covers the application-level equivalent of ASTER Settings — enabling/starting the multi-seat service and choosing how seat sessions come up — without the parts of ASTER's tab that don't apply to OpenMultiSeat's design:

- No **Windows Settings** shortcuts for Proxy/Device Manager/virtual-NIC creation — OpenMultiSeat doesn't hand out per-workplace IP addresses; it targets local seats (keyboard/mouse/monitor/audio sets) on one physical PC rather than networked workplaces.
- No **ASTER Activation Settings** group — see [Activation Dialog](activation-dialog.md); OpenMultiSeat has no licensing or activation system.

The seat-startup-mode concept (manual / at system startup / at first login) is the closest parallel to work still ahead — it would govern when `OpenMultiSeat.Sessions`' `SessionManager` brings up each seat's session and pairs with the not-yet-built [Confirming Starting of Workplaces](confirming-starting-of-workplaces.md) dialog. See [Known Issues](../known-issues.md) for the current state of Settings-page functionality.
