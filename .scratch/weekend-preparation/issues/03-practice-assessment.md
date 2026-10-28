# 03: Run one practice assessment

> GitHub issue: [#105](https://github.com/rociodevenish/BoxBox/issues/105) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Confirm a provisional setup, receive one stable two-driver practice assessment and revise the final setup.

**Blocked by:** 02: Choose a team-wide Weekend Setup.

**Status:** resolved

## Acceptance criteria

- [x] Expose deterministic preparation/practice results with separate designer diagnostics; repeated requests and refresh return the same recorded assessment.
- [x] Engineer Setup and driver Setup Feedback affect justified confidence; feedback reveals no opponent secrets or future outcomes.
- [x] Practice changes no race tyres/components; allow final revision without further assessments or rerolls and present accessible confidence and compromises.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented public deterministic `WeekendPreparation.Assess(preset, laps, setup, seed)` over the same effective model, with legitimate circuit/two-driver handling feedback and separate designer diagnostics. Engineer Setup and driver Setup Feedback improve information/confidence only; Adaptability retains its existing preference-mismatch consumer. The versioned numerical confidence and uncertainty mappings are authored hypotheses documented in [Practice Assessment v1](../../../docs/practice-assessment.md), not calibrated performance or human-comprehension claims. Practice does not advance a race or consume tyres/components.

An authorised controller confirms the saved provisional setup once. Complete preserved source/configuration, setup, seed, result, model/content versions and canonical operation receipt are saved together before acknowledgement. Current authority and assignment are checked before retry lookup; stale revisions, invalid identities and cross-operation identity reuse reject atomically. Refresh, identical retry and actual durable-store server recovery return the same saved assessment. Candidate setup revisions remain available after assessment without changing the original feedback or permitting another assessment. Player/Open/SignalR projections omit the seed and designer diagnostics; public preparation omits assessment entirely.

The existing Angular preparation view now provides explicit confirmation/retry, both-driver fit/confidence/uncertainty and readable frozen-assessment versus final-candidate choices. Existing supplied identities, portraits and circuit fallback remain intact. [Focused test records and inspected phone/display captures](../evidence/ticket03/README.md) record three simulation tests, four API tests, one overlapping strengthened API rerun, one rendered browser path and Angular typecheck. Docker source packaging now includes the two embedded source JSON files. Final-Ready/freeze and AI guidance remain tickets 04/05; no hosted deployment, full regression or human acceptance is claimed here.
