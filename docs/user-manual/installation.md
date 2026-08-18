# Installation

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_installation

## ASTER's approach

ASTER installs from a downloaded setup package: the user picks a language, accepts the EULA, chooses an install folder, and is expected to check a "Configuring Windows settings" box that lets the installer disable Memory Integrity and Fast Boot, enable debug logging, switch to the "High Performance" power plan, prevent monitors/USB devices from sleeping, add a custom screen saver, and create Windows Defender/Firewall exceptions. If ASTER is enabled during setup, a reboot is required before further configuration is possible. Uninstallation is done through Windows' standard "Remove Programs" tooling rather than manual file deletion.

## OpenMultiSeat's approach

OpenMultiSeat installs from a single NSIS-based Windows installer, `OpenMultiSeat-1.0.0-x64-setup.exe`, built as a framework-dependent .NET 9 deployment. Requirements are straightforward:

- 64-bit Windows.
- Administrator rights — the installer requests elevation up front, since it writes to `Program Files` and to `HKLM`.
- .NET 9 runtime present (framework-dependent deployment; not bundled in the installer).

During setup you choose which components to install, each independently selectable:

- **Core Components** — the OpenMultiSeat Windows Service, which does the actual device/display/session/audio coordination.
- **GUI Application** — the WPF "OpenMultiSeat Administration Console" used to configure and monitor seats.
- **Testing Tools** — diagnostic/test utilities.
- **Documentation** — this documentation set, installed locally.

There's no license screen, no activation code entry, and no trial-vs-paid branching anywhere in the installer — OpenMultiSeat has no licensing system at all (see [About OpenMultiSeat](about-openmultiseat.md)), so setup is just: run the installer, accept elevation, pick components, finish. Unlike ASTER's installer, OpenMultiSeat's does not currently modify Windows power settings, Memory Integrity, Fast Boot, or screen-saver configuration on your behalf; any such tuning is left to the administrator to apply manually if their hardware needs it.

After installation, the Service starts as a normal Windows Service and the Administration Console can be launched from the Start Menu shortcut the installer creates. Uninstallation is done the standard way, through *Settings → Apps* (or Control Panel's "Programs and Features") — not by deleting files manually.

For the complete, authoritative step-by-step walkthrough — including screenshots and troubleshooting — see `docs\INSTALLATION_GUIDE.md` in the repository. That guide takes precedence over this summary if the two ever disagree.
