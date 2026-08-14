# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Phase 0: Hardware Discovery POC
  - Input device enumeration (keyboard/mouse) via Raw Input API
  - Display enumeration with resolution/refresh/position via Display Configuration API
  - Windows session enumeration via WTS API
  - Phase0.Poc console application for testing

### In Development
- Phase 1: Device Discovery (Stable IDs, VID/PID, serial numbers)
- Phase 2: Seat Configuration (Data model, validation, persistence)
- Phase 3: Session Management (Lifecycle, process launching)
- Phase 4: Display Management (Assignment, topology)
- Phase 5: Input Isolation (Critical: keyboard/mouse routing)
- Phase 6: Audio (Device routing)
- Phase 7: Admin GUI (WPF console)
- Phase 8: Installer (MSI package)
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

- Input isolation not yet implemented (Phase 5)
- Display routing not yet implemented (Phase 4)
- Audio not yet enumerated (Phase 6)
- GUI not yet implemented (Phase 7)
- No kernel driver signing (Phase 8)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Repository**: https://github.com/RKY2023/OpenMultiSeat  
**Documentation**: https://github.com/RKY2023/OpenMultiSeat/tree/master/docs  
**Issues**: https://github.com/RKY2023/OpenMultiSeat/issues
