# BPMN Workflows

Business-process view of OpenMultiSeat's end-to-end lifecycle, from install to running multiple independent seats on one PC. Swimlanes separate what the **User**, the **Admin GUI**, and the **Windows Service** each do. Rendered as Mermaid flowcharts (GitHub-flavored Markdown renders these natively); each subgraph is one swimlane.

Unlike ASTER's equivalent workflow, there is **no Activation/License step** — OpenMultiSeat is free and open source, so the process goes straight from install to configuration.

## 1. Install → First Configuration

```mermaid
flowchart TD
    subgraph User["User"]
        U1([Download installer]) --> U2[Run OpenMultiSeat-x.x.x-x64-setup.exe]
        U2 --> U3{Admin<br/>elevation<br/>prompt}
        U3 -->|Accept| U4[Choose components:<br/>Core / GUI / Testing Tools / Docs]
        U4 --> U5[Finish wizard]
        U5 --> U6[Launch Admin Console]
    end

    subgraph Installer["NSIS Installer"]
        I1[Copy Service files to Program Files] --> I2[Copy GUI files]
        I2 --> I3[Register Start Menu / Desktop shortcuts]
        I3 --> I4[Write uninstaller + registry keys]
    end

    subgraph Service["OpenMultiSeat Service (Windows Service)"]
        S1[Service registered] --> S2[Service starts on boot]
        S2 --> S3[Load persisted SeatConfiguration]
    end

    subgraph GUI["Admin GUI"]
        G1[Connect to Service via Named Pipe IPC] --> G2[Show Dashboard]
        G2 --> G3[Devices page: Scan Devices]
        G3 --> G4[🚧 Seats page: assign devices to seats]
        G4 --> G5[🚧 Displays page: assign monitors to seats]
        G5 --> G6[🚧 Audio page: assign audio devices to seats]
    end

    U2 --> I1
    U3 -->|Deny| X1([Install aborted<br/>admin required for<br/>Program Files / HKLM])
    I4 --> S1
    U6 --> G1
```

## 2. Starting Seats (Session Launch)

```mermaid
flowchart TD
    subgraph User["User"]
        A1([Click 'Start Seat' for a<br/>configured seat]) --> A2{🚧 Confirm<br/>Starting of<br/>Workplaces dialog}
    end

    subgraph GUI["Admin GUI"]
        A2 -->|Confirm| B1[Send StartSeat IPC request]
    end

    subgraph Service["Windows Service"]
        B1 --> C1[SessionManager: validate seat config]
        C1 --> C2{Devices/display/<br/>audio all assigned<br/>and free?}
        C2 -->|No| C3[Return validation error]
        C2 -->|Yes| C4[InputIsolation: bind seat's<br/>keyboard/mouse exclusively]
        C4 --> C5[DisplayManager: route seat's<br/>monitor(s) via DisplayEnumerator]
        C5 --> C6[AudioManager: route seat's<br/>audio device]
        C6 --> C7[SessionManager.CreateProcessInSession<br/>via CreateProcessAsUser]
        C7 --> C8[Independent Windows session<br/>running for this seat]
    end

    C3 --> D1[GUI shows error to user]
    C8 --> D2[GUI shows seat as 'Running'<br/>on Dashboard]
```

## 3. Device Reconnect / Reliability (USB unplug-replug)

```mermaid
flowchart TD
    E1([USB device unplugged]) --> E2[Windows raises device-removal event]
    E2 --> E3[HidDeviceEnumerator detects removal]
    E3 --> E4[Reliability: DiagnosticsCollector logs event]
    E4 --> E5{Device belongs to<br/>a running seat?}
    E5 -->|Yes| E6[InputIsolation: seat loses input<br/>until device returns]
    E5 -->|No| E7[No action — device was unassigned]
    E8([USB device replugged]) --> E9[HidDeviceEnumerator re-detects<br/>by stable Hardware ID]
    E9 --> E10[Reliability: auto-rebind to<br/>its previously assigned seat]
    E6 -.->|on replug| E10
```
