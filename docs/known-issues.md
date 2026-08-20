# Known Issues

## GUI: Input Isolation page is a non-functional stub

**Status:** open, tracked here. Devices, Seats, Displays, Assign CPU Cores, and Audio have since been wired to real backends (see below) — Input Isolation has not.

**Found by:** launching the built `OpenMultiSeat.GUI.exe` and screenshotting each nav page. Devices, Seats, Displays, Assign CPU Cores, and Audio were fixed in follow-up work; see their own status rows below rather than the original screenshots, which now describe a stale state for those.

### What's actually there today

The one remaining page renders as: a page title, a one-line static description, and a single button whose *only* behavior is to pop a generic `MessageBox.Show("Configure <X>")` info dialog — the box literally repeats the button's own label back at the user and does nothing else. There is no data grid, no form, no binding to `InputIsolationService`, the backend that already exists for this area.

Compare this to **Devices**, **Seats**, **Displays**, **Assign CPU Cores**, and **Audio**, which are genuinely wired up: Devices has a real `DataGrid` (Device Name, Class, Hardware ID, Vendor ID, Product ID, Assigned To, First Seen columns) bound to `HidDeviceEnumerator`/`GeneralDeviceEnumerator`/`DevicePersistence` with working Scan/Refresh/Export Report/Assign to Seat/Unassign buttons — the Class column and `GeneralDeviceEnumerator` (a WMI `Win32_PnPEntity` scan for cameras, USB controllers/hubs, and Bluetooth devices/radios, shown for visibility alongside the Raw-Input-detected keyboards/mice) were added after a follow-up request for broader device coverage; Seats has a real seat list with working Create/Delete, bound to `SeatManager`/`SeatPersistence`; Displays has a real scan (`DisplayEnumerator`, via `EnumDisplayMonitors`) with working Assign to Seat/Unassign; Assign CPU Cores (reachable from the Settings page) reads and writes real `Seat.CpuCoreAffinity` data via `ISeatPersistence`; Audio has a real scan of Windows Core Audio endpoints (speakers, microphones, Bluetooth audio — anything Windows itself recognizes as a playback/recording device) with working Assign to Seat/Unassign.

