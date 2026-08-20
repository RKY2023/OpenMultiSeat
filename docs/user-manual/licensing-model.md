# Licensing Model

> ASTER reference: https://dokwiki.ibiksoft.com/en/v3/core/ugd/ugd_licensingandterms

## ASTER's approach

ASTER's licensing hub page links out to three documents that together define how the commercial product is used:

- **Terms of Licensing and Updating** — the rules governing how the software may be used and how updates are delivered.
- **End User License Agreement (EULA)** — the formal legal document defining users' rights and restrictions.
- **Working with Licenses** — the practical day-to-day guide covering license *activation*, *deactivation*, the *trial period*, and the *per-seat licensing model*.

Reading further into ASTER's "Important Information" page fills in the mechanics: an ASTER license is **tied to the CPU and HDD of the specific computer it's activated on**, it can only be active on one machine at a time (the license must be explicitly deactivated before it can move to another machine or survive a hardware swap), and **every additional workplace beyond what a base license covers requires purchasing a separate Pro or Annual license**. In practice this means:

- The product ships as a 14-day trial.
- After the trial, continued use requires a paid license key.
- Scaling up the number of seats costs more money, license by license.
- Hardware changes (new CPU, new drive) can invalidate an activation and require a deactivate/reactivate cycle, sometimes involving contacting support.

## OpenMultiSeat's approach

**OpenMultiSeat has no licensing or activation system at all — it is free and open source, full stop.** There is no trial period because there is nothing that expires. There is no activation step, no deactivation step, no hardware fingerprinting, no license server, and no per-seat fee.

Concretely, this means:

- **No trial limit.** Install it and use it for as long as you like, on as many machines as you like.
- **No per-seat charge.** Configure two seats or twenty; OpenMultiSeat does not meter or gate seat count by license.
- **No hardware lock-in.** Reinstalling Windows, swapping a CPU, or replacing a drive never requires "deactivating" anything first — there is nothing to deactivate.
- **No account or key required.** There is no purchase flow, no license key to enter, and nothing to lose if you lose an email or an order confirmation.

Instead of a EULA governing paid use, OpenMultiSeat is distributed under an open-source license. See the [`LICENSE`](../../LICENSE) file at the root of the repository for the exact terms (currently MIT), and see the project repository at **https://github.com/RKY2023/OpenMultiSeat** for the source code itself. In short: you may use, copy, modify, and redistribute OpenMultiSeat freely, subject only to preserving the license notice — there are no usage restrictions tied to seat count, commercial use, or renewal.

This is the single biggest practical difference from ASTER for anyone evaluating the two products: where ASTER's "Working with Licenses" guide walks you through activation keys, trial countdowns, and per-seat purchases, OpenMultiSeat simply has no equivalent workflow to document.
