# 03: Review results and prepare asynchronously for the next event

> GitHub issue: [#10](https://github.com/rociodevenish/BoxBox/issues/10) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Inspect historical event reports, save next-circuit preparation and launch the next weekend when ready.

**Blocked by:** 02: Commit points and rebuild championship standings.

**Status:** resolved — claimed and implemented by s11_next_event on codex/s11-03

## Acceptance criteria

- [x] Calendar/report navigation explains points changes and upcoming circuit; saved drafts and practice obey existing one-assessment/final-lock policies.
- [x] Separate public readiness/results from private setup/advice; returning authorised players reclaim through existing boundary rules.
- [x] Advance cursor and create the next event once after readiness or a disclosed recorded missing-team fallback; restart between events preserves drafts and history.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented durable separately scoped next-event drafts, public readiness, authenticated returning-device reclaim, historical circuit reports and points deltas, CAS cursor advancement and exactly-once next-weekend creation. Assigned teams must be Final-Ready; existing unclaimed-team automatic policy is disclosed and recorded. Actual Auth/Firestore two-event/restart/concurrency/failure checks passed 9/9; the fresh-browser return flow passed 1/1. [Implementation, content mappings and evidence](../evidence/03-next-event.md).
