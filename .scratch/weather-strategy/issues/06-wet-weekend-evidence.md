# 06: Verify complete weather weekends and crossover evidence

**What to build:** Publish replayable dry/wet/drying/changeable weekend evidence and actual clarity/pacing findings.

**Blocked by:** 05: Guide and recover weather decisions; incidents-safety-cars 06; qualifying-race-start 05.

**Status:** ready-for-human

## Acceptance criteria

- [x] Cover mid-segment rain, drying, hysteresis, queued stops, coincident Safety Cars, neutralised weather and finish-at-trigger with one legal window and no duplicate stops.
- [x] Publish paired-seed compound crossover, stop-window and forecast-quality comparisons on contrasting tracks, retaining uncertainty, versions and eligibility/distance groups.
- [ ] Complete browser wet/drying flows and recovery at forecast/tyre/trigger commits; measure forecast understanding and added decision/presentation time in actual weekends.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.


## Answer

Completed automated evidence and explicit real-host activation. [Report and frozen artifacts](../evidence/06-weekend-evidence.md) record 560 complete 22-car simulation weekends, all saved inputs, exact outcome replay, paired uncertainty, forecast coverage and eligibility/distance groups. Existing 01–05 browser and durable mechanism checks are reused; no UI changed. Defaults preserve null-weather replay behavior; the optional five profiles are host configured and actual-track preparation is frozen.

Human forecast understanding, added decision/presentation time and mixed-trigger clarity remain unmeasured. The browser portion of that criterion is evidenced in 03–05; its household measurement portion remains open. Status is ready-for-human, not a claim that every acceptance criterion is resolved.
