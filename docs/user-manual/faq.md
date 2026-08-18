# Frequently Asked Questions

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/faq and https://dokwiki.ibiksoft.com/en/v3/core/faq/faq_config

## ASTER's approach

ASTER organizes its FAQ into five categories: **PC System & Hardware Requirements** (reference builds, GPU compatibility, incompatible software), **Licensing, Activation & Deactivation** (reactivation, license transfer, terms of licensing), **Desktops & Workplaces** (identical desktop setups, multi-station login, monitor-less workstations, remote access, sleep/hibernate behavior), **ASTER & Other Software** (antivirus compatibility, browser/Steam deployment across seats), and **Other Topics** (updates, Windows 10 notes, troubleshooting, USB sharing via USBDLM, disk cloning). A large share of this content — the entire "Licensing" category plus parts of "Other Topics" (update checks) — exists purely to support ASTER's paid, activation-based model.

## OpenMultiSeat's approach

### What is multi-seat computing?

Multi-seat computing lets one physical Windows PC serve multiple people at once, each with their own keyboard, mouse, monitor, and audio device, each running their own independent, isolated Windows session simultaneously — instead of buying a separate PC per person.

### What hardware do I need?

At minimum: one 64-bit Windows PC with enough CPU/RAM/storage headroom to run several Windows sessions concurrently, plus per-seat peripherals — a keyboard and mouse per seat (fanned out via USB ports/hubs), a monitor per seat (driven by a multi-output GPU or multiple GPUs), and an audio output per seat (USB sound devices or a multi-output motherboard chipset). See [`system-requirements.md`](system-requirements.md) for full detail.

### Does OpenMultiSeat cost anything? Do I need a license?

**No, and no.** OpenMultiSeat is free and open source with no licensing or activation system whatsoever — no trial period, no per-seat fees, no license keys, no license server. This is the core way it differs from ASTER, which requires a paid Pro or Annual license per additional workplace after a 14-day trial. See [`licensing-model.md`](licensing-model.md).

### Will OpenMultiSeat work with my existing software?

Because OpenMultiSeat runs standard, independent Windows sessions (via the same session/`CreateProcessAsUser` mechanisms Windows itself uses for multi-user login), most ordinary desktop software should behave normally per seat. As with ASTER, expect friction from software that assumes exclusive access to one specific piece of hardware system-wide — GPU vendor utilities (GeForce Experience-style tools), benchmarking/overclocking utilities, and other multiseat products running at the same time. Hardware-bound USB security dongles and some game controllers are also typically usable from only one seat at a time on Windows in general, independent of which multiseat product you use. If you hit a specific incompatibility, please report it via [`technical-support.md`](technical-support.md) so it can be tracked.

### How many seats does OpenMultiSeat support?

There is no license-imposed seat cap — unlike ASTER, where every additional seat requires another paid license, OpenMultiSeat does not meter or gate seat count at all. The practical limit is your hardware: available USB ports/hubs for input devices, available GPU outputs for displays, available audio outputs, and the CPU/RAM/storage headroom to run that many concurrent Windows sessions.

### How does OpenMultiSeat compare to ASTER?

They target the same core use case — turning one PC into several independent workstations — and OpenMultiSeat's architecture (privileged service + admin GUI, per-seat device/display/audio assignment, session management) mirrors the shape of ASTER's feature set. The differences that matter most:

| | ASTER | OpenMultiSeat |
|---|---|---|
| Cost | Paid, per-seat licensing after a 14-day trial | Free, open source, unlimited seats |
| Activation | Required, hardware-locked, needs a license server | None — nothing to activate |
| Proxy/firewall needs | May need proxy config to reach the license server | None — no license-server traffic exists |
| Support | Commercial support channels + community forum | Community support via GitHub Issues |
| Maturity | Long-established commercial product | Newer project; Devices page fully implemented, Seats/Displays/Input Isolation/Audio assignment UI still in progress (see [`software-features-and-limitations.md`](software-features-and-limitations.md)) |

### Where do I report bugs or ask questions?

Through GitHub Issues on the project repository: **https://github.com/RKY2023/OpenMultiSeat**. See [`technical-support.md`](technical-support.md) for details — there is no phone/chat/ticketed commercial support desk.

### Is OpenMultiSeat production-ready today?

Device enumeration and the installer are solid. Full per-seat configuration (Seats, Displays, Input Isolation, Audio pages) is still under active development — check [`../known-issues.md`](../known-issues.md) and [`software-features-and-limitations.md`](software-features-and-limitations.md) before planning a production rollout.
