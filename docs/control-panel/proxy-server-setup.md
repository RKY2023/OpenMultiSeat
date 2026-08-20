# Proxy Server Setup

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_proxysetup

## What ASTER does

The "Proxy Server Setup" window lets a user configure an HTTP or SOCKS5 proxy (host, port, and optional username/password) that ASTER uses for its network connections. It's reached from the "Proxy Settings" button on the General Settings tab or from the license Activation dialog, and includes a Test button to validate connectivity before saving.

## OpenMultiSeat status

➖ **Not applicable today.** OpenMultiSeat makes no outbound network calls of any kind in its current implementation — there's no license server to activate against (see [Activation IDs on this PC](activation-ids-on-this-pc.md) and [Create Master License](create-master-license.md)) and no cloud service it phones home to. Everything runs locally: the GUI talks to the Service exclusively over local Named Pipe IPC (see `docs\architecture.md`), and configuration/state stay in `%ProgramData%\OpenMultiSeat\`. A proxy setting would have nothing to apply to.

📋 **Planned, conditionally.** The one feature that would need outbound HTTP is a future update checker (see [Check for Updates](check-for-updates.md)), which doesn't exist yet either. If and when that's implemented, environments that only allow internet egress through a proxy would need a way to configure one for that single HTTPS call — most likely by respecting the system proxy configuration (`WinHTTP`/`netsh winhttp`) rather than adding a dedicated ASTER-style proxy dialog, since update-checking is the only thing that would ever need it. Until an update checker exists, this page has nothing to configure.
