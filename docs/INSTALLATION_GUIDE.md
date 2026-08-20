# OpenMultiSeat Installation and Configuration Guide

This guide covers installation, configuration, and first-run setup for OpenMultiSeat.

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

### Step 1: Download Installer
Download the latest OpenMultiSeat installer from GitHub:
```
https://github.com/RKY2023/OpenMultiSeat/releases
```

### Step 2: Run Installer
1. Right-click `OpenMultiSeat-1.0.0-x64-setup.exe`
2. Select "Run as administrator"
3. Click "Next" on the Welcome screen
4. Review and accept the license
5. Select components to install:
   - **Core Components**: Always required
   - **GUI Application**: Recommended for configuration
   - **Testing Tools**: Optional for troubleshooting
   - **Documentation**: Optional for reference

### Step 3: Choose Installation Directory
Default: `C:\Program Files\OpenMultiSeat`

Modify if needed, then click "Install"

### Step 4: Complete Installation
- Click "Finish" to close the installer
- OpenMultiSeat Admin shortcut appears on Desktop and Start Menu

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
