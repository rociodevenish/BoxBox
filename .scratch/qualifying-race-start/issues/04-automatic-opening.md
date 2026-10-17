# 04: Resolve the automatic opening into the race

**What to build:** Show automatic standing-start gains/losses and reach the first Race Phase with coherent authoritative state.

**Blocked by:** 02: Complete Q2, Q3 and the final grid; weekend-preparation 01.

**Status:** in-progress (codex/s08-04)

## Acceptance criteria

- [ ] Use grid, supported Starts/Racecraft/Error Control, launch fit and circuit inputs in the public seeded start operation; add no player launch decision.
- [ ] End at a documented common opening-lap boundary with coherent timing and spatial progress handed into the existing engine, without a separate time model.
- [ ] Commit once before display, reconcile exact Classification after skip/reduced motion, and limit outcomes to legal gains/losses until incidents arrive.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
