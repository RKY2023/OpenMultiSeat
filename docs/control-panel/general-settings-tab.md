# "General Settings" Tab

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform_tabcontrol_tabcommon

## What ASTER does

The General Settings tab groups configuration into three areas: **Windows Settings** (shortcuts to Proxy Settings, Device Manager for creating the virtual network devices ASTER uses to hand out per-workplace IP addresses, and Network Connections); **ASTER Settings** (enabling/disabling the ASTER service, starting workstations, choosing how workplaces start — manually via a button, automatically at system startup, or automatically at first login — plus language, interface style, and color scheme); and **ASTER Activation Settings** (opening the Registration/activation dialog and reviewing past activation IDs).

## OpenMultiSeat status

**🚧 Partially implemented** — maps to the Settings page reachable from the main window's left nav.

Only one piece of this page is real: **"Assign CPU Cores…"**, which opens the working `AssignCpuCoresWindow` (see [Assign CPU Cores](assign-cpu-cores.md)). Everything else ASTER Settings covers — the service enable/start toggle, and specifically **how workplaces start** (manually via a button / automatically at system startup / automatically at first login) — has no equivalent yet. The page's other button, **"Configure Settings…"**, is a bare stub: it pops a `MessageBox.Show("Configure Settings")` and does nothing else, the same non-functional-placeholder pattern the Input Isolation page has (see [Known Issues](../known-issues.md)).

Concretely, that means:

- **No seat-startup-mode setting exists anywhere in the model or GUI** — no `manual`/`at system startup`/`at first login` field on `Seat` or elsewhere, and nothing governs *when* a seat's session comes up. Today the only way to start a seat's session at all is the manual, one-off "Test Launch…" button on the [User Account](user-account-for-workstation.md) dialog (added this round) — there is no automatic trigger of any kind, system-startup or otherwise.
- **[Confirming Starting of Workplaces](confirming-starting-of-workplaces.md)** (the "do you want to start workplaces now?" prompt ASTER shows on manual-startup boot) remains unbuilt for the same reason — it has nothing to gate, since there's no startup-mode setting or automatic startup path yet.
- No **Windows Settings** shortcuts for Proxy/Device Manager/virtual-NIC creation — OpenMultiSeat doesn't hand out per-workplace IP addresses; it targets local seats (keyboard/mouse/monitor/audio sets) on one physical PC rather than networked workplaces.
- No **ASTER Activation Settings** group — see [Activation Dialog](activation-dialog.md); OpenMultiSeat has no licensing or activation system.

Building the startup-mode setting for real would mean: a persisted mode field (on `Seat` or a machine-wide setting), a way to trigger `SessionManager`/`ProcessLauncher` at actual Windows startup (a scheduled task or a Windows service entry point — `OpenMultiSeat.Service` exists as a project but doesn't do this today) or at first login (a logon-triggered hook), and the confirmation dialog above gating the manual-start path. None of that exists yet.
