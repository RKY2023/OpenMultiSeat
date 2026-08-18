# Check for Updates

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_updateinfo

## What ASTER does

The "Check for Updates" window, reached from the About tab when ASTER is run as administrator, shows the installed version alongside the latest available version and its release notes, with a Download button to fetch the newest setup package. It also links out to the ASTER website for more information.

## OpenMultiSeat status

📋 **Planned** — no update-check mechanism exists yet. The GUI's About page today only reports the running version; there is no code that contacts any server, compares version numbers, or offers a download.

A real implementation would need:

- A lightweight version-check call (most naturally against the GitHub Releases API for `github.com/RKY2023/OpenMultiSeat`, since that's already the project's home and release artifacts already land there as NSIS installers) run on demand from an "Check for Updates" button on the About page, plus optionally a periodic background check owned by the Service.
- Comparison of the running GUI/Service version against the latest tagged release, surfaced as "Your version" / "Latest version" fields mirroring ASTER's layout.
- A link to the release's notes/changelog (e.g. the GitHub release body) in place of ASTER's "Version information" panel.
- A "Download" action that opens the release page or downloads the new `OpenMultiSeat-<version>-x64-setup.exe` for the user to run manually — OpenMultiSeat has no in-place auto-updater today, so this would launch the existing NSIS installer rather than patch files live.

Because OpenMultiSeat has no license server, this feature — if implemented — would be the *only* thing in the product that makes an outbound network call, which is why [Proxy Server Setup](proxy-server-setup.md) notes it as the one scenario a proxy configuration might eventually matter for.
