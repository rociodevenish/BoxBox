# 01: Prepare a weekend with effective attributes

> GitHub issue: [#103](https://github.com/rociodevenish/BoxBox/issues/103) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Open a preset weekend with circuit and two-car summaries whose supported effective attributes produce reproducible race behaviour.

**Blocked by:** race-continuity-ai-assistance 07; segment-race-model 07, 08, 09, 10; browser-dry-race 05 (reviewed evidence).

**Status:** resolved

## Acceptance criteria

- [x] Define units, bounds and supported/inactive consumers for the locked car and driver vocabulary; keep Simple groups separate from physics and map legacy fixtures/version compatibility explicitly.
- [x] Use immutable preset car, driver, circuit and engineer inputs with neutral Base Car/package/setup context; apply effective inputs once, retaining replay sources and unchanged base inputs.
- [x] Demonstrate contrasting circuit demands through public race outcomes without multiplying whole-lap gains by segment count; show readable summaries using supplied content.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented the approved public simulation slice: immutable versioned car/driver/circuit/engineer sources, neutral composition context, supplied detailed JSON catalog, readable circuit and two-car summary, and effective preparation/race boundaries. Complete units, bounds, supported/inactive consumers, explicit coarse mappings, legacy replay compatibility and asset provenance are recorded in [the runtime contract](../../../docs/effective-weekend-presets.md). Preparation maps only detailed ratings; races consume the resulting inputs without reapplying them. Preferences retain stable keys and live labels. The existing engine/replay contracts remain unchanged.

Five new public tests cover immutable source/saved JSON replay, contrasting circuit outcomes, content/effective model incompatibility, and segment-subdivision invariance. Together with affected existing race/segment/replay contracts, **26 tests pass**. [Focused check record](../evidence/01/focused-check.json); red examples [version rejection](../evidence/01/red-version-check.json) and [public race boundary](../evidence/01/red-public-race-check.json).

[Eight paired comparisons](../evidence/01/effective-weekend-v1.json) use 20 paired seeds (101–120), equal/mixed two-car fields and zero/noisy clear-air controls. Over 12 laps, the speed probe beats the corner probe at Monza by approximately **2.59 seconds**; the corner probe beats speed at Monaco by approximately **3.53 seconds**. Common driver randomness cancels in these controlled paired comparisons, so equal/noisy cases have the same mean. This validates the authored fit hypothesis, not factual calibration, traffic balance, setup optimality or player comprehension. Sources and effective inputs are included. [Runner record](../evidence/01/evidence-check.json).

This ticket adds no durable session transition or frontend interaction. Setup persistence/UI and browser phone/display review remain ticket 02; practice and lifecycle remain later tickets. Source images were inspected, originals preserved, and packaging boundaries documented; no unsupported imagery-derived geometry was introduced.

Verification uses SDK 10.0.401, isolated `.tools/artifacts/s07-01`, and bounded Node 24 launches. Sandbox-only VSTest could not connect to its local testhost; narrow authorized escalation completed all checks. NuGet vulnerability service was unavailable, so cached locked dependencies were restored with `NuGetAudit=false`; no pins changed. No UI/API/emulator listener was started, and existing previews were untouched. The integration branch's retained hosted/human evidence gate remains unchanged.
