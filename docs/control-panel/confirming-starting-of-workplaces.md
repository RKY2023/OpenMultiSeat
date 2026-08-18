# Confirming Starting of Workplaces

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_askforstart

## What ASTER does

When a user has chosen manual workplace startup (in the General Settings tab's "Starting Workplaces" option), this dialog appears at PC power-on to confirm the user actually wants to start the ASTER workplaces before doing so. It offers a "Do not ask this question again" checkbox so users who always answer the same way can skip the prompt on future boots.

## OpenMultiSeat status

**📋 Planned** — no current implementation. OpenMultiSeat should offer an equivalent confirmation step before it starts seat sessions, gated behind a manual-startup preference on the [Settings page](general-settings-tab.md) (analogous to ASTER's "How to start Workplaces" option). It would build on `OpenMultiSeat.Sessions`' `SessionManager`, which is what actually brings up each seat's Windows session and launches its process (`ProcessLauncher`, using `CreateProcessAsUser`): the confirmation dialog would sit in front of that call, with a persisted "don't ask again" preference stored alongside the rest of `SeatConfiguration`. No such dialog, preference, or startup-mode setting exists in the GUI yet.
