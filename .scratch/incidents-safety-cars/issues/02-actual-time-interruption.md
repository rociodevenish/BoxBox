# 02: Interrupt a Race Phase at the actual incident time

**What to build:** Stop at a qualifying major incident with every car in coherent shared-clock state.

**Blocked by:** 01: Resolve contextual incidents and reliability consequences.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Resolve earliest events and simultaneous-event ties directly without resolving then rolling back; return normal, Safety Car or finish completion reasons.
- [ ] Preserve elapsed progress, committed queued/ongoing stops and authoritative Classification across mid-segment, lap-boundary, pit and finish scenarios.
- [ ] Minor events remain local; finish precedence prevents unnecessary windows; resumed normal windows target the configured lap span from current leader progress.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
