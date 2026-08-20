# "User Account for Workstation" Window

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_logininfo

## What ASTER does

This window lets an administrator change the Windows login used for a given workplace and choose whether that workplace logs in automatically or shows a login prompt at startup. It adapts to the account type: for a domain account it takes a domain name, username, and password (with confirmation); for a local account it offers a dropdown of local accounts plus password/confirmation and a shortcut button into Windows' own "User accounts" tool. The local-account dropdown's default entry is **"Display login dialog"** rather than any specific account — meaning the workplace shows Windows' normal login prompt instead of auto-logging in; picking an actual account name from the dropdown switches to unattended auto-login using that account's stored credentials. A companion **"Force relogin"** action (on the workplace's own context menu, alongside this dialog) signs the current session out immediately so a changed account/login setting takes effect without waiting for a restart.

## OpenMultiSeat status

🚧 **Partially implemented.**

The Seats page has a real **"User Account…"** dialog (`UserAccountWindow`), reachable by selecting a seat: Local/Domain account type, a local-account dropdown populated from real Windows accounts (via WMI `Win32_UserAccount`, defaulting to **"Display login dialog"** exactly like ASTER), and password/confirmation fields. `Seat.WindowsUser`, `WindowsDomain`, `DisplayLoginDialog`, and `EncryptedPassword` are all real, persisted fields. A password, when set, is protected with Windows DPAPI (`SeatCredentialProtector`, `CurrentUser` scope) and stored base64-encoded — never in plaintext — with a round-trip verified in `tests/OpenMultiSeat.Tests/SeatCredentialProtectorTests.cs`.

What's deliberately **not** built yet, and is a materially bigger and more security-sensitive piece than the configuration dialog itself:

- **No actual auto-login / session launch using this data.** Configuring a seat's account here doesn't make anything happen at session-start time — nothing reads `EncryptedPassword` back and calls `CreateProcessWithLogonW` (or an equivalent unattended-logon flow) yet. `SessionManager`/`ProcessLauncher` still only use `CreateProcessAsUser` against an *already-logged-in* WTS session (see the [sequence diagrams](../diagrams/sequence-diagrams.md)), which is a different, narrower operation than logging a user in from scratch.
- **No "Force relogin" action.** Would need `SessionEnumerator`'s WTS APIs to terminate the seat's current session — not implemented.
- **DPAPI's real constraint, stated plainly:** `CurrentUser` scope means the encrypted password can only be decrypted by the same Windows user account that encrypted it, on the same machine. Whatever eventually consumes this data (a future auto-login feature) needs to run as that same account — worth knowing before building on top of this.
