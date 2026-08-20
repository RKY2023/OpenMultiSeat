# Proxy Configuration for Corporate Clients

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_proxysetup_summary

## ASTER's approach

ASTER ships a dedicated **Proxy Server Program** plus a **Configurator** app specifically so that corporate networks with restrictive outbound firewalls can still reach ASTER's licensing infrastructure. The documented purpose is explicit: the proxy "receives a request from the PC with ASTER installed via the local area network (LAN), forwards it to the requested licensing server, receives the response via the internet, and then returns it to the originator" — while also acting as a filter that "allows requests only to certain hosts and rejecting all others."

The related "Setting up ASTER" page adds the client-side half of this: an administrator can point ASTER at an HTTP or SOCKS5 proxy (hostname, port, optional credentials, with a "Test" button to validate connectivity) specifically to "address internet connectivity issues with key activation or support requests." In other words, this entire feature category — a standalone proxy binary, a configurator, per-client proxy settings, connection testing — exists because ASTER **must** periodically phone home to a license server to activate, reactivate, or validate a key, and locked-down corporate networks often block that traffic by default.

## OpenMultiSeat's approach

**OpenMultiSeat makes no license-server calls, no telemetry calls, and no other outbound network calls at all today.** There is nothing on a corporate network that needs to be reached, so there is currently no proxy configuration to document, no proxy binary to deploy, and no firewall rule to punch a hole for.

This is a genuine, positive difference from ASTER, not just an absence of documentation: because OpenMultiSeat has no licensing system (see [`licensing-model.md`](licensing-model.md)), it has no activation traffic that a corporate firewall could block in the first place. IT administrators evaluating OpenMultiSeat for a locked-down environment do not need to:

- Open outbound firewall rules for a license server.
- Deploy or maintain a separate proxy relay service.
- Configure per-machine proxy credentials.
- Worry about license activation failing silently behind a web filter.

**When this could change:** if a future version of OpenMultiSeat adds an update-checker (an equivalent of ASTER's "Check for New Version" feature, which contacts a server to report the latest available release), that feature would be the first legitimate reason to need outbound network/proxy configuration. If and when that happens, this page will be updated with the relevant proxy settings. Until then, treat "no proxy needed" as accurate, current behavior — not a placeholder.
