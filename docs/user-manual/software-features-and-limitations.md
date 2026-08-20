# Software Features and Limitations

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_miscinfo

## ASTER's approach

ASTER's "Important Information" page is mostly about licensing mechanics (hardware-bound activation, one-computer-at-a-time, paid licenses required per additional workplace) and known software conflicts (competing multiseat tools that must be removed; GPU vendor utilities like GeForce Experience, VulkanRT, AMD Gaming Evolved, MSI Afterburner, and RivaTuner that can cause instability). It doesn't present a structured features/limitations table — that information is scattered across the miscellaneous-info and FAQ pages.

## OpenMultiSeat's approach

An honest, current snapshot of what works today versus what's still in progress. Status reflects the codebase as of this writing, not the eventual roadmap.

| Feature | Status | Notes |
|---|---|---|
| HID device enumeration (Devices page) | ✅ Implemented | Real data grid backed by SetupAPI/Raw Input enumeration, with Scan, Refresh, and Export actions. |
| Windows installer (NSIS) | ✅ Implemented | Enforces 64-bit Windows and admin elevation; installs the Service and GUI together. |
| Privileged Service + IPC | ✅ Implemented | Windows Service host with Named Pipe IPC to the GUI, ACL-restricted to authenticated users, allowlisted operations only. |
| Display enumeration (backend) | ✅ Implemented | `OpenMultiSeat.Displays` detects monitors via Windows Display Configuration APIs and parses EDID for resolution/refresh info. |
| Audio endpoint enumeration (backend) | ✅ Implemented | `OpenMultiSeat.Audio` enumerates endpoints via Windows Core Audio / MMDevice API. |
| Session management (backend) | ✅ Implemented | `OpenMultiSeat.Sessions` handles Windows session lifecycle via WTS APIs and `CreateProcessAsUser`. |
| Crash recovery / diagnostics | ✅ Implemented | `OpenMultiSeat.Reliability` covers crash recovery, USB reconnect handling, and diagnostics collection. |
| Seats page (per-seat configuration UI) | 🚧 Stub | Placeholder page with a single button that opens a generic info popup — no real seat-assignment UI yet. See [`../known-issues.md`](../known-issues.md). |
| Displays page (monitor assignment UI) | 🚧 Stub | Backend enumeration exists; drag-and-drop/assignment UI is not yet built. |
| Input Isolation page (keyboard/mouse routing UI) | 🚧 Stub | Input isolation layer is architected (Phase 5+ per [`../architecture.md`](../architecture.md)) but the assignment UI and system-wide routing are not yet exposed. |
| Audio page (per-seat audio assignment UI) | 🚧 Stub | Backend enumeration exists; per-seat routing UI is not yet built. |
| USB hub cascading assignment | 📋 Not yet implemented | ASTER lets you assign an entire USB hub (and its children) to a seat at once; OpenMultiSeat currently enumerates and would assign individual devices. |
| Per-seat network/IP identity | 📋 Not yet implemented | No equivalent yet to ASTER's optional per-workplace IP-address assignment feature. |
| Remote / networked workplaces | 📋 Not yet implemented | Everything today targets one local physical PC with directly attached peripherals; there is no remote-seat or virtual-display-over-network capability yet. |
| Update checker | 📋 Not yet implemented | No "check for new version" feature exists yet; see [`proxy-for-corporate-clients.md`](proxy-for-corporate-clients.md) for why this also means no proxy config is needed today. |
| Licensing / activation limits | ➖ Not applicable — by design | OpenMultiSeat is free and open source with **no licensing system, no trial period, and no per-seat fee**, unlike ASTER's paid per-seat licensing after a 14-day trial. This isn't a missing feature — it's a deliberate architectural difference. See [`licensing-model.md`](licensing-model.md). |

**Legend:** ✅ implemented and usable today · 🚧 architected/partially built, UI is a stub · 📋 not yet started · ➖ intentionally absent (not a gap)
