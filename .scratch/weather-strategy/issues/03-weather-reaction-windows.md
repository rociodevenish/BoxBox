# 03: React to significant weather changes

**What to build:** Stop at meaningful condition changes and submit simultaneous two-car strategy updates.

**Blocked by:** 02: Race with Intermediate and Wet compounds; incidents-safety-cars 04.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Detect suitability-band crossings at actual event time with hysteresis/minimum simulated separation; document weather/incident/boundary/finish precedence.
- [ ] Offer legal Pace, Pit/compound and contextual Battle choices; preserve queued/ongoing stops and next-lap-end timing.
- [ ] Merge coincident Safety Car/weather triggers into one window, including changes during neutralisation; reuse deadlines, fallback and commit-before-broadcast recovery.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
