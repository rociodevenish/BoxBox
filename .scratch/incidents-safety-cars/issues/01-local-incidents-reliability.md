# 01: Resolve contextual incidents and reliability consequences

> GitHub issue: [#26](https://github.com/rociodevenish/BoxBox/issues/26) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Experience local mistakes, encounter contact and mechanical failures with explained damage or retirement.

**Blocked by:** weekend-preparation 01; qualifying-race-start 04; race-continuity-ai-assistance 06.

**Status:** resolved

## Acceptance criteria

- [x] Use Error Control, actual encounters, Pace/Battle Instructions, tyres, Reliability and circuit stress; sample stable versioned opportunities with zeroable channels and exposure invariant under subdivision.
- [x] Apply bounded time loss, tyre wear, persistent damage or retirement once; wear follows elapsed running/distance, and ordinary stops do not repair damage.
- [x] Show observed outcomes and justified team estimates, with explicit retired status and stable distance/progress ranking; retain zero-risk compatibility and separate hidden diagnostics.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented the opt-in `local-incidents-v1` public simulation laboratory: integrated versioned error/mechanical hazards, contact at actual encounters, elapsed running component wear, bounded local consequences and persistent damage/retirement. Public facts and team condition/risk bands exclude exact designer component/exposure diagnostics. Forced scenarios use a separate lab constructor/replay input. Detailed Error Control/Reliability and explicitly normalized ordinal Mechanical Stress are snapshotted with source versions.

Live configuration remains incident-free pending tickets 02–04. Lap-entry tyre/component context is the documented coarse risk approximation. Ticket 02 must add completion/interruption semantics at the existing earliest-event hook, drain simultaneous events and respect finish precedence; no phase rollback is needed. No Safety Car window, neutralised running, API/UI, imagery or durable session transition changed in this slice.

Verification: 11 incident public-operation tests passed; 47 distinct affected existing segment/traffic/pit/replay/start tests passed (58 unique tests). All 400 historical S08 v1 and 400 current v2 saved full-field runs replayed exactly. See [rules and units](../../../docs/local-incidents.md) and [verification evidence](../evidence/01-local-incidents.md). No hosted, household or real-world calibration claim.
