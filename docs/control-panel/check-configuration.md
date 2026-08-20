# Check Configuration

> ASTER reference: observed in the live ASTER v2.70.5 desktop app (Workplaces tab → hamburger menu → "Check configuration") — not covered by the public wiki pages this doc set otherwise cites.

## What ASTER does

"Check configuration" runs a manual validation pass over the current Workplaces-tab setup and flags problems — e.g. a workplace with no display assigned, or devices left in an inconsistent state. It's a manually-triggered counterpart to ASTER's automatic hot-plug detection: when the set of connected displays/devices changes live (a monitor is plugged in or removed), a "Configuration has changed" banner appears at the bottom of the Workplaces tab with **Yes** (accept the detected change), **Move to unused** (park newly-detected devices in the unassigned pool instead), and **Close** options — the same underlying consistency check, just triggered automatically by a hardware-change event instead of on demand.

## OpenMultiSeat status

📋 **Planned** — no current implementation of either the manual check or the automatic hot-plug banner.

Both would build on the same validation logic, just triggered differently:

- A `SeatConfiguration` validator (Core) that checks each `Seat` has at least one display, at least one input device, and no device assigned to more than one seat simultaneously (device assignment is expected to be exclusive per seat, unlike audio which can be shared).
- A manual "Check configuration" button on the Seats page that runs the validator on demand and surfaces any problems found.
- An automatic variant triggered by device-add/remove events already available from `HidDeviceEnumerator`/`DisplayEnumerator` — when a new display or device shows up that isn't in any seat's assignment, prompt with the same Yes / Move to unused / Close pattern ASTER uses, rather than silently ignoring or silently auto-assigning it.

See [Devices to Workplace(s) Assignment](devices-to-workplace-assignment.md) for the underlying assignment model this validates.
