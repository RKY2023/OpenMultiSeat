# Create Master License

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/gui/gui_masterform

## What ASTER does

The "Create Master License" window, opened via `mutectl –make-master`, lets an administrator turn a batch activation code into a master license record on a machine that will later be cloned to other computers, optionally protected by a password required when activating the clones. "Create Master Record" writes the disk record (requiring admin rights) and strips any existing ASTER licenses from the master machine first.

## OpenMultiSeat status

➖ **Not applicable — OpenMultiSeat has no licensing system to seed a master license from.** There are no batch activation codes, no per-clone activation passwords, and no license records to write to disk, because OpenMultiSeat has no license keys or license server of any kind (see [Activation IDs on this PC](activation-ids-on-this-pc.md)).

The underlying scenario ASTER is solving — deploying the same configured install across many cloned machines — is still relevant to OpenMultiSeat, but it requires no special tooling: since there's nothing to activate, deploying to a cloned machine is just running the same NSIS installer (or copying the same install) and letting the Service pick up its own `SeatConfiguration` from `%ProgramData%\OpenMultiSeat\config.json`. There is no equivalent "master" step, no clone-side password prompt, and nothing that needs administrator-only license provisioning beyond the installer's normal admin-elevation requirement.
