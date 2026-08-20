# Technical Support

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_support

## ASTER's approach

ASTER, being a commercial product, documents a structured multi-channel support process:

- **In-app support requests** — a button in the ASTER Control Panel (and in error dialogs) opens a request wizard.
- **Request categorization** — the user picks from five categories: "Just a question," activation issues, startup problems, system stability concerns, or "Other Problems."
- **Diagnostic report** — the user selects a report depth, ranging from basic ASTER info up to detailed debugging data including system minidumps, then attaches supporting files/screenshots.
- **Submission** — the compressed report is sent, and the user receives a support ticket number.
- **Other channels** — a community forum at `ibiksoft.com/community/`, email to `support@ibik.ru` (or `support@ibiksoft.com` per the "Important Information" page) if the automated report fails to send, a license verification tool at `aster-tools.ibiksoft.com/`, and chat support over Skype, WhatsApp, and Telegram.

This entire apparatus exists largely because ASTER is a paid product: a meaningful fraction of support requests are about activation, deactivation, and license transfer, which is why "activation issues" is one of the five top-level request categories.

## OpenMultiSeat's approach

OpenMultiSeat has no commercial support desk, no ticket numbers, and — because there's no licensing system — no "activation issues" category to route requests to in the first place. Support is entirely community-based, through the project's GitHub repository:

**https://github.com/RKY2023/OpenMultiSeat**

To get help or report a problem:

1. **Search existing GitHub Issues first.** Someone may have already hit the same problem (device enumeration quirk, installer failure, session-startup bug, etc.).
2. **Open a new GitHub Issue** if you don't find one. Include:
   - Your Windows version/build and whether the machine is 64-bit (required).
   - The OpenMultiSeat version (from the About page).
   - What you were doing, what you expected, and what actually happened.
   - Relevant logs from `%ProgramData%\OpenMultiSeat\logs\` and, if useful, your `%ProgramData%\OpenMultiSeat\config.json` (redact anything sensitive).
   - Diagnostics collected via the Reliability component's `DiagnosticsCollector`, if you're able to gather them — this is the closest equivalent to ASTER's "detailed debugging report."
3. **Feature requests and questions** are also welcome as GitHub Issues (or Discussions, if enabled on the repo) — there's no separate "Just a question" queue; it's all the same open, public issue tracker.
4. **Contribute a fix.** Because the project is open source, anyone can submit a pull request rather than waiting on a vendor to schedule a bugfix.

There is no phone line, no chat support, no ticket-number system, and no paid support tier — everything routes through the public GitHub repository, and response times depend on maintainer and community availability rather than a service-level agreement.
