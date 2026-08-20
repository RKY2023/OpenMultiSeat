# Support Request

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_supportrequest

## What ASTER does

The "Support Request" window lets a user submit a technical issue directly to ASTER support, with a Request Type dropdown, an editable pre-filled email address, and a free-text problem description. The user chooses how much diagnostic depth to attach — general ASTER info, license info, basic or detailed system info, or full debug/minidump data — and can add screenshots or paste clipboard contents as files. The finished report can either be sent directly ("Create and Send Request"), built as a file for manual emailing ("Create ASTER report"), or just saved to disk.

## OpenMultiSeat status

➖ **Not applicable today.** OpenMultiSeat has no in-app support-request dialog, no support backend to send reports to, and no license data to prefill (see [Activation IDs on this PC](activation-ids-on-this-pc.md)). Support today happens entirely through the project's GitHub repository: [github.com/RKY2023/OpenMultiSeat](https://github.com/RKY2023/OpenMultiSeat) issues. Users experiencing a problem should open a GitHub issue and manually attach logs from `%ProgramData%\OpenMultiSeat\logs\` and, if relevant, `%ProgramData%\OpenMultiSeat\config.json`.

📋 **Planned** — an in-app diagnostics/support-bundle export dialog would be a natural and valuable addition, even without a paid support channel behind it. It wouldn't need to *send* anything (no server to send to), just collect and package the same categories of information ASTER offers — service/GUI version, current `SeatConfiguration`, recent log files, and basic system info — into a single zip the user attaches to a GitHub issue by hand. The repo already anticipates this: `OpenMultiSeat.Reliability`'s `DiagnosticsCollector` is the intended place to implement the collection logic, with the GUI's Settings or About page adding an "Export Diagnostics" button that calls it over the existing Named Pipe IPC channel. This is a much lighter-weight design than ASTER's, since it skips the license-info and direct-submission portions entirely — there's no license to report and no ASTER-run intake server to submit to.
