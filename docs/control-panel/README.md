# Program Interface "OpenMultiSeat Control Panel"

This section documents every window/dialog/tab of OpenMultiSeat's admin GUI, structured after [ASTER Multiseat Software's](https://dokwiki.ibiksoft.com/en/v3/core/gui) equivalent "ASTER Control Panel" documentation — since OpenMultiSeat targets the same core use case (turn one PC into several independent seats) and is meant as a free, open-source replacement for it.

Each page states **what ASTER's equivalent does**, then **OpenMultiSeat's status** for that piece of functionality:

| Symbol | Meaning |
|---|---|
| ✅ **Implemented** | Live in OpenMultiSeat today |
| 🚧 **Stub / Partial** | A page exists but only shows a placeholder popup, or is only partly wired to its backend ([details](../known-issues.md)) |
| 📋 **Planned** | Not built yet; described here as a design target |
| ➖ **Not applicable** | An ASTER concept that doesn't apply — almost always because OpenMultiSeat has no licensing/activation system at all |

## Main Window & Tabs

| Page | ASTER equivalent | Status |
|---|---|---|
| [Main Window of ASTER Control Panel](aster-control-panel-main-window.md) | Main window | ✅ Implemented |
| ["General Settings" Tab](general-settings-tab.md) | General Settings tab | 🚧 Partial — "Assign CPU Cores…" and a real Workplace Start Mode setting (Manual/At System Startup/Via Workplace 1, wired to real Windows Scheduled Tasks) plus "Start Workplaces Now" are real; seat passwords use DPAPI LocalMachine scope so SYSTEM can decrypt them at boot, at the cost of no longer being a secret from other local accounts (see the page for the trade-off) |
| ["Workplaces" Tab](workplaces-tab.md) | Workplaces tab | 🚧 Partial — seat list/create/delete, account config, and device/display/audio assignment all real, with a per-seat "Configure…" view, a shared cross-seat **System** page, and a real drag-and-drop **Tile Layout** window (System + one column per seat) |
| ["About" Tab](about-tab.md) | About tab | ✅ Implemented |

## Licensing & Activation (ASTER-only — no equivalent needed in OpenMultiSeat)

| Page | ASTER equivalent | Status |
|---|---|---|
| [ASTER Activation Dialog](activation-dialog.md) | Activation Dialog | ➖ N/A |
| [Deactivation Dialog](deactivation-dialog.md) | Deactivation Dialog | ➖ N/A |
| ["Activation Ids on this PC" Window](activation-ids-on-this-pc.md) | Activation Ids window | ➖ N/A |
| ["Create Master License" Window](create-master-license.md) | Create Master License window | ➖ N/A |
| [Request for Automatic Activation](request-for-automatic-activation.md) | Auto-activation request | ➖ N/A |
| ["License Expire Time" Notification Window](license-expire-time-notification.md) | License expiry notice | ➖ N/A |
| ["Cleanup License" Action](cleanup-license.md) | Cleanup License | ➖ N/A |

## Seat / Device / Display / Input Configuration

| Page | ASTER equivalent | Status |
|---|---|---|
| [The 'User Account for Workstation' Window](user-account-for-workstation.md) | User Account for Workstation | 🚧 Partial — real account/password config with DPAPI-encrypted storage; verified credential-based process launch (`CreateProcessWithLogonW`) via "Test Launch…", not yet wired to automatic seat start |
| ["Workplace Tab Settings" Window](workplace-tab-settings.md) | Workplace Tab Settings | 🚧 Partial — real unlinked-displays filter + new-device highlight window; a real Tile Layout window exists now (see [Workplaces Tab](workplaces-tab.md)) but its columns are a fixed layout, not resizable/redistributable, so icon size/tile distribution still have nothing to control |
| ["Devices to Workplace(s) Assignment" Window](devices-to-workplace-assignment.md) | Devices to Workplace(s) Assignment | 🚧 Partial — exclusive single-seat assignment works (keyboard/mouse, plus camera/USB/Bluetooth as ownership records), no "To All" shared mode |
| ["Confirm Device Destination" Window](confirm-device-destination.md) | Confirm Device Destination | 🚧 Partial — real before/after move confirm on every Assign flow; no drag-and-drop or batched multi-device table |
| ["Assigning Video Outputs" Window](assigning-video-outputs.md) | Assigning Video Outputs | 🚧 Partial — real scan + assignment, no manual output remapping or clone toggle |
| ["Input Devices Switch" Window](input-devices-switch.md) | Input Devices Switch | 🚧 Partial — real bind/unbind + isolation toggle + validate on the Input Isolation page; no hotkey-rebind UI |
| ["IP Address for the Workplace" Window](ip-address-for-workplace.md) | IP Address for the Workplace | 📋 Planned |
| ["Applications and Folders" Window](applications-and-folders.md) | Applications and Folders | 📋 Planned |
| [Confirming Starting of Workplaces Window](confirming-starting-of-workplaces.md) | Confirming Starting of Workplaces | 🚧 Partial — a real Yes/No confirm gates the manual "Start Workplaces Now" button; no automatic boot-time prompt or "don't ask again" |
| ["Experimental Settings" Window](experimental-settings.md) | Experimental Settings | 📋 Planned |
| ["Assign CPU Cores" Window](assign-cpu-cores.md) | Assign CPU Cores | ✅ Implemented |
| ["Set Log Verbosity" Window](log-verbosity.md) | Set Log Verbosity | 📋 Planned |
| ["Reset Settings" Action](reset-settings.md) | Reset Settings | 📋 Planned |
| ["Check Configuration" Action](check-configuration.md) | Check Configuration | 📋 Planned |

## Network & Support

| Page | ASTER equivalent | Status |
|---|---|---|
| ["Proxy Server Setup" Window](proxy-server-setup.md) | Proxy Server Setup | ➖ N/A today / 📋 Planned if an update-checker is added |
| ["Check for Updates" Window](check-for-updates.md) | Check for Updates | 📋 Planned |
| ["Support Request" Window](support-request.md) | Support Request | ➖ N/A today (GitHub Issues) / 📋 Planned in-app diagnostics bundle |

## Status summary

- **3 pages fully implemented** (Main Window, About tab, Assign CPU Cores)
- **9 pages are stubs or partially wired** — every one now has *some* real backend wiring, none are pure `MessageBox` stubs any more: Workplaces tab, Devices to Workplace(s) Assignment, Assigning Video Outputs, and User Account for Workstation have real seat CRUD, device/display/audio assignment, and account configuration; General Settings tab has a real Workplace Start Mode setting wired to genuine Windows Scheduled Tasks alongside CPU Cores; Confirming Starting of Workplaces has a real manual-trigger confirm; Input Devices Switch has real bind/unbind + isolation toggle + validate (no hotkey-rebind UI); Confirm Device Destination has a real before/after move confirm on every Assign flow (no drag-and-drop/batched table); Workplace Tab Settings has a real unlinked-displays filter and new-device highlight window (no icon size/tile distribution, since there's no tile layout) (see [Known Issues](../known-issues.md))
- **7 pages are planned**, no implementation yet
- **9 pages don't apply today** — 7 pure licensing/activation pages plus Proxy Server Setup and Support Request, which currently have no OpenMultiSeat need but could become planned items later (see [`../user-manual/licensing-model.md`](../user-manual/licensing-model.md))
