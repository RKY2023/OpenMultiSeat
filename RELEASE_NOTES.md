# OpenMultiSeat — Build & Release Notes

**Build date:** 2026-08-31
**Branch:** `feature/cpu-core-affinity`
**Head commit:** `c9bd8de`
**Target platform:** Windows 10/11 x64

This file consolidates everything about the current build: what the shipped artifacts are, where they live, what changed, what's been verified, and how to rebuild. For the full engineering detail behind each item — including what's still stubbed or unverified — see [docs/known-issues.md](docs/known-issues.md).

---

## 1. Artifacts

Both are built from the same self-contained, single-file publish output. Neither needs a separate .NET runtime installed on the target machine.

| File | Size | Location | Use it when |
|---|---|---|---|
| `OpenMultiSeat.GUI.exe` | 71.4 MB | `D:\Kidbea\IT Team\OpenMultiSeat\OpenMultiSeat.GUI.exe` | You just want to run the admin console — copy anywhere, double-click, nothing to install |
| `OpenMultiSeat-Setup.msi` | 65.5 MB | `D:\Kidbea\IT Team\OpenMultiSeat\OpenMultiSeat-Setup.msi` | You want a real install: Program Files, Start Menu + Desktop shortcuts, clean uninstall |

Both artifacts are deliberately **not** committed to git (`publish/` is gitignored — they're large binaries, not source). The installer's *source* (`installer/Product.wxs`, `installer/License.rtf`) **is** committed, so the MSI can be rebuilt at any time.

### MSI package identity

| Property | Value |
|---|---|
| ProductName | OpenMultiSeat |
| ProductVersion | 1.0.0 |
| Manufacturer | OpenMultiSeat Contributors |
| ProductCode | `{1E982E42-7333-48A2-8EB9-45EDFA5F0E1F}` |
| UpgradeCode | `{6B382A7B-C748-48A9-B36E-6B91F45C0E65}` (fixed forever — never change it) |
| Install scope | Per-machine (`C:\Program Files\OpenMultiSeat`), prompts for UAC |

> **Version-number caveat:** the MSI declares `1.0.0` because Windows Installer requires a version, but [CHANGELOG.md](CHANGELOG.md)'s own scheme reserves `1.0.0` for "Phase 9 complete / production ready," which this is not. Worth aligning on a real version (e.g. `0.7.0`) before any public release — the `UpgradeCode` stays fixed either way, so renumbering now costs nothing.

---

## 2. Installing and running

### Option A — standalone exe (no install)
Copy `OpenMultiSeat.GUI.exe` anywhere and double-click it. Nothing is installed, registered, or written outside `%AppData%\OpenMultiSeat\`.

### Option B — MSI installer
1. Double-click `OpenMultiSeat-Setup.msi` → Windows prompts for Administrator (UAC), because it installs per-machine.
2. Accept the MIT license, optionally change the install folder (default `C:\Program Files\OpenMultiSeat`), click Install.
3. Installs one file (`OpenMultiSeat.GUI.exe`) plus a **Start Menu** shortcut and a **Desktop** shortcut, both named "OpenMultiSeat".

There are no component checkboxes and no `tools\` folder of phase testers — the single GUI exe is the whole product today.

### Uninstalling
- Installed via MSI → Settings → Apps → Apps & features → "OpenMultiSeat" → Uninstall (also removes both shortcuts).
- Ran the standalone exe only → nothing to uninstall; delete the file.

Either way, `%AppData%\OpenMultiSeat\` (seat/device configuration, settings, startup log) is intentionally left in place.

### Administrator: what needs it, and what doesn't
Running the app normally does **not** require Administrator. Two things do:
- **Installing the MSI** (per-machine, writes to Program Files) — Windows prompts automatically.
- **Changing "Workplace Start Mode"** on the Settings page — it registers/removes a Windows Scheduled Task, including when switching *back* to Manual. The app now detects this itself and offers a one-click elevated relaunch (see §3).

---

## 3. What changed in this build

### New: real MSI installer (`c9bd8de`)
Built from `installer/Product.wxs` with WiX Toolset v5. Supports `MajorUpgrade`, so a future MSI with a higher version and the same `UpgradeCode` replaces this install automatically instead of going side-by-side.

**WiX is pinned to 5.0.2 deliberately.** WiX v6/v7 require accepting a separate paid-use EULA (the "Open Source Maintenance Fee") just to add the UI extension this build needs. v5.0.2 predates that requirement and needs no EULA acceptance.

### Fixed: Workplace Start Mode was a dead end for Administrator (`9e3f653`)
Reported as *"problem with administrator with run 2 workplace at startup option."* Two distinct bugs:

1. **The dead end.** Picking "Automatically at system startup" (or "Via Workplace 1") while not already elevated failed with an error telling you to go run the app as Administrator yourself, then reverted the dropdown — with no way to act on it short of quitting, relaunching elevated, and reselecting the same option.
2. **A silent correctness bug behind it.** Switching an already-registered automatic mode *back to Manual* while not elevated silently "succeeded": `settings.json` flipped to Manual and the GUI reported success, but the actual SYSTEM-owned scheduled task was left behind untouched and **would still fire at the next boot**. This happened because the delete-task step has to swallow its own errors (a *missing* task also looks like a failure to `schtasks`).

**Fix:** elevation is now checked up front (`ElevationHelper`) *before* touching `schtasks.exe` at all, for every mode change including back to Manual — closing the silent-failure gap. And the dead end is gone: the Settings page now offers a UAC prompt to relaunch elevated, carrying the chosen mode on the command line (`--apply-start-mode=<mode>`), then applies it automatically once the elevated instance opens. You pick the mode **once**, not once before the prompt and again after. Declining the UAC prompt just reverts the dropdown.

### Fixed: dialog buttons cropped off-screen, in 9 windows (`fda6005`)
Reported against "Workplace Tab Settings," where the Save/Cancel buttons were pushed below the visible window with no scrollbar and no way to resize. An audit found the **identical bug in 9 dialogs** — all used a fixed pixel height + `ResizeMode="NoResize"` + no scroll fallback, so any content needing more vertical room than the hardcoded height (longer text, DPI/font scaling, conditional panels) silently pushed the button row out of reach:

`WorkplaceTabSettingsWindow`, `CreateSeatWindow`, `AssignDeviceToSeatWindow`, `AssignDisplayToSeatWindow`, `AssignAudioDeviceToSeatWindow`, `AssignOtherDeviceToSeatWindow`, `UserAccountWindow`, `SeatPickerWindow`, `ConfirmDeviceDestinationWindow`.

All nine were restructured to the safe pattern two other windows already used correctly — title fixed at top, content scrolls in the middle, **buttons fixed at the bottom where they can't be pushed away** — and made user-resizable as a second line of defense.

### Fixed: Tile Layout drop area too small (`fda6005`)
Seat columns were only as tall as their tiles, so the visually-empty space below them wasn't actually part of the drop target — making it easy to miss when dragging onto a sparsely-populated seat. Columns now stretch to fill the visible viewport height, re-applied on every resize, so the whole visible column accepts a drop.

### Fixed: `RawInputDeviceType` enum had wrong values (`c892f03`)
A genuine, long-standing bug found during investigation: the enum declared `{ Mouse = 1, Keyboard = 2, Hid = 4 }` with a `[Flags]` attribute, but the real Win32 constants are `RIM_TYPEMOUSE=0, RIM_TYPEKEYBOARD=1, RIM_TYPEHID=2` — mutually exclusive tags, not a bitmask. Effect: real mice fell through to "Other," real keyboards were saved as mice, and generic HID collections were saved as keyboards. This is very likely the actual root cause of the "`DeviceRecord.DeviceType` is unreliable" problem that had been worked around in three separate places as if it were an inherent Windows limitation.

**Note:** already-persisted device records aren't retroactively corrected — run **Devices → Scan Devices** once to pick up the fix for currently-connected hardware.

### Added: live raw-input diagnostic in Tile Layout (`c892f03`)
Keyboard/mouse "Indicate device" still hadn't been confirmed working after two rounds of fixes. Rather than guess a third time, the Tile Layout window now shows a live one-line diagnostic that updates on every raw-input event, naming exactly which stage it reached (registration → device resolution → hardware ID → device lookup → tile lookup → "blinking now"). **This is the key open item — see §4.**

### Earlier in this series
- `90d2e52` — keyboard/mouse Indicate: registered with `RIDEV_INPUTSINK` so raw input is delivered while the window is open rather than only while it's the foreground window.
- `c38cdd7` — modern UI theme across the whole admin console (design-token palette + implicit control styles, dark nav rail, restyled grids/inputs/scrollbars).
- `c2a4ae6` — live camera preview for camera tiles; keyboard/mouse raw-input P/Invoke marshaling fix.

---

## 4. Verification status — read this before trusting anything below

| Item | Status |
|---|---|
| `OpenMultiSeat.GUI.csproj` Release build | ✅ Clean (pre-existing warnings only) |
| Unit tests | ✅ 54/54 passing |
| MSI is a well-formed package | ✅ Opened via the Windows Installer COM API; identity properties confirmed |
| Display "Identify" overlay, audio level meter | ✅ Confirmed working by you |
| **MSI install / uninstall actually run** | ❌ **Not done** — a real per-machine install changes system state, left for you |
| **Keyboard/Mouse "Indicate device"** | ❌ **Still unconfirmed** — diagnostic added to pinpoint it |
| Dialog-cropping fixes (9 windows) | ⏳ Built and shipped, not yet retested by you |
| Tile Layout drop-area fix | ⏳ Built and shipped, not yet retested by you |
| Workplace Start Mode UAC relaunch | ⏳ Built and shipped, not yet retested by you |
| Camera preview | ⏳ Pending first verification |

**Why so much is unverified:** GUI and hardware-interop behavior in this project is verified by build + tests + code review only. This session runs on your live desktop, and after an earlier simulated mouse click landed on the wrong window, all further live-desktop UI automation was stopped. Anything requiring a real key press, mouse move, drag, UAC click, or installer run needs you.

### The one open question
Open **Seats → Tile Layout…**, press a key / move the mouse, and tell me what the diagnostic line at the top of the window says. That single line identifies exactly which stage of the raw-input pipeline is failing, and resolves the keyboard/mouse Indicate issue for good — it's been through three rounds of individually-correct fixes that each turned out to be insufficient.

---

## 5. Rebuilding

### The exe
```
dotnet publish src\OpenMultiSeat.GUI\OpenMultiSeat.GUI.csproj -c Release -r win-x64 ^
    --self-contained true -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true ^
    -o publish\OpenMultiSeat.GUI
```
Kill any running instance first (`Get-Process OpenMultiSeat.GUI | Stop-Process -Force`) — the publish fails if the exe is locked.

### The MSI
```
dotnet tool install --global wix --version 5.0.2
wix extension add WixToolset.UI.wixext/5.0.2
wix build installer\Product.wxs -ext WixToolset.UI.wixext -arch x64 ^
    -d SourceDir=publish\OpenMultiSeat.GUI -b installer ^
    -o publish\OpenMultiSeat-Setup.msi
```
Publish the exe first — the MSI packages that output.

### Building and testing
```
dotnet build src\OpenMultiSeat.GUI\OpenMultiSeat.GUI.csproj -c Release
dotnet test tests\OpenMultiSeat.Tests\OpenMultiSeat.Tests.csproj -c Release
```

> **Known build snag, unrelated to any of this:** a whole-solution `dotnet build` currently fails in `scripts\Phase0.Poc.csproj` with an `MSB4018` / `GenerateDepsFile` error, caused by a stale missing `System.Management.dll` runtime asset under that project's own `bin\` folder. It predates this work and doesn't affect the GUI or the tests — build those two projects directly, as above.

---

## 6. Not signed

Neither the exe nor the MSI is code-signed (no certificate available). SmartScreen will warn on first run/install, and the publisher shows as unknown in the UAC prompt. Getting an Authenticode certificate and signing both is worth doing before distributing this to anyone else's machine.

---

## 7. Where to look next

- [docs/known-issues.md](docs/known-issues.md) — the honest, detailed status of every page and feature, including which bugs were found and fixed in each round
- [docs/INSTALLATION_GUIDE.md](docs/INSTALLATION_GUIDE.md) — installation and first-run setup (installation section is accurate; later sections still describe target design, flagged inline)
- [docs/control-panel/](docs/control-panel/) — per-feature parity docs against ASTER, each marked ✅ implemented / 🚧 partial / 📋 planned / ➖ N/A
- [CHANGELOG.md](CHANGELOG.md) — version history
