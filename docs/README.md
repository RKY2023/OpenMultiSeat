# OpenMultiSeat Documentation

## Product & Design

- [Architecture](architecture.md) — components, IPC, data flow
- [Development Phases](phases.md)
- [Phase 0 Implementation Notes](phase0-implementation.md)
- [Known Issues](known-issues.md) — tracked gaps, including the current Seats/Displays/Input Isolation/Audio GUI stubs

## Diagrams

- [BPMN Workflows](diagrams/bpmn-workflows.md) — install → configure → start-seat, and the device-reconnect flow
- [UML Class Diagram](diagrams/uml-class-diagram.md) — Core domain model and manager classes
- [Sequence Diagrams](diagrams/sequence-diagrams.md) — device scan, seat start, session-launch message flows
- [Data Flow Diagram (DFD)](diagrams/dfd.md) — context diagram + Service-level data flow

## ASTER-parity documentation

OpenMultiSeat is a free, open-source replacement for [ASTER Multiseat Software](https://dokwiki.ibiksoft.com) (a commercial product on a 14-day trial). These two sections mirror ASTER's own wiki structure, page for page, describing OpenMultiSeat's equivalent (or planned, or not-applicable) feature for each one:

- [Program Interface "OpenMultiSeat Control Panel"](control-panel/README.md) — every window/dialog/tab
- [User Manual](user-manual/README.md) — install, setup, licensing (there isn't any), support, FAQ, requirements
- [Market Positioning](market-positioning.md) — the use-case verticals (home, education, business, industrial), condensed from ASTER's 30+ marketing articles into one positioning summary

## Installation & Operations

- [Installation Guide](INSTALLATION_GUIDE.md) *(on branches that include the NSIS installer work)*
- [Reliability Guide](RELIABILITY_GUIDE.md) *(on branches that include Phase 9)*
