# 01: Prepare a weekend with effective attributes

**What to build:** Open a preset weekend with circuit and two-car summaries whose supported effective attributes produce reproducible race behaviour.

**Blocked by:** race-continuity-ai-assistance 07; segment-race-model 07, 08, 09, 10; browser-dry-race 05 (reviewed evidence).

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Define units, bounds and supported/inactive consumers for the locked car and driver vocabulary; keep Simple groups separate from physics and map legacy fixtures/version compatibility explicitly.
- [ ] Use immutable preset car, driver, circuit and engineer inputs with neutral Base Car/package/setup context; apply effective inputs once, retaining replay sources and unchanged base inputs.
- [ ] Demonstrate contrasting circuit demands through public race outcomes without multiplying whole-lap gains by segment count; show readable summaries using supplied content.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
