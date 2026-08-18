# "User Account for Workstation" Window

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_logininfo

## What ASTER does

This window lets an administrator change the Windows login used for a given workplace and choose whether that workplace logs in automatically or shows a login prompt at startup. It adapts to the account type: for a domain account it takes a domain name, username, and password (with confirmation); for a local account it offers a dropdown of local accounts plus password/confirmation and a shortcut button into Windows' own "User accounts" tool.

## OpenMultiSeat status

**📋 Planned** — no current implementation. OpenMultiSeat needs an equivalent per-seat account-assignment dialog, reachable from the [Seats page](workplaces-tab.md)'s per-seat controls, letting an admin pick a local or domain Windows account (and optionally its credentials) for a seat's session. The backend for this already exists: `OpenMultiSeat.Sessions`' `SessionManager` and `ProcessLauncher` use `CreateProcessAsUser` to start a session's process as a specific Windows user, and `SessionEnumerator` (WTS APIs) can enumerate existing sessions/accounts to populate a picker. What's missing is entirely on the GUI side — no account-selection dialog, credential fields, or auto-login-vs-prompt toggle exist yet, and no such assignment is wired into `SeatConfiguration`.
