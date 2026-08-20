# Data Flow Diagram (DFD)

Mermaid has no native DFD shape set, so this uses flowchart notation with the standard DFD conventions: rounded rectangles = **external entities**, rectangles = **processes**, open-ended bars = **data stores**, arrows = **data flow**.

## Level 0 — Context Diagram

```mermaid
flowchart LR
    User(("External Entity:<br/>User"))
    HW(("External Entity:<br/>Windows HID/Display/Audio<br/>Subsystems"))
    SYS["OpenMultiSeat<br/>(Service + GUI)"]

    User -->|configuration commands| SYS
    SYS -->|status, device lists| User
    HW -->|raw device/display/audio events| SYS
    SYS -->|input binding, display routing,<br/>audio routing commands| HW
```

## Level 1 — Service Internal Data Flow

```mermaid
flowchart TD
    User(("User"))
    HID(("Windows HID Subsystem"))
    DISP(("Windows Display Subsystem"))
    AUD(("Windows Core Audio"))
    WTS(("Windows WTS / Session Subsystem"))

    P1[["P1: Device Enumeration<br/>(HidDeviceEnumerator)"]]
    P2[["P2: Display Enumeration<br/>(DisplayEnumerator)"]]
    P3[["P3: Audio Enumeration<br/>(Audio manager)"]]
    P4[["P4: Seat Assignment<br/>(SeatManager)"]]
    P5[["P5: Input Isolation<br/>(InputIsolationService)"]]
    P6[["P6: Session Launch<br/>(SessionManager)"]]
    P7[["P7: IPC Gateway<br/>(Named Pipe Server)"]]
    P8[["P8: CPU Affinity Assignment<br/>(CpuAffinityManager)"]]

    DS1[("D1: Device Records store<br/>(DevicePersistence)")]
    DS2[("D2: Seat Configuration store<br/>(SeatConfiguration)")]
    DS3[("D3: Diagnostics/event log<br/>(DiagnosticsCollector)")]

    HID -->|raw device list| P1
    P1 -->|DeviceRecord list| DS1
    DISP -->|raw display list| P2
    AUD -->|raw audio endpoint list| P3

    User -->|scan/assign/start commands| P7
    P7 -->|forwarded requests| P4
    P4 -->|read known devices| DS1
    P4 -->|read/write seat-device-display-audio map| DS2
    P4 -->|assignment confirmed| P7
    P7 -->|responses, status| User

    P4 -->|seat start request| P5
    P4 -->|seat start request| P6
    P5 -->|bind input devices to session| WTS
    P6 -->|CreateProcessAsUser| WTS
    WTS -->|session state| P6
    P6 -->|session events| DS3
    P1 -->|device add/remove events| DS3

    User -->|CPU-core assignment| P7
    P7 -->|forwarded request| P8
    P8 -->|read/write per-seat core mask| DS2
    P8 -->|SetProcessAffinityMask| WTS
    P8 -->|assignment confirmed| P7
```
