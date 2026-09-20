# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

> Current build details, artifact locations, and verification status live in
> [RELEASE_NOTES.md](RELEASE_NOTES.md).

### Added
- Phase 0: Hardware Discovery POC
  - Input device enumeration (keyboard/mouse) via Raw Input API
  - Display enumeration with resolution/refresh/position via Display Configuration API
  - Windows session enumeration via WTS API
  - Phase0.Poc console application for testing
- Admin GUI (Phase 7): every page wired to a real backend — Devices, Seats, Displays,
  Audio, System, Input Isolation, Settings, Assign CPU Cores
- ASTER-style Tile Layout window with drag-and-drop reassignment and per-type
  "Indicate device" (raw-input blink, on-monitor overlay, live audio meter, camera preview)
- Workplace Start Mode (Manual / At System Startup / Via Workplace 1), backed by real
  Windows Scheduled Tasks, plus a manual "Start Workplaces Now" action
- MSI installer (Phase 8): `installer/Product.wxs`, built with WiX Toolset v5 —
  per-machine install, Start Menu and Desktop shortcuts, upgrade support
- Modern UI theme across the whole admin console

### In Development
- Phase 3: Session Management — process launching works (`CreateProcessWithLogonW`),
  but genuinely isolated per-seat desktop sessions do not (see Known Issues)
- Phase 5: Input Isolation — GUI and service wiring exist; real enforcement unconfirmed
- Phase 9: Reliability (Recovery, robustness)

## [0.1.0-alpha] - 2026-08-14

### Added
- Initial project structure
- 8 C# projects (Core, Devices, Displays, Sessions, IPC, Audio, Service, GUI)
- Test project framework
- GitHub Actions CI/CD pipelines
- Security policy and issue templates
- Development documentation

### Infrastructure
- GPG-signed commits enforcement
- Branch protection on master
- Dependency vulnerability scanning
- Code quality gates
- Automated release workflow

## Version Numbering

- **0.1.x-dev**: Phase 0 development (hardware discovery POC)
- **0.1.0-alpha**: First public alpha release (Phase 0 complete)
- **0.2.0-beta**: Phase 1-2 complete (device discovery, seat configuration)
- **0.5.0-rc**: Phase 5 complete (input isolation - critical milestone)
- **1.0.0**: Phase 9 complete (production ready)

## Project Phases

Each phase is tracked as a separate development cycle:

1. **Phase 0** - Feasibility (Hardware enumeration)
2. **Phase 1** - Device Discovery (Stable IDs)
3. **Phase 2** - Seat Configuration
4. **Phase 3** - Session Management
5. **Phase 4** - Display Management
6. **Phase 5** - Input Isolation (Critical)
7. **Phase 6** - Audio
8. **Phase 7** - Admin GUI
9. **Phase 8** - Installer
10. **Phase 9** - Reliability & Security Review

## Security Notice

Before version 1.0.0, please note:
- No input isolation yet (Phase 5 not complete)
- No kernel drivers signed (Phase 8)
- Early development phase - not recommended for production
- Security review scheduled for Phase 9

See [SECURITY.md](SECURITY.md) for more details.

## Commit Convention

All commits follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation
- `test:` - Tests
- `refactor:` - Code refactoring
- `chore:` - Build, CI, tooling

Example: `feat: Phase 0 hardware discovery implementation`

## Release Process

1. Create feature branches from `develop`
2. Open PR to `master` when ready
3. PR requires review + all checks passing
4. Merge to `master` with signed commit
5. Tag release with `git tag -s v0.1.0-alpha`
6. Push tag triggers GitHub Actions release workflow
7. Release notes auto-generated from commits

## Supported Platforms

- **OS**: Windows 11 x64
- **.NET**: 9.0+
- **IDEs**: Visual Studio 2022, VS Code
- **Architecture**: x64 only

## Known Issues

See [docs/known-issues.md](docs/known-issues.md) for the detailed, per-feature status.
The headline items:

- **No true multi-seat sessions yet.** Seats are launched with `CreateProcessWithLogonW`
  ("run as a different user"), which does *not* create a hardware-isolated desktop session
  bound to a specific seat's monitor/keyboard/mouse. Real simultaneous multi-seat login
  needs RDS/MultiPoint-style session support or a Winlogon credential provider — neither exists here.
- Input isolation is wired end-to-end in the GUI, but real enforcement is unconfirmed (Phase 5)
- Keyboard/mouse "Indicate device" is still unconfirmed after three rounds of fixes;
  a live diagnostic now reports which stage fails
- Nothing is code-signed — neither the exe nor the MSI, and no kernel drivers exist
- No GUI page talks to the Service over IPC; pages use the persistence layer directly
- Displays have no persistence layer

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Repository**: https://github.com/RKY2023/OpenMultiSeat  
**Documentation**: https://github.com/RKY2023/OpenMultiSeat/tree/master/docs  
**Issues**: https://github.com/RKY2023/OpenMultiSeat/issues
