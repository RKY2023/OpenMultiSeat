# Set Log Verbosity

> ASTER reference: observed in the live ASTER v2.70.5 desktop app (Workplaces tab → hamburger menu → "Set log verbosity") — not covered by the public wiki pages this doc set otherwise cites.

## What ASTER does

The "Log setup" window is a small dialog with three independent checkboxes — **Trace**, **Debug**, **Info** — that control how much detail ASTER writes to its own log files. They can be combined; "Info" is checked by default. OK applies the change immediately, Cancel discards it.

## OpenMultiSeat status

📋 **Planned** — no current in-GUI log-level control.

The diagnostics/event data this would govern already has a home in the target architecture: the "D3: Diagnostics/event log" data store in the [DFD](../diagrams/dfd.md), written to by the Session Launch and Device Enumeration processes via a `DiagnosticsCollector`. What's missing is (a) a configurable verbosity level in that collector — most naturally an enum (`Trace`/`Debug`/`Info`/`Warning`/`Error`) rather than ASTER's independent checkboxes, since standard .NET logging levels are ordered rather than freely combinable — and (b) a small settings-page control (a dropdown or radio group) wired to it over IPC. This is a small, self-contained addition once `DiagnosticsCollector` exists; it has no other architectural dependencies.
