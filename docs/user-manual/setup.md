# Setting Up OpenMultiSeat

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_config

## ASTER's approach

ASTER's "Setting up ASTER" guide walks through a fairly involved configuration flow:

- **Preparing to set up** — settings are held in memory until "Apply" is clicked, some changes need a full restart, and the app minimizes to the system tray rather than closing. Configuration is split across a "System" menu (general settings) and a "Workplaces" menu (per-workstation settings).
- **Indicating devices** — because a single PC may have many attached monitors, keyboards, and mice, ASTER provides tooltips, colored activity outlines, an explicit "Device Indication" feature (flashing a monitor number or beeping a speaker), and flashing red frames for newly connected hardware, all so the administrator can tell physical devices apart on screen.
- **Assigning monitors** — monitors are dragged onto workplace slots (or auto-resolved on conflict); every monitor initially belongs to the first workplace until reassigned.
- **Starting workplaces** — manually, at system boot, or at first user logon.
- **Assigning keyboard/mouse** — drag-and-drop or right-click assignment, with an emergency `Ctrl+F12` reset that returns all input devices to the controlling workplace.
- **Assigning audio devices** — sound cards/devices can be dedicated to one workplace or shared.
- **Assigning USB hubs** — assigning a whole hub reassigns everything plugged into it, with automatic restrictions on optical drives/removable disks.
- **Assigning IP addresses** — optional per-workplace network identity for apps that need it.
- **Useful settings** — hardware vs. software cursor selection, automatic per-workstation Windows logon.
- **Configuring a proxy server** — needed only to reach ASTER's license activation and support servers from behind a restrictive corporate firewall (see the dedicated proxy page).
- **Troubleshooting and update checks** — safe-mode recovery, disabling ASTER before driver updates, and a manual "check for new version" action on the About tab (which talks to ASTER's license server).

## OpenMultiSeat's approach

OpenMultiSeat's setup flow is deliberately simpler and maps onto the same physical-device-assignment problem ASTER solves, without any of the licensing-related steps:

1. **Install.** Run the NSIS-based installer. It requires a 64-bit Windows machine and administrator rights (the installer enforces both and will refuse to proceed otherwise). The installer sets up the privileged Windows Service and the unprivileged WPF Admin GUI, both talking over a local Named Pipe.
2. **Open the Admin Console.** Launch the OpenMultiSeat GUI. It presents Dashboard, Devices, Seats, Displays, Input Isolation, Audio, Settings, and About pages.
3. **Scan hardware on the Devices page.** This page is fully implemented today: it enumerates connected keyboards, mice, and other HID devices via Windows' SetupAPI/Raw Input, and shows them in a live grid with **Scan**, **Refresh**, and **Export** actions — this is the direct equivalent of ASTER's "Indicating Devices" step, letting you confirm which physical device is which before assigning it to a seat.
4. **Assign devices to seats.** This is the equivalent of ASTER's "Assigning Monitors," "Assigning Keyboard and Mouse," and "Assigning Audio Devices" steps, and it will live on the **Seats**, **Displays**, and **Audio** pages of the Admin Console. Today those pages are stub placeholders (a single button that opens a generic informational popup) — the underlying enumeration logic exists (Displays and Audio projects can already detect hardware) but the assignment UI itself is still being built. See [`known-issues.md`](../known-issues.md) for current status.
5. **No proxy step, no activation step, no update-check-against-a-license-server step.** OpenMultiSeat makes no license or telemetry network calls at all, so there is nothing in the setup flow analogous to ASTER's proxy configuration or "check for new version" against a license server. See [`proxy-for-corporate-clients.md`](proxy-for-corporate-clients.md) for details.
6. **USB hubs and IP address assignment.** OpenMultiSeat does not yet have a dedicated USB-hub cascading assignment feature or a per-seat IP address restriction feature analogous to ASTER's; individual devices are enumerated and assigned directly. If your setup fans keyboards/mice out through a hub, plan on assigning each device individually for now.

Once the Seats/Displays/Input Isolation/Audio pages move out of stub status, this page will be updated with the concrete click-by-click assignment steps for each.
