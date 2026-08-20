# OpenMultiSeat Installer

This directory contains the installer for OpenMultiSeat, a Windows multi-seat system that allows multiple independent workstations to run on a single PC.

## Prerequisites

### Required
- Windows 10/11 64-bit
- .NET 9.0 Runtime
- Administrator privileges for installation

### For Building the Installer
- Visual Studio 2022 or .NET 9 SDK
- NSIS (Nullsoft Scriptable Install System)
  - Download from: https://nsis.sourceforge.io/
  - Add to PATH or install in default location

## Building the Installer

### Using PowerShell Script (Recommended)

```powershell
# Navigate to the installer directory
cd scripts/Installer

# Run the build script
.\Build-Installer.ps1 -Configuration Release -Platform x64

# Or with default parameters
.\Build-Installer.ps1
```

The script will:
1. Build all OpenMultiSeat projects in Release mode
2. Verify NSIS installation
3. Generate the installer executable
4. Validate the output

### Manual Build

If you prefer to build manually:

```powershell
# 1. Build the solution
dotnet build OpenMultiSeat.sln -c Release -p:Platform=x64

# 2. Compile NSIS script
"C:\Program Files (x86)\NSIS\makensis.exe" /V4 scripts/Installer/OpenMultiSeat.nsi

# 3. Output: build/installer/OpenMultiSeat-1.0.0-x64-setup.exe
```

## Installation Components

The installer includes:

### Core Components (Required)
- Core framework library
- Device enumeration and management
- Session management
- Display configuration
- Input isolation
- Audio routing
- IPC communication
- Windows Service executable

### GUI Application (Optional)
- WPF administration console
- System configuration interface
- Device/seat/display/audio management
- Desktop shortcut

### Testing Tools (Optional)
- Phase 1: Device Tester
- Phase 2: Seat Configurator
- Phase 4: Display Configurator
- Phase 5: Input Isolation Tester
- Phase 6: Audio Configurator

### Documentation (Optional)
- README
- License
- Changelog

## Installation Paths

Default installation directory: `C:\Program Files\OpenMultiSeat`

```
OpenMultiSeat/
├── bin/
│   ├── *.dll (core libraries)
│   ├── OpenMultiSeat.Service.exe
│   └── OpenMultiSeat.GUI.exe
├── tools/
│   ├── Phase*.exe (testing tools)
├── config/
│   └── (runtime configuration)
└── docs/
    ├── README.md
    ├── LICENSE
    └── CHANGELOG.md
```

## Post-Installation

### First Run
1. Run OpenMultiSeat Admin from Start Menu
2. Configure input devices in the Devices tab
3. Create seats and assign devices
4. Configure display mapping
5. Set up audio routing if needed

### Service Setup (Optional)
To run as a Windows Service:

```cmd
# Install service (requires admin)
sc create OpenMultiSeat binPath= "C:\Program Files\OpenMultiSeat\bin\OpenMultiSeat.Service.exe"

# Start service
net start OpenMultiSeat

# Stop service
net stop OpenMultiSeat

# Remove service
sc delete OpenMultiSeat
```

## Troubleshooting

### NSIS Not Found
- Download NSIS: https://nsis.sourceforge.io/Download
- Install to default location (C:\Program Files (x86)\NSIS)
- Or add makensis.exe to PATH

### Build Failures
- Ensure .NET 9 SDK is installed: `dotnet --version`
- Clean build directory: `dotnet clean OpenMultiSeat.sln`
- Rebuild: `dotnet build OpenMultiSeat.sln -c Release`

### Installation Fails
- Run installer as Administrator
- Ensure Windows 10/11 64-bit
- Check disk space (minimum 500MB required)
- Disable antivirus temporarily during installation

## Uninstallation

### Via Control Panel
1. Settings > Apps > Apps & features
2. Find "OpenMultiSeat"
3. Click Uninstall

### Via Command Line
```cmd
# Run uninstaller directly
"C:\Program Files\OpenMultiSeat\uninst.exe"
```

## Updating

To update to a new version:
1. Uninstall current version
2. Install new version
3. Reconfigure system settings (configurations are preserved in %APPDATA%)

## File Locations

- **Configuration**: `%APPDATA%\OpenMultiSeat\`
- **Logs**: `%APPDATA%\OpenMultiSeat\logs\`
- **Installation**: `C:\Program Files\OpenMultiSeat\`
- **Start Menu**: `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\OpenMultiSeat\`

## Support

For issues and feature requests, visit:
- GitHub: https://github.com/RKY2023/OpenMultiSeat
- Issues: https://github.com/RKY2023/OpenMultiSeat/issues

## License

OpenMultiSeat is released under the MIT License. See LICENSE file for details.
