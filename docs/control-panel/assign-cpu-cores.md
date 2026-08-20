# Assign CPU Cores

> ASTER reference: observed in the live ASTER v2.70.5 desktop app (Workplaces tab → hamburger menu → "Assign CPU cores") — not covered by the public wiki pages this doc set otherwise cites.

## What ASTER does

The "Workplace CPU cores usage" window shows a matrix with one row per configured workplace and one column per logical CPU core (`CPU 0`, `CPU 1`, … up to however many cores the host has). Each cell is a checkbox: checking it lets that workplace's processes run on that core, unchecking it excludes the core from that workplace's affinity mask. Every checkbox starts checked (all workplaces can use all cores) until an administrator narrows a workplace down to a subset — the standard use case is reserving a fixed set of cores for one seat (e.g. a gaming seat) so its workload can't starve the other seats sharing the same physical CPU. "Save" applies the matrix; "Cancel" discards changes.

## OpenMultiSeat status

📋 **Planned** — no current implementation, and no existing page in this doc set covered CPU-level isolation before this one.

OpenMultiSeat currently isolates seats along four axes: input devices ([Input Isolation](../known-issues.md)), displays, audio, and (planned) network identity ([IP Address for the Workplace](ip-address-for-workplace.md)) — but not CPU scheduling. On a single physical PC running several seats' workloads simultaneously, an unbounded seat can still starve its neighbors' CPU time even with devices/displays/audio fully isolated, so this is a real gap for parity with ASTER, not just a cosmetic one.

A real implementation would need:

- A new Core interface, e.g. `ICpuAffinityProvider`, following the same dependency-inversion pattern as `IDevicePersistence` and `IDisplayEnumerator` — interface in `OpenMultiSeat.Core`, implementation in a feature project (most naturally `OpenMultiSeat.Sessions`, since that's where processes are actually started).
- An affinity-mask field on `Seat`/`SeatConfiguration`, persisted alongside the existing seat model.
- Applying the mask at process-launch time in `OpenMultiSeat.Sessions.ProcessLauncher`, via the Windows `SetProcessAffinityMask` API right after `CreateProcessAsUser` starts a seat's session process (and its children, if scoping needs to extend beyond the initial shell).
- A GUI matrix (workplaces × logical cores) on the Seats page, populated from `Environment.ProcessorCount` and read/written through the same IPC channel (`OpenMultiSeat.IPC`) used for other seat configuration.

See [UML Class Diagram](../diagrams/uml-class-diagram.md) and [DFD](../diagrams/dfd.md) for how this fits the target architecture.
