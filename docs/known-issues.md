# Known Issues

## GUI: Input Isolation and Audio pages are non-functional stubs

**Status:** open, tracked here. Devices, Seats, Displays, and Assign CPU Cores have since been wired to real backends (see below) — Input Isolation and Audio have not.

**Found by:** launching the built `OpenMultiSeat.GUI.exe` and screenshotting each nav page. Devices, Seats, Displays, and Assign CPU Cores were fixed in follow-up work; see their own status rows below rather than the original screenshots, which now describe a stale state for those.

### What's actually there today

The two remaining pages each render as: a page title, a one-line static description, and a single button whose *only* behavior is to pop a generic `MessageBox.Show("Configure <X>")` info dialog — the box literally repeats the button's own label back at the user and does nothing else. There is no data grid, no form, no binding to the backend managers that already exist for each of these areas (`InputIsolationService`, the audio manager in `OpenMultiSeat.Audio`).

Compare this to **Devices**, **Seats**, **Displays**, and **Assign CPU Cores**, which are genuinely wired up: Devices has a real `DataGrid` (Device Name, Hardware ID, Vendor ID, Product ID, Assigned To, First Seen columns) bound to `HidDeviceEnumerator`/`DevicePersistence` with working Scan/Refresh/Export Report/Assign to Seat/Unassign buttons; Seats has a real seat list with working Create/Delete, bound to `SeatManager`/`SeatPersistence`; Displays has a real scan (`DisplayEnumerator`, via the Windows Display Configuration APIs) with working Assign to Seat/Unassign; Assign CPU Cores (reachable from the Settings page) reads and writes real `Seat.CpuCoreAffinity` data via `ISeatPersistence`.

| Page | Real backend exists? | GUI wired to it? |
|---|---|---|
| Devices | ✅ `OpenMultiSeat.Devices` | ✅ Yes |
| Seats (list/create/delete) | ✅ `OpenMultiSeat.Core.SeatManager` | ✅ Yes |
| Displays | ✅ `OpenMultiSeat.Displays` | ✅ Yes |
| Assign CPU Cores | ✅ `OpenMultiSeat.Core.CpuAffinityProvider` | ✅ Yes |
| Devices → Seat assignment (keyboard/mouse) | ✅ `SeatManager.AssignDeviceToSeatAsync` | ✅ Yes — "Assign to Seat…"/"Unassign" on the Devices page |
| Displays → Seat assignment | ✅ `SeatManager.AssignDisplayToSeatAsync` | ✅ Yes — "Assign to Seat…"/"Unassign" on the Displays page |
| Seats → audio assignment | ✅ backend manager exists | ❌ Stub only — no UI to assign an audio endpoint to a seat yet |
| Input Isolation | ✅ `OpenMultiSeat.InputIsolation` | ❌ Stub only |
| Audio | ✅ `OpenMultiSeat.Audio` | ❌ Stub only |

Fixed alongside device assignment: `SeatManager`'s in-memory "already assigned elsewhere" guard (`_deviceToSeatMap`) was never rebuilt from persisted seat data — only populated by assignment calls made within the same `SeatManager` instance's own lifetime. Since the GUI constructs a fresh `SeatManager` per page load, this silently defeated the exclusivity check entirely; a device could be assigned to two seats at once with no error. `GetAllSeatsAsync` now rebuilds the map from each seat's `KeyboardIds`/`MouseIds` on every load. The same class of map (`_displayToSeatMap`) was added correctly from the start when display assignment was built. Both covered by `tests/OpenMultiSeat.Tests/SeatManagerTests.cs`.

**Caveat for all the "wired" pages, same as before:** they talk to `ISeatPersistence`/`IDevicePersistence` directly, not over `OpenMultiSeat.IPC` — there is still no GUI page with real IPC wiring to the Service. Displays additionally has no persistence layer at all (see [Assigning Video Outputs](control-panel/assigning-video-outputs.md) for what that means in practice).

Screenshots of the two still-broken pages (still accurate — nothing changed on these):

| Input Isolation | Audio |
|---|---|
| ![Input Isolation page stub](images/screenshots/input-isolation-page-stub.png) | ![Audio page stub](images/screenshots/audio-page-stub.png) |

The original Devices/Seats/Displays screenshots (`images/screenshots/devices-page-1.png`, `devices-page-2.png`, `seats-page-stub.png`, `displays-page-stub.png`) are kept in the repo for history but no longer embedded here — they show a state that's since been fixed.

### Where the code lives

- `src/OpenMultiSeat.GUI/Pages/SeatsPage.xaml(.cs)` — ✅ list/create/delete real
- `src/OpenMultiSeat.GUI/Pages/DisplaysPage.xaml(.cs)` — ✅ real
- `src/OpenMultiSeat.GUI/Windows/AssignCpuCoresWindow.xaml(.cs)` — ✅ real
- `src/OpenMultiSeat.GUI/Pages/InputPage.xaml(.cs)`
- `src/OpenMultiSeat.GUI/Pages/AudioPage.xaml(.cs)`

### What each remaining page needs to become real (summary — see the matching [control-panel](control-panel/README.md) pages for the ASTER-equivalent UX each should aim for)

- **Seats page — audio assignment** — device and display assignment are both done (see above); the remaining piece is audio endpoint assignment, equivalent to the audio-routing portion of ASTER's device/workplace assignment flow.
- **Reassignment ("move" a device/display between seats)** — today, moving an already-assigned device or display to a different seat means unassigning it first, then assigning it again; there's no single-step move or the before/after confirmation table ASTER's [Confirm Device Destination](control-panel/confirm-device-destination.md) window provides.
- **Input Isolation page** — a device list with per-seat binding, equivalent to ASTER's [Input Devices Switch](control-panel/input-devices-switch.md) window, bound to `InputIsolationService`.
- **Audio page** — a per-seat audio endpoint picker, bound to `OpenMultiSeat.Audio`'s enumerator (no direct ASTER equivalent window found in the wiki structure the docs are modeled on; ASTER handles this within its device/workplace assignment flow).
