# 01: Observe evolving weather and stable forecasts

**What to build:** View current circuit-wide conditions and uncertain forecasts that evolve on authoritative simulation time.

**Blocked by:** incidents-safety-cars 02; weekend-preparation 03.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Use bounded keyed rain/wetness/drying and circuit volatility shared across preparation and racing; choices, diagnostics, display and wall-clock waits cannot reroll or advance weather.
- [ ] Generate stable versioned probabilistic forecasts over stated horizons with bounded information-quality effects; repeated reads provide no fresh lucky estimate.
- [ ] Separate public current conditions from hidden future paths and team forecast confidence; present text distinctions and preserve all-dry regression mode.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
