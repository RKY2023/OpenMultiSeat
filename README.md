# OpenMultiSeat

**Version:** 0.1  
**Date:** 2026-08-14  
**Target Platform:** Windows 11 x64

Open-source Windows multi-seat system — run two or more independent local workstations on a single physical PC using one Windows installation, with dedicated monitors, keyboards, mice, audio devices, and user sessions.

## Project Status

**Phase:** Admin console built out; core session isolation still open  
**Current Milestone:** Every GUI page wired to a real backend; MSI installer shipping

The WPF admin console is real and usable — device/display/audio enumeration and
per-seat assignment, an ASTER-style tile layout with drag-and-drop, CPU-core affinity,
and Workplace Start Mode backed by Windows Scheduled Tasks. What's **not** solved is the
hardest part: launching a genuinely hardware-isolated desktop session per seat.

- [RELEASE_NOTES.md](./RELEASE_NOTES.md) — current build, artifacts, install steps, verification status
- [docs/known-issues.md](./docs/known-issues.md) — honest per-feature status
- [DESIGN.md](./DESIGN.md) — complete technical specification

## Quick Start

### Prerequisites

- Windows 11 x64
- .NET 9 SDK or later
- Visual Studio 2022 (or VS Code with C# Dev Kit)

### Building

```bash
dotnet build src\OpenMultiSeat.GUI\OpenMultiSeat.GUI.csproj -c Release
```

### Running Tests

```bash
dotnet test tests\OpenMultiSeat.Tests\OpenMultiSeat.Tests.csproj -c Release
```

> A whole-solution `dotnet build OpenMultiSeat.sln` currently fails in
> `scripts\Phase0.Poc.csproj` (stale missing runtime asset in its own `bin\`), unrelated to
> the GUI or the tests — build those two projects directly, as above.

### Packaging

See [RELEASE_NOTES.md](./RELEASE_NOTES.md) for the exact `dotnet publish` and
`wix build` commands that produce the standalone exe and the MSI installer.

## Architecture

```
+-------------------------------------------------------------+
|                         Windows 11                          |
|                                                             |
|  +----------------------- OpenMultiSeat ----------------+  |
|  |                                                     |  |
|  |  +------------+       +--------------------------+  |  |
|  |  | Admin GUI  | <---> | MultiSeat Service        |  |  |
|  |  +------------+ IPC   +--------------------------+  |  |
|  |                             |                       |  |
|  |          +------------------+------------------+    |  |
|  |          |                  |                  |    |  |
|  |          v                  v                  v    |  |
|  |    Session Manager    Device Manager     Seat Manager|  |
|  |          |                  |                  |    |  |
|  |          v                  v                  v    |  |
|  |       Windows           Windows APIs        Seat DB |  |
|  |       Sessions          / HID / SetupAPI             |  |
|  +-----------------------------------------------------+  |
```

## Documentation

- [Design Specification](./DESIGN.md) — Complete technical design
- [Architecture](./docs/architecture.md) — High-level system design

## License

MIT
