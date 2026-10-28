# 02: Complete Q2, Q3 and the final grid

> GitHub issue: [#42](https://github.com/rociodevenish/BoxBox/issues/42) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Advance eligible cars through qualifying and display a unique complete 22-car grid.

**Blocked by:** 01: Lock preparation and complete Q1.

**Status:** resolved

## Acceptance criteria

- [x] Q2 retains ten for Q3; eliminated groups retain their session-defined ordering and never gain later-session promotion.
- [x] Reject eliminated-driver/incomplete choices before mutation; every driver gets one grid slot, including tied/no-time attempts.
- [x] Persist session transitions/results/grid before broadcast; refresh, duplicate callbacks and restart cannot resolve twice or change recorded attempts.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented durable Q2/Q3 progression, complete eligible-team locks and a unique 22-car grid through public simulation/API operations. Q1 positions 17-22 and Q2 positions 11-16 retain their recorded groups; Q3 supplies positions 1-10. Saved histories, canonical Continue receipts, eligible-only barriers/timing, presentation wakeup generations and final-grid replay survive retry/restart without resampling. Eliminated/invalid/incomplete/stale commands reject before mutation.

Reviewed independent phone choices, eliminated teammate presentation, historical results and the complete public grid with supplied identities/fallbacks. Eight actual-emulator API tests, five public simulation tests, one complete three-session browser flow, Angular typecheck and frontend/backend builds pass. [Evidence, hooks and limits](../evidence/02/README.md); [command/PID/check manifest](../evidence/02/checks.json). This slice ends at `grid-ready`; ticket 04 adds automatic opening and ticket 03 expands policies. Integration owns broad regression.
