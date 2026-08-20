# Main Window of ASTER Control Panel

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_controlform

## What ASTER does

The ASTER Control Panel's main window is the single hub for all program configuration. Its header shows the installed license version/type, activation status, and workplace count; the body is a tab panel whose content changes with the selected tab (General Settings, Workplaces, About); and a bottom row of controls — System Restart (hidden unless needed), Apply, Help (F1), Exit, and Support Request — stays constant no matter which tab is active. Applying some settings can trigger a Windows UAC prompt because they need administrative rights.

## OpenMultiSeat status

**✅ Implemented** — maps to `MainWindow.xaml` in `OpenMultiSeat.GUI`, the "OpenMultiSeat Administration Console."

Instead of a tab strip, the main window uses a left-hand navigation rail plus a content frame that swaps pages: **Dashboard, Devices, Seats, Displays, Input Isolation, Audio, Settings, About**. There is no license/activation banner in the header — OpenMultiSeat has no licensing system to report status for. There is also no persistent Apply/System-Restart/UAC row at the bottom of the window; each page is responsible for its own save/apply actions (today, only the Devices page has real controls to apply — see [Known Issues](../known-issues.md) for the status of the other pages). A Support Request shortcut equivalent to ASTER's does not currently exist; the project instead directs users to its GitHub repository for issues and discussion.
