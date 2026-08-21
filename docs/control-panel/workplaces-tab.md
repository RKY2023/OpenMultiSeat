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

This maps to OpenMultiSeat's **Seats** page. Seat management itself is now real: a data grid lists every configured `Seat` (name, Windows user, device/display counts, status, enabled), with working **Create Seat…** and **Delete Selected** actions bound to `ISeatManager`/`SeatPersistence`.

Device assignment (keyboard/mouse, plus camera/USB/Bluetooth as ownership records — see [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md)), display assignment, and audio assignment are all real and each still lives on its own page (**Devices**, **Displays**, **Audio**) — but the Seats page now also has a **"Configure…"** button (or double-click a seat row) opening `SeatDetailsWindow`: a single consolidated per-seat screen showing that seat's assigned devices/display/audio together, with inline assign/unassign for each, plus a shortcut into **User Account…**. That view is the closest OpenMultiSeat gets to ASTER's per-workplace column, but it's seat-centric, not ASTER's shared **System** pool.

A new **System** page (top-level nav, between Seats and Displays) is that shared pool: one grid listing every device, display, and audio endpoint across *every* seat at once — Kind/Name/Detail/Assigned To columns, with generic **"Assign Selected To…"**/**"Unassign Selected"** actions that open a seat picker and route to the same underlying `ISeatManager`/`IAudioManager` calls the type-specific pages use (device→`AssignDeviceToSeatAsync`/`AssignOtherDeviceToSeatAsync`, display→`AssignDisplayToSeatAsync`, audio→`AssignAudioDeviceToSeatAsync`, inferring type/role the same way each dedicated Assign window does). This is materially closer to ASTER's actual layout than `SeatDetailsWindow`: an admin can now see every *unassigned* resource across the whole machine in one place, not just one seat's assigned ones — though it's still a flat combined list rather than a grid.

A **"Tile Layout…"** button on the Seats page opens `WorkplaceTileLayoutWindow` — the actual ASTER-style visual layout the System page's flat list wasn't: one **System** column plus one column per seat, populated live from the same `ISeatManager`/`IDevicePersistence`/`IDisplayEnumerator`/`IAudioManager` data every other page reads. It's real, working drag-and-drop, not a mock-up:

- Dragging a device/display/audio tile onto a different column reassigns it. `TileAssignmentDecision` (Core, unit-tested) decides what that means — unassigned→a seat is a direct assign, a seat→a *different* seat shows the same `ConfirmDeviceDestinationWindow` prompt every other Assign flow uses before actually moving it, and a seat→**System** unassigns it. A device's Keyboard/Mouse/Other classification is read from *which list it's actually stored in* (`Seat.KeyboardIds`/`MouseIds`/`OtherDeviceIds`), not guessed from `DeviceRecord.DeviceType`, for already-assigned devices — the guess is only used as a fallback for still-unassigned ones, since trusting `DeviceType` for an already-assigned device caused a real bug (it could route an unassign call at the wrong list, duplicating the device across two seats instead of moving it).
- Each seat column's header carries a status dot colored from that seat's real `Seat.Status` (`Running`→green, `Configured`/`Starting`→orange, `Error`/`LoggedOff`→red) — genuine data, though nothing in this codebase currently updates `Seat.Status` from an actually-running process in real time (see [Known Issues](../known-issues.md)), so a dot reflects the last value written, not a live poll.
- **"Indicate device" is a different real mechanism per tile type**, not one uniform click:
  - **Keyboard/Mouse:** press the physical key or move the physical mouse — the window listens for Win32 Raw Input while it has focus and blinks whichever tile's device generated the event, correlated back through the same device-path→`StableId` lookup `HidDeviceEnumerator` uses at scan time.
  - **Display:** right-click → a large `workplace.display-index` label (e.g. `1.1`, `2.4`, computed by `DisplaySubNumbering`, Core, unit-tested) appears directly on that physical monitor for ~2.5 seconds, positioned from the display's real bounds — verified by looking at the actual screen, not a tile here.
  - **Audio:** no click needed — while the window is open, each endpoint's real peak level is polled continuously (`IAudioManager.GetPeakLevelAsync`, via NAudio's audio meter, the same source the Windows volume mixer's bars use) and its tile glows live whenever that endpoint is actually carrying sound.
  - **Camera:** right-click → a real ~6-second live video feed plays right inside the tile, matched to a DirectShow video input by friendly name (no shared stable ID with `GeneralDeviceEnumerator`'s WMI record the way keyboard/mouse have via raw input, so this is a name match, not guaranteed-unique).
  - **USB/Bluetooth (other "Other" devices):** right-click still blinks the tile's border in-app only — there's no live signal for these, so this is the one case where a GUI-only highlight (not a hardware signal) is the closest available.

What ASTER has that this doesn't: the blue "shared device" corner badges, the hot-plug "Configuration has changed" reconciliation banner, and icon-size/tile-distribution controls (columns are a fixed width, sized to fit the seat count rather than a resizable/redistributable canvas — see [Workplace Tab Settings](workplace-tab-settings.md)).

**User Account…** (assigning a Windows login to a seat, see [User Account for Workstation](user-account-for-workstation.md)) is real too, reachable from both the Seats page toolbar and `SeatDetailsWindow` — but it only configures the account; nothing yet reads it back to actually log the seat in.

No other per-seat context menu actions yet — rename seat, start/restart the seat's session via `OpenMultiSeat.Sessions`' `SessionManager`.

[Assign CPU Cores](assign-cpu-cores.md) — one item from ASTER's tab-wide hamburger menu above — is also implemented, reachable from the Settings page rather than from this tab (there's no tab-wide hamburger menu here yet).

Per-workplace IP-address assignment ([IP Address for the Workplace](ip-address-for-workplace.md)) remains planned but unscheduled, since OpenMultiSeat currently targets local seats on a single PC rather than networked workplaces.
