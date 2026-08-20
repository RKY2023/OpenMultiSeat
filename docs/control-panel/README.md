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
| ["General Settings" Tab](general-settings-tab.md) | General Settings tab | ✅ Implemented |
| ["Workplaces" Tab](workplaces-tab.md) | Workplaces tab | 🚧 Partial — seat list/create/delete, account config, and device/display/audio assignment all real, with both a per-seat "Configure…" view and a shared cross-seat **System** page |
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
| ["Workplace Tab Settings" Window](workplace-tab-settings.md) | Workplace Tab Settings | 🚧 Stub |
| ["Devices to Workplace(s) Assignment" Window](devices-to-workplace-assignment.md) | Devices to Workplace(s) Assignment | 🚧 Partial — exclusive single-seat assignment works (keyboard/mouse, plus camera/USB/Bluetooth as ownership records), no "To All" shared mode |
| ["Confirm Device Destination" Window](confirm-device-destination.md) | Confirm Device Destination | 📋 Planned |
| ["Assigning Video Outputs" Window](assigning-video-outputs.md) | Assigning Video Outputs | 🚧 Partial — real scan + assignment, no manual output remapping or clone toggle |
| ["Input Devices Switch" Window](input-devices-switch.md) | Input Devices Switch | 🚧 Stub |
| ["IP Address for the Workplace" Window](ip-address-for-workplace.md) | IP Address for the Workplace | 📋 Planned |
| ["Applications and Folders" Window](applications-and-folders.md) | Applications and Folders | 📋 Planned |
| [Confirming Starting of Workplaces Window](confirming-starting-of-workplaces.md) | Confirming Starting of Workplaces | 📋 Planned |
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

- **4 pages fully implemented** (Main Window, General Settings tab, About tab, Assign CPU Cores)
- **6 pages are stubs or partially wired** — real backend exists; Workplaces tab, Devices to Workplace(s) Assignment, Assigning Video Outputs, and User Account for Workstation now have real seat CRUD, device/display/audio assignment, and account configuration, but Workplace Tab Settings and Input Devices Switch (and Confirm Device Destination, now Planned) remain unwired (see [Known Issues](../known-issues.md))
- **9 pages are planned**, no implementation yet
- **9 pages don't apply today** — 7 pure licensing/activation pages plus Proxy Server Setup and Support Request, which currently have no OpenMultiSeat need but could become planned items later (see [`../user-manual/licensing-model.md`](../user-manual/licensing-model.md))
