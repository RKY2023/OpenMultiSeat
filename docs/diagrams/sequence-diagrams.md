# Sequence Diagrams

Message-level sequences for OpenMultiSeat's three core interactions, all crossing the GUI ↔ Service Named Pipe IPC boundary described in [`../architecture.md`](../architecture.md).

## 1. Device Scan (Devices page — this one is fully implemented today)

```mermaid
sequenceDiagram
    actor U as User
    participant GUI as Admin GUI<br/>(DevicesPage)
    participant IPC as Named Pipe IPC
    participant SVC as MultiSeat Service
    participant HID as HidDeviceEnumerator
    participant DP as DevicePersistence

    U->>GUI: Click "Scan Devices"
    GUI->>IPC: ScanDevicesRequest
    IPC->>SVC: forward request
    SVC->>HID: EnumerateDevices()
    HID->>HID: SetupAPI / Raw Input query
    HID-->>SVC: List<DeviceRecord>
    SVC->>DP: SaveDevices(records)
    DP-->>SVC: ack
    SVC-->>IPC: ScanDevicesResponse(records)
    IPC-->>GUI: forward response
    GUI-->>U: Populate data grid<br/>(Device Name, Hardware ID, Vendor ID, Product ID, First Seen)
```

## 2. Seat Start (target design — Seats/Displays/Audio pages are stubs today, see known-issues.md)

```mermaid
sequenceDiagram
    actor U as User
    participant GUI as Admin GUI<br/>(SeatsPage, planned)
    participant IPC as Named Pipe IPC
    participant SVC as MultiSeat Service
    participant SM as SeatManager
    participant II as InputIsolationService
    participant DM as DisplayManager
    participant AM as AudioManager
    participant SESS as SessionManager

    U->>GUI: Click "Start Seat"
    GUI->>GUI: Show "Confirming Starting<br/>of Workplaces" dialog
    U->>GUI: Confirm
    GUI->>IPC: StartSeatRequest(seatId)
    IPC->>SVC: forward request
    SVC->>SM: ValidateSeat(seatId)
    alt devices/display/audio all assigned & free
        SM-->>SVC: OK
        SVC->>II: BindDevices(seat)
        SVC->>DM: RouteDisplay(seat)
        SVC->>AM: RouteAudio(seat)
        SVC->>SESS: CreateProcessInSession(seat)
        SESS-->>SVC: session started
        SVC-->>IPC: StartSeatResponse(success)
    else validation fails
        SM-->>SVC: ValidationError(reason)
        SVC-->>IPC: StartSeatResponse(error)
    end
    IPC-->>GUI: forward response
    GUI-->>U: Update Dashboard status
```

## 3. Login / Process Launch Detail (`CreateProcessAsUser`)

```mermaid
sequenceDiagram
    participant SVC as MultiSeat Service
    participant SESS as SessionManager
    participant WTS as Windows WTS APIs
    participant OS as Windows Session Manager

    SVC->>SESS: CreateProcessInSession(seat, userAccount)
    SESS->>WTS: WTSQueryUserToken(sessionId)
    WTS-->>SESS: user token
    SESS->>OS: CreateProcessAsUser(token, shell/app path)
    OS-->>SESS: process handle
    SESS-->>SVC: session running for seat
```