Camera/USB/Bluetooth entries from `GeneralDeviceEnumerator` are now assignable too, as **ownership records** — `Seat.OtherDeviceIds` plus `SeatManager.AssignOtherDeviceToSeatAsync`/`UnassignOtherDeviceFromSeatAsync` (a separate pair from the Keyboard/Mouse-only `AssignDeviceToSeatAsync`, since these device classes don't fit `InputDeviceType`). The Devices page checks a row's class and routes "Assign to Seat…" to the matching dialog (`AssignDeviceToSeatWindow` for Keyboard/Mouse, `AssignOtherDeviceToSeatWindow` for Camera/Image/USB/Bluetooth) rather than letting an admin force-pick a nonsensical type. This gets the same exclusivity guarantee as every other assignment (`_otherDeviceToSeatMap`, rebuilt from persisted `OtherDeviceIds` the same way `_deviceToSeatMap`/`_displayToSeatMap` are), but — stated plainly in the dialog's own text — no routing/isolation effect: there's no InputIsolation-equivalent subsystem for arbitrary peripherals, so assigning a camera here is bookkeeping, not enforcement.

A new **System** page (top-level nav) lists every device/display/audio endpoint across *all* seats in one grid — the shared cross-seat pool view `SeatDetailsWindow` doesn't provide, since that one is per-seat. See [Workplaces Tab](control-panel/workplaces-tab.md) for how it maps to ASTER's own System-area concept.

Credential-based process launching is real and verified: `ISessionManager.LaunchProcessWithCredentialsAsync` wraps `CreateProcessWithLogonW` (the mechanism behind Explorer's "Run as different user"), reachable via a "Test Launch (Notepad)…" button on the User Account dialog. Verified against the real API — correct credentials launch and return a PID, a deliberately wrong password returns the exact expected Win32 error 1326. It is **not** wired to automatic seat start, and does **not** create a hardware-isolated session bound to a seat's own monitor/keyboard/mouse — see [User Account for Workstation](control-panel/user-account-for-workstation.md) for the full scope statement.

That same "not wired to automatic seat start" gap is now closed: the Settings page's new **Workplace Start Mode** (Manual / At System Startup / Via Workplace 1) registers real Windows Scheduled Tasks (`schtasks.exe`, via `WindowsStartupTriggerManager`) that invoke `OpenMultiSeat.GUI.exe --start-seats`, a genuinely headless codepath (routed through a hand-written `Program.Main`, not `App.OnStartup` — a blocking async call there deadlocks waiting on a WPF Dispatcher that hasn't started pumping yet, hit and fixed while building this) that calls `SeatStartupOrchestrator.StartAllSeatsAsync` to launch Explorer as every eligible seat's account via the same `CreateProcessWithLogonW` path. A **"Start Workplaces Now"** button runs the same orchestrator interactively, behind a confirm prompt.

Making At System Startup actually work (it runs as `SYSTEM`) required switching `SeatCredentialProtector` from DPAPI `CurrentUser` scope to `LocalMachine` scope — `CurrentUser`-scoped data is decryptable only by the exact account that encrypted it, which `SYSTEM` never is. **Disclosed trade-off, not a bug:** `LocalMachine` scope means any local account/process on the machine can decrypt a saved seat password, not just the account that set it — a real weakening of the protection boundary, accepted specifically so this mode can function; it's still far better than plaintext. Passwords saved under the old `CurrentUser` scope still decrypt transparently (`Unprotect` tries `LocalMachine` first, falls back to `CurrentUser`), so upgrading doesn't strand existing seat configurations. See [General Settings tab](control-panel/general-settings-tab.md) and [User Account for Workstation](control-panel/user-account-for-workstation.md) for the full reasoning.

| Page | Real backend exists? | GUI wired to it? |
|---|---|---|
| Devices | ✅ `OpenMultiSeat.Devices` | ✅ Yes |
| Seats (list/create/delete) | ✅ `OpenMultiSeat.Core.SeatManager` | ✅ Yes |
| Displays | ✅ `OpenMultiSeat.Displays` | ✅ Yes |
| Audio | ✅ `OpenMultiSeat.Audio` | ✅ Yes |
| Assign CPU Cores | ✅ `OpenMultiSeat.Core.CpuAffinityProvider` | ✅ Yes |
| Devices → Seat assignment (keyboard/mouse) | ✅ `SeatManager.AssignDeviceToSeatAsync` | ✅ Yes — "Assign to Seat…"/"Unassign" on the Devices page |
| Devices → Seat assignment (camera/USB/Bluetooth, ownership record only) | ✅ `SeatManager.AssignOtherDeviceToSeatAsync` | ✅ Yes — "Assign to Seat…"/"Unassign" on the Devices page |
| Displays → Seat assignment | ✅ `SeatManager.AssignDisplayToSeatAsync` | ✅ Yes — "Assign to Seat…"/"Unassign" on the Displays page |
| Audio → Seat assignment | ✅ `AudioManager.AssignAudioDeviceToSeatAsync` (pre-existing, was already correct) | ✅ Yes — "Assign to Seat…"/"Unassign" on the Audio page |
| Shared cross-seat System pool view | ✅ reuses existing managers | ✅ Yes — new **System** page |
| Credential-based process launch | ✅ `SessionManager.LaunchProcessWithCredentialsAsync` (`CreateProcessWithLogonW`), verified | ✅ Yes — "Test Launch…" on the User Account dialog; not wired to automatic seat start |
| Workplace Start Mode (Manual/At System Startup/Via Workplace 1) | ✅ `WindowsStartupTriggerManager` + `SeatStartupOrchestrator` | ✅ Yes — dropdown + "Start Workplaces Now" on the Settings page; seat passwords use DPAPI LocalMachine scope (disclosed trade-off, not a bug — see above) so SYSTEM can decrypt them |
| Input Isolation | ✅ `OpenMultiSeat.InputIsolation` | ❌ Stub only |

Fixed alongside device assignment: `SeatManager`'s in-memory "already assigned elsewhere" guard (`_deviceToSeatMap`) was never rebuilt from persisted seat data — only populated by assignment calls made within the same `SeatManager` instance's own lifetime. Since the GUI constructs a fresh `SeatManager` per page load, this silently defeated the exclusivity check entirely; a device could be assigned to two seats at once with no error. `GetAllSeatsAsync` now rebuilds the map from each seat's `KeyboardIds`/`MouseIds` on every load. The same class of map (`_displayToSeatMap`) was added correctly from the start when display assignment was built. Both covered by `tests/OpenMultiSeat.Tests/SeatManagerTests.cs`. `OpenMultiSeat.Audio`'s `AudioManager` did **not** have this bug — its duplicate-assignment guard is reloaded from persistence on every call, not just within one instance's lifetime, so it needed no equivalent fix.

Audio's enumeration piece (`AudioDeviceEnumerator`, previously a stub returning empty lists for both playback and recording) uses [NAudio](https://github.com/naudio/NAudio) rather than hand-rolled COM interop against `IMMDeviceEnumerator` — this session already hit two real, crash-causing P/Invoke struct-layout bugs writing raw Win32 interop by hand for Displays (see below), and COM interop has its own different sharp edges (vtable declarations, reference counting), so a well-tested existing wrapper was used instead of risking a third hand-rolled bug.

**Caveat for all the "wired" pages, same as before:** they talk to `ISeatPersistence`/`IDevicePersistence`/`IAudioPersistence` directly, not over `OpenMultiSeat.IPC` — there is still no GUI page with real IPC wiring to the Service. Displays additionally has no persistence layer at all (see [Assigning Video Outputs](control-panel/assigning-video-outputs.md) for what that means in practice).

**Displays required a second fix after the first one landed.** The original `DisplayEnumerator` used the newer `QueryDisplayConfig` Windows API; a P/Invoke struct-size mismatch there caused genuine heap corruption (`STATUS_HEAP_CORRUPTION`) on every call — fixed once, but `QueryDisplayConfig` then turned out to still return all-zeroed data even with the correct struct sizes, for reasons not fully root-caused. Rather than keep debugging that API, `DisplayEnumerator` was rewritten around the older, simpler `EnumDisplayMonitors`/`GetMonitorInfo`/`EnumDisplaySettings` GDI APIs, verified directly against a real machine. Trade-off: no connection/output type (HDMI/DP/etc.) — GDI doesn't expose that, only the DisplayConfig family does.

Screenshot of the one still-broken page (still accurate — nothing changed here):

![Input Isolation page stub](images/screenshots/input-isolation-page-stub.png)

The original Devices/Seats/Displays/Audio screenshots (`images/screenshots/devices-page-1.png`, `devices-page-2.png`, `seats-page-stub.png`, `displays-page-stub.png`, `audio-page-stub.png`) are kept in the repo for history but no longer embedded here — they show a state that's since been fixed.

### Where the code lives

- `src/OpenMultiSeat.GUI/Pages/SeatsPage.xaml(.cs)` — ✅ list/create/delete real
- `src/OpenMultiSeat.GUI/Pages/DisplaysPage.xaml(.cs)` — ✅ real
- `src/OpenMultiSeat.GUI/Pages/AudioPage.xaml(.cs)` — ✅ real
- `src/OpenMultiSeat.GUI/Windows/AssignCpuCoresWindow.xaml(.cs)` — ✅ real
- `src/OpenMultiSeat.GUI/Pages/SystemPage.xaml(.cs)` — ✅ real, shared cross-seat pool view
- `src/OpenMultiSeat.GUI/Windows/AssignOtherDeviceToSeatWindow.xaml(.cs)` — ✅ real, camera/USB/Bluetooth ownership-record assignment
- `src/OpenMultiSeat.Sessions/SessionManager.cs` (`LaunchProcessWithCredentialsAsync`) — ✅ real, verified via `CreateProcessWithLogonW`
- `src/OpenMultiSeat.Sessions/SeatStartupOrchestrator.cs` — ✅ real, starts every eligible seat via `LaunchProcessWithCredentialsAsync`
- `src/OpenMultiSeat.Sessions/WindowsStartupTriggerManager.cs` — ✅ real, registers/removes the `schtasks.exe` Scheduled Tasks
- `src/OpenMultiSeat.Core/GeneralSettings.cs`, `GeneralSettingsPersistence.cs` — ✅ real, persists `SeatStartMode`
- `src/OpenMultiSeat.Core/SeatCredentialProtector.cs` — ✅ real, DPAPI `LocalMachine` scope (switched from `CurrentUser`; old-scope data still reads back)
- `src/OpenMultiSeat.GUI/Program.cs`, `App.xaml.cs` (`--start-seats`) — ✅ real, headless entry point the Scheduled Tasks invoke
- `src/OpenMultiSeat.GUI/Pages/SettingsPage.xaml(.cs)` — ✅ real, Workplace Start Mode dropdown + "Start Workplaces Now"
- `src/OpenMultiSeat.GUI/Pages/InputPage.xaml(.cs)`

### What the remaining page needs to become real (see the matching [control-panel](control-panel/README.md) page for the ASTER-equivalent UX it should aim for)

- **Input Isolation page** — a device list with per-seat binding, equivalent to ASTER's [Input Devices Switch](control-panel/input-devices-switch.md) window, bound to `InputIsolationService`.
- **Reassignment ("move" a device/display/audio endpoint between seats)** — today, moving an already-assigned resource to a different seat means unassigning it first, then assigning it again; there's no single-step move or the before/after confirmation table ASTER's [Confirm Device Destination](control-panel/confirm-device-destination.md) window provides.
