# 01: Create a durable short campaign and first event

**What to build:** Create an authorised preset campaign with a short immutable calendar and start its first weekend.

**Blocked by:** weather-strategy 06; shared-sessions 06; race-continuity-ai-assistance 07.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Persist stable campaign/team/driver/event identities, ownership, preset cars/rosters and ordered versioned contrasting-track calendar; full-calendar option stays gated.
- [ ] Expose creation, preparation and active-weekend lifecycle/readiness through API and browser; concurrent or duplicate starts create at most one active event.
- [ ] Reuse legitimate preparation, membership, AI takeover and recovery rules; private preparation stays filtered and constructor identity grants no performance.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
