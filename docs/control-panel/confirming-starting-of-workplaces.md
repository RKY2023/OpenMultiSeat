# Confirming Starting of Workplaces

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_askforstart

## What ASTER does

When a user has chosen manual workplace startup (in the General Settings tab's "Starting Workplaces" option), this dialog appears at PC power-on to confirm the user actually wants to start the ASTER workplaces before doing so. It offers a "Do not ask this question again" checkbox so users who always answer the same way can skip the prompt on future boots.

## OpenMultiSeat status

**🚧 Partial** — a narrower, button-triggered version exists, not ASTER's automatic boot-time prompt.

The [Settings page](general-settings-tab.md)'s new **"Start Workplaces Now"** button (the manual-start path of the new Workplace Start Mode setting) shows a real Yes/No `MessageBox` — "Start all configured workplaces now? ... seats set to 'Display login dialog' are skipped" — before calling `SeatStartupOrchestrator.StartAllSeatsAsync`. That's the same "confirm, then actually start sessions" shape ASTER's dialog has, and it does gate a real action (Explorer launched as each eligible seat's Windows account via `SessionManager.LaunchProcessWithCredentialsAsync`), so it's no longer a pure placeholder.

What's still missing relative to ASTER's own version:

- **It's manual-trigger only.** ASTER shows this prompt *automatically at PC power-on* specifically when Manual mode is selected. OpenMultiSeat's Manual mode registers no Scheduled Task at all — nothing runs automatically under Manual mode, so there's no boot-time moment to show a prompt from. (The two *automatic* modes — At System Startup / At First Login — skip any prompt and just try to start seats directly, matching ASTER's own automatic-mode behavior of not asking.)
- **No "do not ask this question again" checkbox** or persisted preference for it — every manual start re-prompts.

Building the ASTER-equivalent boot-time prompt for real would need an interactive-session component triggered at logon (not the headless `--start-seats` scheduled task, which by design has no UI to prompt from) that shows this same confirm dialog before calling into the orchestrator, plus a persisted "don't ask again" flag.
