# 02: Complete Q2, Q3 and the final grid

**What to build:** Advance eligible cars through qualifying and display a unique complete 22-car grid.

**Blocked by:** 01: Lock preparation and complete Q1.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Q2 retains ten for Q3; eliminated groups retain their session-defined ordering and never gain later-session promotion.
- [ ] Reject eliminated-driver/incomplete choices before mutation; every driver gets one grid slot, including tied/no-time attempts.
- [ ] Persist session transitions/results/grid before broadcast; refresh, duplicate callbacks and restart cannot resolve twice or change recorded attempts.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
