# About OpenMultiSeat

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_about

## ASTER's approach

ASTER describes itself simply: "a program (and only a program!) which allows you to create several workplaces on the basis of one system unit" — no thin clients or terminal stations needed, just extra monitors, keyboards, mice, and optionally audio devices or game controllers plugged into one PC. ASTER states it can support up to 12 workplaces depending on the machine's capacity, with setups of 6 or more recommended to test in advance for compatibility. It's offered as a 14-day trial, after which continued use requires a paid per-seat license.

## OpenMultiSeat's approach

OpenMultiSeat is a free and open-source Windows multi-seat system built on the same core idea: turn one physical PC into several independent workstations by fanning out keyboards, mice, monitors, and audio devices to separate Windows sessions, instead of buying a thin client or a separate PC per seat. Architecturally it's a privileged Windows Service that owns device enumeration, display detection, session lifecycle, and audio routing, paired with an unprivileged WPF admin GUI — the "OpenMultiSeat Administration Console" — that the administrator uses to configure and monitor seats, talking to the Service over local Named Pipe IPC. See `docs\architecture.md` in the repository for the full component breakdown.

The most important difference from ASTER is licensing: **OpenMultiSeat has no trial period, no license keys, no activation, and no per-seat licensing limits — it is fully free and open source, permanently.** There is nothing to buy, nothing to activate, and no seat count that unlocks more functionality; every seat the software is technically capable of driving is available from install. Every "license" or "activation" page in this documentation set (see [Activation IDs on this PC](../control-panel/activation-ids-on-this-pc.md), [Create Master License](../control-panel/create-master-license.md)) exists only to explain that OpenMultiSeat has no equivalent — because there is genuinely nothing there to configure.

Today's GUI is a mix of finished and in-progress pages: the **Devices** page is fully implemented, with a real device grid and working scan/refresh/export actions. The **Seats**, **Displays**, **Input Isolation**, and **Audio** pages are currently stubs — each shows a heading, a short description, and a single button, while the underlying backend logic in `OpenMultiSeat.Sessions`, `OpenMultiSeat.Displays`, `OpenMultiSeat.InputIsolation`, and `OpenMultiSeat.Audio` already exists and awaits UI wiring. The project's practical capacity for concurrent seats depends on the same real-world factors ASTER calls out — available USB ports/hubs, GPU outputs, and CPU/RAM — rather than any license-imposed ceiling.

For hands-on setup instructions, see [Installation](installation.md) and the repository's `docs\INSTALLATION_GUIDE.md`. For support, see the project's GitHub repository: [github.com/RKY2023/OpenMultiSeat](https://github.com/RKY2023/OpenMultiSeat).
