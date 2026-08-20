# IP Address for the Workplace

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_selectapps

## What ASTER does

The "IP Address for the Workplace" window lets an administrator assign a static IP address to a workplace and bind that address to specific applications or folders — either applied to all programs, only to a chosen list, or to all programs except a chosen list. It's primarily used for network games and other software that needs a distinct, predictable network identity per seat.

## OpenMultiSeat status

📋 **Planned** — no current implementation.

OpenMultiSeat currently targets a single physical PC with multiple local seats (keyboard/mouse/monitor/audio fanned out via USB hubs or multi-head GPUs); there is no networked/remote "workplace" concept and no per-seat virtual network identity today. ASTER's per-workplace IP binding assumes each workplace can present itself as a distinct network endpoint, which OpenMultiSeat's architecture does not yet address.

If this were implemented, it would likely build on:

- `OpenMultiSeat.Sessions.ProcessLauncher`, which already uses `CreateProcessAsUser` to start a process inside a specific seat's session — the natural place to inject a per-seat network identity (e.g., a virtual adapter or loopback binding) before launch.
- A new Core interface (e.g., `INetworkIdentityProvider`) and a corresponding manager, following the same dependency-inversion pattern used for `IDevicePersistence` and `IDisplayEnumerator`.
- A UI list/table of applications and folders per seat with include/exclude modes, similar to ASTER's "Apply to all" / "Apply only to listed" / "Apply to all except listed" options.

This is a larger architectural addition (virtual network adapters or a Windows Filtering Platform-based binding) rather than a simple GUI page, so it remains unscheduled.
