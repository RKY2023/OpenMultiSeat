# Confirm Device Destination

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_selectdevdialog

## What ASTER does

The "Confirm Device Destination" window appears after an administrator drags and drops a device onto a different workplace in ASTER's control panel. It shows a table of the affected devices with a before/after column comparing their current and proposed workplace assignment, and lets the user check which of the listed devices should actually be reassigned before clicking OK to apply the change (or Cancel to discard it).

## OpenMultiSeat status

🚧 **Partially implemented** — a real single-resource confirm exists; no drag-and-drop or batched multi-device table.

Every "Assign to Seat…" action across Devices, Displays, Audio, and the System page now checks whether the picked resource is already assigned to a *different* seat before actually moving it. If so, a `ConfirmDeviceDestinationWindow` shows the resource name with its current and new seat side by side — Move to proceed, Cancel to abort — and only on confirmation does the GUI unassign from the old seat and assign to the new one. Picking the *same* seat it's already on, or assigning a not-yet-assigned resource, skips the prompt (nothing to compare against, matching ASTER's own trigger condition — the prompt is for genuine reassignment, not first assignment).

What's still missing relative to ASTER's own window:

- **No drag-and-drop.** The move still starts from the existing "Assign to Seat…" button/dialog flow (pick a device, then a seat from a dropdown), not a drag gesture onto a workplace.
- **One resource at a time, no batched table.** ASTER's dialog handles multiple dragged devices in one table with per-row checkboxes; this confirms a single resource per action, matching how assignment already works everywhere else in this GUI.
- **Not a single atomic operation.** The move is still `UnassignXFromSeatAsync` followed by `AssignXToSeatAsync` — two `ISeatManager`/`IAudioManager` calls in sequence from the GUI, not one transactional call. A failure between the two (unlikely, but possible) could leave the resource unassigned rather than on either seat.
