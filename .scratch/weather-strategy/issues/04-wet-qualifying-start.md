# 04: Complete wet qualifying and select starting tyres

**What to build:** Qualify in shared authoritative conditions and choose both starting compounds before automatic opening.

**Blocked by:** 02: Race with Intermediate and Wet compounds; qualifying-race-start 04.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Each qualifying session uses a documented common abstract time interval with no submission-order benefit and no new tyre/run-management game.
- [ ] Persist a simultaneous starting-compound step with disclosed fallback and grid, distinct from launch tactics; opening uses shared weather and incident rules.
- [ ] Present legitimate current conditions/forecasts and compound suitability; restart cannot change accepted tyres, conditions or qualifying attempts.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
