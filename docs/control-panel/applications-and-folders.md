# Applications and Folders

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_selectappordir

## What ASTER does

The "Applications and Folders" window is a tree-view file browser used to pick programs or folders to bind to a workplace configuration (such as a per-seat IP address). Selecting a program and clicking "Add application" adds just that program to the binding list; selecting a folder and clicking "Add folder" adds every program inside it in one step.

## OpenMultiSeat status

📋 **Planned** — no current implementation.

OpenMultiSeat has no per-seat "bind an application or folder" UI today. This would build on `OpenMultiSeat.Sessions.ProcessLauncher`, which already handles launching a process inside a given seat's Windows session via `CreateProcessAsUser`, and would extend it with a browsable picker so an administrator can pre-select which applications (or entire folders of applications) should be available, auto-launched, or restricted per seat.

A real implementation would need:

- A tree-view or file-picker control rooted at "Computer"/drives, matching ASTER's navigation model.
- "Add application" enabled only when a single executable is selected.
- "Add folder" enabled only when a directory is selected, adding all programs within it.
- A resulting list model (per `Seat`/`SeatConfiguration`) that `ProcessLauncher` reads from when starting a seat's session.

This depends on the per-seat application/shortcut model not yet existing in `OpenMultiSeat.Core`, so it is planned but not scheduled.
