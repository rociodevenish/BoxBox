# 04: Recover playback and adopt the full circuit assets

**What to build:** Reconnect to current state and show full-calendar circuits with robust renderer fallback.

**Blocked by:** 02: Play, skip and replay race highlights; 03: Switch Simple and Full views without changing play; full-calendar-content 04.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Recover subscriptions by authoritative revision/cursor, clear obsolete queues and ignore older/duplicate events without stale reconciliation.
- [ ] Map current/new supplied circuit art/geometry through stable identities with orientation/scale/provenance checks and consistent stylised fallback.
- [ ] Graphics/context/asset failure leaves decisions, DOM event summaries and exact tower usable; controllers stay lightweight and public/private filtering matches both views.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
