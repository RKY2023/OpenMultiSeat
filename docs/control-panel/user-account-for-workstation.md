# "User Account for Workstation" Window

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_logininfo

## What ASTER does

This window lets an administrator change the Windows login used for a given workplace and choose whether that workplace logs in automatically or shows a login prompt at startup. It adapts to the account type: for a domain account it takes a domain name, username, and password (with confirmation); for a local account it offers a dropdown of local accounts plus password/confirmation and a shortcut button into Windows' own "User accounts" tool. The local-account dropdown's default entry is **"Display login dialog"** rather than any specific account — meaning the workplace shows Windows' normal login prompt instead of auto-logging in; picking an actual account name from the dropdown switches to unattended auto-login using that account's stored credentials. A companion **"Force relogin"** action (on the workplace's own context menu, alongside this dialog) signs the current session out immediately so a changed account/login setting takes effect without waiting for a restart.

## OpenMultiSeat status

**📋 Planned** — no current implementation. OpenMultiSeat needs an equivalent per-seat account-assignment dialog, reachable from the [Seats page](workplaces-tab.md)'s per-seat controls, letting an admin pick a local or domain Windows account (and optionally its credentials) for a seat's session, with the same "prompt vs. auto-login" default choice ASTER offers. The backend for this already exists: `OpenMultiSeat.Sessions`' `SessionManager` and `ProcessLauncher` use `CreateProcessAsUser` to start a session's process as a specific Windows user, and `SessionEnumerator` (WTS APIs) can enumerate existing sessions/accounts to populate a picker and to implement "Force relogin" (terminate the seat's current WTS session so the next launch re-authenticates). What's missing is entirely on the GUI side — no account-selection dialog, credential fields, or auto-login-vs-prompt toggle exist yet, and no such assignment is wired into `SeatConfiguration`.
