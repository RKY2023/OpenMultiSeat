# Assign CPU Cores

> ASTER reference: observed in the live ASTER v2.70.5 desktop app (Workplaces tab → hamburger menu → "Assign CPU cores") — not covered by the public wiki pages this doc set otherwise cites.

## What ASTER does

The "Workplace CPU cores usage" window shows a matrix with one row per configured workplace and one column per logical CPU core (`CPU 0`, `CPU 1`, … up to however many cores the host has). Each cell is a checkbox: checking it lets that workplace's processes run on that core, unchecking it excludes the core from that workplace's affinity mask. Every checkbox starts checked (all workplaces can use all cores) until an administrator narrows a workplace down to a subset — the standard use case is reserving a fixed set of cores for one seat (e.g. a gaming seat) so its workload can't starve the other seats sharing the same physical CPU. "Save" applies the matrix; "Cancel" discards changes.

## OpenMultiSeat status

✅ **Implemented** — the first OpenMultiSeat page in this doc set to move past the "planned" stage after ASTER's own live UI motivated it.

- `Seat.CpuCoreAffinity` (`OpenMultiSeat.Core`) is a list of allowed logical-core indices; empty means unrestricted.
- `ICpuAffinityProvider` / `CpuAffinityProvider` (`OpenMultiSeat.Core`) turn a list of core indices into a Windows affinity bitmask — pure bit math, fully unit-tested (`tests/OpenMultiSeat.Tests/CpuAffinityTests.cs`).
- `OpenMultiSeat.Sessions.ProcessLauncher.CreateProcessInSession` applies the mask via the Windows `SetProcessAffinityMask` API right after `CreateProcessAsUser` succeeds, while the process handle is still open; a failure here is logged, not thrown, since the process is already running by that point.
- `SessionManager.LaunchProcessInSessionAsync` takes an optional `cpuCoreAffinity` parameter and only computes/applies a mask when one is actually supplied — an empty or omitted list means "don't touch affinity at all," matching Windows' own default.
- **"Assign CPU Cores…"** is a real button on the Settings page, opening `AssignCpuCoresWindow` — a checkbox matrix (seats × logical cores, sized from `Environment.ProcessorCount`) that reads and writes real `Seat` records.

**Caveat, stated plainly:** the GUI reads/writes seat data by calling `ISeatPersistence` directly against the same on-disk store (`%AppData%\OpenMultiSeat\seats.json`) the Service's `SeatManager` uses — there is no IPC round-trip through `OpenMultiSeat.IPC` yet, because *no* GUI page has that wiring today (see the note on `DevicesPage` in [Known Issues](../known-issues.md)). The dialog also depends on seats already existing: since the Seats page still can't create a seat, `AssignCpuCoresWindow` correctly shows an empty state ("No seats are configured yet") until a seat exists via some other path (e.g. `SeatManager.CreateSeatAsync` called directly, or a future working Seats page).

See [UML Class Diagram](../diagrams/uml-class-diagram.md) and [DFD](../diagrams/dfd.md) for how this fits the target architecture.
