# System Requirements

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_miscinfo and https://dokwiki.ibiksoft.com/en/v3/core/faq/faq_config

## ASTER's approach

ASTER documents fairly detailed hardware guidance because multi-seat computing is fundamentally hardware-constrained — every seat needs its own keyboard, mouse, monitor(s), and (usually) audio device, all fed from one physical PC. Its published guidance includes:

- **Operating system:** Windows 8/8.1/10/11 (Windows 7 and earlier dropped as of ASTER 2.51).
- **Example reference build for 6 workstations:** Intel Core i5 7500 (integrated graphics for 3 monitors) + a discrete GPU for 3 more, 16GB+ RAM, SSD storage, an 800W power supply.
- **RAM formula:** base OS footprint (1–4GB) plus roughly 500MB per additional workstation.
- **Per-seat peripherals:** a dedicated keyboard and mouse per user, and either an on-board or discrete GPU output (or USB/wireless display adapter) per monitor.
- **Display connectivity:** any ATI/Nvidia/Intel GPU works; USB video adapters (FrescoLogic, DisplayLink) and wireless display (WiDi/Miracast) are also supported, with cable-length limits noted (USB ~5m, HDMI ~5m, DVI 10–15m, VGA 5–50m depending on resolution).
- **Input devices:** no special drivers needed for standard keyboards/mice; wireless sets may interfere with each other; joysticks/touch/Xbox controllers and most USB security dongles only work on the first workstation.
- **Software conflicts:** competing multiseat products (BeTwin, SoftXpand, WM Program) must be uninstalled first; GPU vendor utilities (GeForce Experience, VulkanRT, AMD Gaming Evolved, MSI Afterburner, RivaTuner, etc.) can conflict and should be removed if problems occur, with a recommendation to do a selective/custom GPU driver install.
- **Licensing prerequisite:** a valid ASTER license/trial and (behind restrictive firewalls) a way to reach the license server — see [`licensing-model.md`](licensing-model.md) and [`proxy-for-corporate-clients.md`](proxy-for-corporate-clients.md) for why OpenMultiSeat has no equivalent requirement here.

## OpenMultiSeat's approach

OpenMultiSeat's requirements are similar in shape — it's solving the same physical multi-seat problem — but the software side is far lighter and, critically, carries **no license requirement of any kind**.

**Operating system**
- **64-bit Windows is required.** The NSIS installer actively checks for this and will refuse to install on a 32-bit system.
- **Administrator rights are required** to install. The installer requests elevation, and the OpenMultiSeat Service itself must run with the privileges needed to enumerate hardware, manage Windows sessions (via WTS APIs), and launch per-seat processes (`CreateProcessAsUser`).

**Runtime**
- OpenMultiSeat is a **framework-dependent .NET 9 deployment**, meaning the machine needs the .NET 9 runtime available (rather than bundling a self-contained runtime into the installer). Make sure the appropriate .NET 9 desktop/runtime components are installed before or alongside setup.

**Per-seat hardware** (mirrors ASTER's model — one PC, multiple simultaneous independent Windows sessions):
- **Input:** a dedicated keyboard and mouse (or combined set) per seat, fanned out via USB ports and/or USB hubs. The Devices page enumerates these through Windows' HID/SetupAPI and Raw Input APIs today.
- **Displays:** one or more monitors per seat, driven by either a multi-output GPU or multiple discrete GPUs/graphics adapters — the same "one graphics card can drive several monitors, but monitors for a single seat should share a card" logic ASTER documents applies here too, since it's a Windows/GPU-driver constraint rather than an ASTER- or OpenMultiSeat-specific one.
- **Audio:** a distinct audio output per seat — either USB sound cards/headsets per seat or a multi-output motherboard audio chipset, so each seat's session is routed to its own audio device rather than sharing the PC's single default output.
- **General sizing:** as with ASTER, plan RAM, CPU, and storage headroom per additional seat — each seat runs a full independent Windows session concurrently, so resource needs scale roughly linearly with seat count. No official OpenMultiSeat sizing formula is published yet; ASTER's rough "≈500MB RAM per extra workstation" figure is a reasonable starting estimate for planning purposes until OpenMultiSeat publishes its own.

**What OpenMultiSeat does *not* require, unlike ASTER**
- No license key, no trial period, no activation, no per-seat license purchase.
- No proxy/firewall exception to reach a license server (there isn't one).
- No separate license verification tool.

**Known current limitation:** because the Seats/Displays/Input Isolation/Audio configuration pages are still stub UI (see [`software-features-and-limitations.md`](software-features-and-limitations.md) and [`../known-issues.md`](../known-issues.md)), device *enumeration* works today but full per-seat *assignment* through the GUI is still in progress — factor that into hardware planning if you need a fully working multi-seat deployment right now versus evaluating the roadmap.
