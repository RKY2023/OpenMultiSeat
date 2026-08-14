# OpenMultiSeat Architecture

## High-Level Overview

OpenMultiSeat is structured as a privileged Windows Service + unprivileged user GUI communicating via Named Pipes IPC.

```
User Layer
    └── Admin GUI (WPF)
            ↓ IPC (Named Pipes)
        ┌───────────────────────┐
        │ MultiSeat Service     │
        │ (Windows Service)      │
        └───────────────────────┘
            ├── Device Manager
            ├── Display Manager
            ├── Session Manager
            ├── Seat Manager
            └── Audio Manager
```

## Component Breakdown

### Core (OpenMultiSeat.Core)
Data models and shared types:
- `Seat` — multi-seat configuration
- `InputDevice` — keyboard/mouse
- `Display` — monitor
- `AudioDevice` — audio endpoint
- `SeatConfiguration` — global configuration

### Devices (OpenMultiSeat.Devices)
Windows HID enumeration:
- `IHidDeviceEnumerator`
- Device discovery via SetupAPI, Raw Input APIs
- Stable device ID tracking

### Displays (OpenMultiSeat.Displays)
Display detection and management:
- `IDisplayEnumerator`
- Windows Display Configuration APIs
- EDID parsing, resolution/refresh detection

### Sessions (OpenMultiSeat.Sessions)
Windows session lifecycle:
- `ISessionEnumerator`
- WTS APIs for session enumeration
- Process launching (`CreateProcessAsUser`)

### Audio (OpenMultiSeat.Audio)
Audio endpoint routing:
- `IAudioEnumerator`
- Windows Core Audio / MMDevice API
- Per-seat audio assignment

### IPC (OpenMultiSeat.IPC)
Local inter-process communication:
- Named Pipe server/client
- JSON message serialization
- Security ACLs

### Service (OpenMultiSeat.Service)
Main Windows Service:
- Lifecycle management
- Component coordination
- IPC endpoint hosting
- State persistence

### GUI (OpenMultiSeat.GUI)
WPF administration console:
- Seat configuration
- Device assignment
- Status monitoring
- Diagnostics

## Data Flow

### Session Startup
1. GUI requests seat start
2. Service validates configuration
3. Session Manager creates Windows session
4. Seat Manager tracks session-to-seat mapping
5. Application launcher starts configured apps
6. Status updates flow back to GUI

### Input Routing (Phase 5+)
1. Device Manager detects keyboard/mouse
2. Assigns to seat based on configuration
3. Input isolation layer routes events
4. Virtual HID or driver handles system-wide routing
5. Correct seat receives input

## Security Model

Named Pipe ACLs restrict client access to authenticated users.

Allowed operations (allowlist):
- `GetSystemInfo`
- `GetSeats`
- `StartSeat`
- `StopSeat`
- `AssignDevice`

Forbidden:
- Arbitrary command execution
- File system access beyond configuration
- Privilege elevation bypass

## State Persistence

Configuration stored in JSON:
```
%ProgramData%\OpenMultiSeat\config.json
```

Logs in:
```
%ProgramData%\OpenMultiSeat\logs\
```
