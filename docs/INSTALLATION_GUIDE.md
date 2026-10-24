# OpenMultiSeat Installation and Configuration Guide

This guide covers installation, configuration, and first-run setup for OpenMultiSeat.

> **Accuracy note:** the [Installation](#installation) section below reflects the real MSI installer that now exists (`installer/Product.wxs`, built with the [WiX Toolset](https://wixtoolset.org/)). Most of what follows it — the multi-component installer options, the `tools\` folder of standalone Phase testers, "Export Configuration," "Generate Diagnostic Report," fast-user-switching-based multi-session testing — describes the project's original target design, not what's built today. See [Known Issues](known-issues.md) for what's actually wired up in the shipped GUI right now.



## Table of Contents

1. [System Requirements](#system-requirements)
2. [Installation](#installation)
3. [Initial Configuration](#initial-configuration)
4. [Device Setup](#device-setup)
5. [Testing](#testing)
6. [Troubleshooting](#troubleshooting)

## System Requirements

### Hardware
- **CPU**: Intel or AMD processor (64-bit)
- **RAM**: Minimum 4GB (8GB+ recommended)
- **Storage**: 500MB free space
- **Display**: Multiple monitors (one per seat)
- **Input Devices**: USB keyboards and mice (one per seat)

### Software
- **OS**: Windows 10/11 64-bit
- **Runtime**: .NET 9.0
- **Drivers**: Latest chipset and USB drivers

### Administrator Access
Installation requires administrator privileges.

## Installation

Two ways to get OpenMultiSeat running, both produced from the same `dotnet publish` output (a self-contained, single-file build — no separate .NET runtime install needed):

### Option A: Standalone .exe (no install, no admin needed just to run it)
`OpenMultiSeat.GUI.exe` is a self-contained single-file executable — copy it anywhere and double-click it, nothing to install or register. This is the fastest way to just run the app. (Some individual features — notably "Workplace Start Mode" on the Settings page — still need the app running as Administrator, regardless of which of these two options you used; see [General Settings tab](control-panel/general-settings-tab.md).)

### Option B: `OpenMultiSeat-Setup.msi` (real installer)
A genuine Windows Installer package, built from `installer/Product.wxs` with the [WiX Toolset](https://wixtoolset.org/) v5:

1. Double-click `OpenMultiSeat-Setup.msi`. Since it installs per-machine under Program Files, Windows will prompt for Administrator via UAC.
2. Accept the license (MIT — same text as the repo's `LICENSE` file), optionally change the install folder (default `C:\Program Files\OpenMultiSeat`), click Install.
3. The installer places one file — `OpenMultiSeat.GUI.exe` — under the chosen folder, plus a Start Menu shortcut ("OpenMultiSeat") and a Desktop shortcut.

There are no installer-time component checkboxes (Core/GUI/Testing Tools/Documentation) and no `tools\` folder of Phase testers — those don't exist as shipped artifacts; the single GUI exe is the whole product today.

**Building the installer from source** (not needed just to install — only if you're rebuilding it):
```
dotnet publish src\OpenMultiSeat.GUI\OpenMultiSeat.GUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\OpenMultiSeat.GUI
dotnet tool install --global wix --version 5.0.2
wix extension add WixToolset.UI.wixext/5.0.2
wix build installer\Product.wxs -ext WixToolset.UI.wixext -arch x64 -d SourceDir=publish\OpenMultiSeat.GUI -b installer -o publish\OpenMultiSeat-Setup.msi
```
(Version pinned to 5.0.2 deliberately — WiX v6/v7 require accepting a separate paid-use EULA (the "Open Source Maintenance Fee") to add extensions; v5.0.2 predates that and needs no EULA acceptance.)

### Uninstalling
Installed via the MSI: Settings → Apps → Apps & features → "OpenMultiSeat" → Uninstall, same as any Windows Installer package (also removes the two shortcuts). Ran only from the standalone .exe: there's nothing installed to uninstall — just delete the .exe. Either way, `%AppData%\OpenMultiSeat\` (seat/device configuration, settings, logs) is left in place, matching the [Uninstallation](#uninstallation) section below.

## Initial Configuration

### Launch Admin Console
1. Double-click "OpenMultiSeat Admin" on Desktop
2. Or find in Start Menu > OpenMultiSeat

### Dashboard Overview
The Dashboard shows:
- Total input devices detected
- Configured seats
- Connected displays
- Active Windows sessions

## Device Setup

### Phase 1: Enumerate Devices

1. In Admin Console, click **Devices** tab
2. Click **Scan Devices** button
3. Wait for scan to complete
4. Review detected devices:
   - Keyboards
   - Mice
   - Displays
   - Audio devices

**Using Testing Tool** (if installed):
```cmd
"C:\Program Files\OpenMultiSeat\tools\Phase1.DeviceTester.exe"
```

### Phase 2: Create Seats

1. Click **Seats** tab in Admin Console
2. Click **Create Seat**
3. Enter seat name (e.g., "Workstation 1")
4. Click **Create**
5. Repeat for each additional seat

**Using Testing Tool**:
```cmd
"C:\Program Files\OpenMultiSeat\tools\Phase2.SeatConfigurator.exe"
```

### Phase 3: Configure Input Isolation

1. Click **Input Isolation** tab
2. For each input device:
   - Select device
   - Choose target seat
   - Bind device to seat
3. Click **Validate Configuration**
4. Ensure no errors shown

**Important**: At least one keyboard and mouse per seat

**Using Testing Tool**:
```cmd
"C:\Program Files\OpenMultiSeat\tools\Phase5.InputIsolationTester.exe"
```

### Phase 4: Configure Displays

1. Click **Displays** tab
2. Review detected displays
3. For each display:
   - Select display
   - Choose target seat
   - Assign to seat
4. Click **Validate Configuration**

**Using Testing Tool**:
```cmd
"C:\Program Files\OpenMultiSeat\tools\Phase4.DisplayConfigurator.exe"
```

### Phase 5: Configure Audio (Optional)

1. Click **Audio** tab
2. For each audio device:
   - Select device
   - Choose seat and role (Playback/Recording)
   - Assign to seat
3. Click **Enable Audio Routing**

**Using Testing Tool**:
```cmd
"C:\Program Files\OpenMultiSeat\tools\Phase6.AudioConfigurator.exe"
```

## Testing

### Test Seat 1
1. Navigate to Seat 1's display
2. Move mouse assigned to Seat 1
   - Cursor should appear only on Seat 1's display
3. Type on Seat 1's keyboard
   - Should only register on Seat 1
4. Check display isolation
   - Only Seat 1's display should show content

### Test Seat 2 (if configured)
Repeat same tests for Seat 2

### Test Windows Session Launch
1. Log in as different users on each seat
2. Verify separate Windows sessions are created
3. Test application launching per seat

### Export Configuration
1. Click Dashboard tab
2. Click **Export Configuration** button
3. Save backup copy of configuration

## Troubleshooting

### Devices Not Detected
**Issue**: Device scan finds 0 devices

**Solutions**:
1. Check USB connections
2. Update chipset drivers
3. Disable USB Selective Suspend:
   - Settings > Power > Additional power settings
   - Change plan settings > Change advanced power settings
   - USB selective suspend setting > Disabled

### Devices Detected But Not Binding
**Issue**: Cannot bind devices to seats

**Solutions**:
1. Verify device is in registry (Phase 1 Tester)
2. Try scanning again
3. Restart application
4. Check Device Manager for conflicts

### Display Not Assigned
**Issue**: Monitor won't assign to seat

**Solutions**:
1. Verify display is detected and connected
2. Check for duplicate assignments
3. Verify seat exists before assignment
4. Try unplugging/replugging display

### Input Not Isolated
**Issue**: Keyboard/mouse input affects all seats

**Solutions**:
1. Check input bindings (Phase 5 Tester)
2. Verify isolation is enabled
3. Test individual devices
4. Check for unbound devices

### Audio Not Routing
**Issue**: Sound plays through all speakers

**Solutions**:
1. Verify audio devices detected (Phase 6 Tester)
2. Check audio assignments
3. Enable audio routing
4. Update audio drivers

### Windows Sessions Not Created
**Issue**: Only single Windows session visible

**Solutions**:
1. Verify different user accounts created
2. Check fast user switching enabled
3. Try manual login (Ctrl+Alt+Del > Switch user)
4. Check Task Manager > Users tab

## Support and Logging

### Viewing Logs
Logs are stored in: `%APPDATA%\OpenMultiSeat\logs\`

### Generating Diagnostics
1. Click Settings in Admin Console
2. Click **Generate Diagnostic Report**
3. Save report for troubleshooting

### Getting Help
- GitHub Issues: https://github.com/RKY2023/OpenMultiSeat/issues
- Check documentation in `C:\Program Files\OpenMultiSeat\docs\`
- Review CHANGELOG.md for known issues

## Next Steps

After successful configuration:

1. **Backup Configuration**
   - Export configuration regularly
   - Store in safe location

2. **Create User Accounts**
   - One account per seat
   - Set up with appropriate permissions

3. **Test Applications**
   - Run productivity software
   - Verify performance
   - Test with real workloads

4. **Monitor Performance**
   - Check resource usage
   - Monitor logs for errors
   - Adjust settings as needed

5. **Security Hardening**
   - Enable Windows Firewall
   - Use strong passwords
   - Restrict physical access

## Performance Tips

- **CPU**: Limit background processes
- **RAM**: Close unused applications
- **Storage**: Monitor disk space
- **Network**: Dedicate network for system
- **Display**: Use quality monitors/cables

## Uninstallation

To remove OpenMultiSeat:

1. Settings > Apps > Apps & features
2. Find "OpenMultiSeat"
3. Click Uninstall
4. Click Yes to confirm

Configuration files remain in `%APPDATA%\OpenMultiSeat\` after uninstall.
