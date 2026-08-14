# OpenMultiSeat

**Version:** 0.1  
**Date:** 2026-08-14  
**Target Platform:** Windows 11 x64

Open-source Windows multi-seat system — run two or more independent local workstations on a single physical PC using one Windows installation, with dedicated monitors, keyboards, mice, audio devices, and user sessions.

## Project Status

**Phase:** Bootstrap (Scaffolding & Architecture)  
**Current Milestone:** Project structure and design documentation setup

See [DESIGN.md](./DESIGN.md) for the complete technical specification.

## Quick Start

### Prerequisites

- Windows 11 x64
- .NET 9 SDK or later
- Visual Studio 2022 (or VS Code with C# Dev Kit)

### Building

```bash
dotnet build OpenMultiSeat.sln
```

### Running Tests

```bash
dotnet test OpenMultiSeat.sln
```

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
