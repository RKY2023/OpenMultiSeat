# Development Phases

## Phase 0: Feasibility (Current)
Prove Windows device/session/display enumeration works.

**Deliverables:**
- Device enumerator POC
- Display enumerator POC
- Session enumerator POC
- JSON output samples

## Phase 1: Device Discovery
Complete keyboard/mouse/monitor/audio discovery with stable IDs.

**Success:** GUI correctly identifies all devices per seat.

## Phase 2: Seat Configuration
Multi-seat model, validation, device assignment.

**Success:** Two seats configured, no device conflicts.

## Phase 3: Session Management
Windows session lifecycle, process launching.

**Success:** Two users can log in independently.

## Phase 4: Display Management
Display enumeration, assignment, topology.

**Success:** Monitors route to correct seat.

## Phase 5: Input Isolation
Keyboard/mouse routing to correct seat (critical).

**Success:** Input does not leak between seats.

## Phase 6: Audio
Audio endpoint routing per seat.

## Phase 7: Admin GUI
Full WPF administration console.

## Phase 8: Installer
MSI installer with service registration.

## Phase 9: Reliability
Crash recovery, USB reconnect, Windows Update resilience.
