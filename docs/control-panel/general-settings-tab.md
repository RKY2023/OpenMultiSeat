# "General Settings" Tab

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform_tabcontrol_tabcommon

## What ASTER does

The General Settings tab groups configuration into three areas: **Windows Settings** (shortcuts to Proxy Settings, Device Manager for creating the virtual network devices ASTER uses to hand out per-workplace IP addresses, and Network Connections); **ASTER Settings** (enabling/disabling the ASTER service, starting workstations, choosing how workplaces start — manually via a button, automatically at system startup, or automatically at first login — plus language, interface style, and color scheme); and **ASTER Activation Settings** (opening the Registration/activation dialog and reviewing past activation IDs).

## OpenMultiSeat status

**🚧 Partially implemented** — maps to the Settings page reachable from the main window's left nav.

**"Assign CPU Cores…"** opens the working `AssignCpuCoresWindow` (see [Assign CPU Cores](assign-cpu-cores.md)). The former "Configure Settings…" stub has been replaced outright with a real **Workplace Start Mode** setting — ASTER's "how workplaces start" choice — plus a real **"Start Workplaces Now"** manual-trigger button:

- **Workplace Start Mode** is a dropdown with the same three choices ASTER offers: **Manual**, **Automatically at system startup**, and **Automatically via Workplace 1 login** ("at first login," specifically the login of the seat treated as "Workplace 1" — the first seat in the seat list). Selecting an automatic option registers a real Windows Scheduled Task (`schtasks.exe /Create`, via `WindowsStartupTriggerManager`) with an `ONSTART` or `ONLOGON` trigger; selecting Manual removes both. The chosen mode itself persists to `%AppData%\OpenMultiSeat\settings.json` via `GeneralSettingsPersistence`.
- Both scheduled tasks invoke `OpenMultiSeat.GUI.exe --start-seats` — a genuinely headless run (no `MainWindow`, driven by a hand-written `Program.Main` rather than `App.OnStartup`, specifically so a blocking async call doesn't deadlock waiting on a WPF Dispatcher pump that hasn't started yet) that calls `SeatStartupOrchestrator.StartAllSeatsAsync`. That orchestrator starts every enabled seat with a saved (non-"Display login dialog") Windows login by launching Explorer as that seat's account via the same verified `CreateProcessWithLogonW` path the [User Account dialog's](user-account-for-workstation.md) "Test Launch" button already exercises — it's still "run this program as a different user," not a hardware-isolated seat login (same caveat as that dialog). Results are appended to `%AppData%\OpenMultiSeat\startup-log.txt`, since a Scheduled Task run at boot/logon has no console for the usual logger to write to.
- **"Start Workplaces Now"** runs the same orchestrator interactively, behind a Yes/No confirmation prompt standing in for ASTER's own [Confirming Starting of Workplaces](confirming-starting-of-workplaces.md) dialog, and reports a per-seat success/skip/fail summary in a message box.

**A real, disclosed limitation — At System Startup specifically:** seat passwords are DPAPI-protected with `DataProtectionScope.CurrentUser` (see [User Account for Workstation](user-account-for-workstation.md)), decryptable only by the same Windows account that saved them. The At-System-Startup task runs as `SYSTEM`, which is *not* that account, so it currently can't decrypt any seat's saved password — the scheduled task genuinely runs and genuinely tries, but every seat with a saved auto-login password fails with a decrypt error (visible in `startup-log.txt`), and only "Display login dialog" seats (which never attempted auto-login anyway) are unaffected. Fixing this for real would mean moving to `DataProtectionScope.LocalMachine` (weaker — any local process/account can decrypt) or running the boot-time trigger from a proper Windows service holding its own key material, not this scheduled-task shortcut. **At First Login doesn't hit this**, but only works end-to-end if OpenMultiSeat's seat passwords were saved while logged in as the same Windows account as "Workplace 1" — a natural setup (the PC's main/admin user *is* usually Workplace 1) but not guaranteed.

Concretely, that means:

- **A seat-startup-mode setting now exists** — `SeatStartMode` (`Manual` / `AtSystemStartup` / `AtFirstLogin`) on a new machine-wide `GeneralSettings` record, not per-seat, matching ASTER's own single General-Settings-tab choice.
- **[Confirming Starting of Workplaces](confirming-starting-of-workplaces.md)** now has a real (if narrower) counterpart: a Yes/No prompt in front of the manual "Start Workplaces Now" button. ASTER's own version is a boot-time prompt shown automatically under Manual mode specifically — OpenMultiSeat's Manual mode registers no scheduled task at all (nothing runs automatically), so there's nothing to prompt from at boot; the confirm here is button-triggered instead.
- No **Windows Settings** shortcuts for Proxy/Device Manager/virtual-NIC creation — OpenMultiSeat doesn't hand out per-workplace IP addresses; it targets local seats (keyboard/mouse/monitor/audio sets) on one physical PC rather than networked workplaces.
- No **ASTER Activation Settings** group — see [Activation Dialog](activation-dialog.md); OpenMultiSeat has no licensing or activation system.

See [Known Issues](../known-issues.md) for the full file list and the DPAPI/SYSTEM caveat's test coverage.
