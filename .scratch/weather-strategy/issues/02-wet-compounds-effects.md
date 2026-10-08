# 02: Race with Intermediate and Wet compounds

**What to build:** Choose wet-weather tyres and observe coherent suitability, pace, wear and risk trade-offs.

**Blocked by:** 01: Observe evolving weather and stable forecasts; incidents-safety-cars 01.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Add Intermediate/Wet suitability, warm-up and degradation with bounded wrong-condition consequences; retain unlimited sets and existing service semantics.
- [ ] Map supported Wet-Weather Skill, Tyre Temperature Management and traction/error effects through the effective model; avoid duplicate accident rolls.
- [ ] Exempt a driver from two-dry-compound eligibility only when Intermediate/Wet is actually used in the race; forecast or qualifying use alone does not qualify.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
