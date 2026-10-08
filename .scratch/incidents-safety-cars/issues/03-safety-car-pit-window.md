# 03: Open a simultaneous Safety Car pit window

**What to build:** React to Safety Car deployment with quick legal Stay Out or Pit/compound choices for both active cars.

**Blocked by:** 02: Interrupt a Race Phase at the actual incident time; race-continuity-ai-assistance 03, 05, 06.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Open one identified window with shared-device allowance, disclosed deadlines/fallback and preserved accepted locks; retired cars require no choice.
- [ ] Queued/ongoing stops stay committed; legal choices cannot cancel, duplicate or silently replace them, and new stops retain next-lap-end entry.
- [ ] Freeze AI from legitimate observed facts; advice and public/private controls expose no hidden thresholds or future failures; show clear incident and stop status.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
