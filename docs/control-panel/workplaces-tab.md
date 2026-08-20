# "Workplaces" Tab

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform_tabcontrol_tabterminals

## What ASTER does

The Workplaces tab is where hardware gets assigned to seats. A **System** area on the left lists PC devices not yet assigned to any workplace, plus devices shared across all workplaces; a context menu on a device slot there offers **Workplace appointment…** (assign it to a seat), **Indicate device** (flash/highlight it to identify which physical device it is), **Assign video outputs…** (for displays — see [Assigning Video Outputs](assigning-video-outputs.md)), **Info**, and **Set custom icon**.

To the right, one numbered column per configured workplace (`1`, `2`, `3`, …) shows that workplace's assigned displays, keyboards, and mice. Each column header carries a colored status dot — green when the workplace is currently launched/running, orange when it's configured but not started, red when it has a configuration problem — and a hamburger (☰) menu with **Edit name**, **Edit user login** (see [User Account for Workstation](user-account-for-workstation.md)), **Assign IP address** (see [IP Address for the Workplace](ip-address-for-workplace.md)), and **Force relogin**. Displays within a column are labeled `workplace.index` (e.g. `1.1`, `1.4`) when a workplace has more than one monitor.

A separate hamburger menu next to the **System** column header opens workplace-tab-wide settings and tools:

| Menu item | Documented at |
|---|---|
| Workplaces tab settings | [Workplace Tab Settings](workplace-tab-settings.md) |
| User accounts | [User Account for Workstation](user-account-for-workstation.md) (per-seat); this entry opens a list of all configured accounts |
| Display settings | opens Windows' own Display settings applet |
| Network connections | opens Windows' own Network Connections applet |
| Device manager | opens Windows' own Device Manager |
| Assign CPU cores | [Assign CPU Cores](assign-cpu-cores.md) |
| Keyboard/mice switch hotkey | [Input Devices Switch](input-devices-switch.md) |
| Experimental settings | [Experimental Settings](experimental-settings.md) |
| Cleanup license | [Cleanup License](cleanup-license.md) |
| Special settings | a gated, support-only advanced panel exposing ~25 internal tuning flags (compatibility/hardware-scheduling toggles like display-mode workarounds and boot-behavior overrides) behind a warning that they should only be changed on ASTER support's advice; not itemized here since these are ASTER-internal flags with no direct OpenMultiSeat design implication |
| Set log verbosity | [Set Log Verbosity](log-verbosity.md) |
| Reset settings | [Reset Settings](reset-settings.md) |
| Check configuration | [Check Configuration](check-configuration.md) |

If a display or device is hot-plugged while ASTER is running, a **"Configuration has changed"** banner appears at the bottom of the tab with **Yes** (accept the change into the current layout), **Move to unused** (park the new device/display in the System area instead), and **Close**.

## OpenMultiSeat status

**🚧 Partially implemented** — see [Known Issues](../known-issues.md).

This maps to OpenMultiSeat's **Seats** page. Seat management itself is now real: a data grid lists every configured `Seat` (name, Windows user, device/display counts, status, enabled), with working **Create Seat…** and **Delete Selected** actions bound to `ISeatManager`/`SeatPersistence`. What's still missing is everything ASTER's Workplaces tab uses a seat list *for* — assigning hardware to a seat:

- No unassigned/common-devices device pool or per-seat assign/unassign controls yet, even though the backend it needs (`IDevicePersistence`, `HidDeviceEnumerator`, `SeatManager.AssignDeviceToSeatAsync`) already exists and is exercised correctly by the Devices page.
- No display/audio assignment UI (`IDisplayEnumerator`/`DisplayEnumerator`, `AudioDevice`) wiring into a seat.
- No per-seat context menu actions yet — rename seat, assign a Windows user (see [User Account for Workstation](user-account-for-workstation.md)), start/restart the seat's session via `OpenMultiSeat.Sessions`' `SessionManager`.

[Assign CPU Cores](assign-cpu-cores.md) — one item from ASTER's tab-wide hamburger menu above — is also implemented, reachable from the Settings page rather than from this tab (there's no tab-wide hamburger menu here yet).

Per-workplace IP-address assignment ([IP Address for the Workplace](ip-address-for-workplace.md)) remains planned but unscheduled, since OpenMultiSeat currently targets local seats on a single PC rather than networked workplaces.
