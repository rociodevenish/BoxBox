# 04: Complete wet qualifying and select starting tyres

> GitHub issue: [#100](https://github.com/rociodevenish/BoxBox/issues/100) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Qualify in shared authoritative conditions and choose both starting compounds before automatic opening.

**Blocked by:** 02: Race with Intermediate and Wet compounds; qualifying-race-start 04.

**Status:** resolved

Claimed by: s10_start_tyres, branch `codex/s10-04`, base `1709841becdf94b45a724b8cb3c446eb83d4f570`.

## Acceptance criteria

- [x] Each qualifying session uses a documented common abstract time interval with no submission-order benefit and no new tyre/run-management game.
- [x] Persist a simultaneous starting-compound step with disclosed fallback and grid, distinct from launch tactics; opening uses shared weather and incident rules.
- [x] Present legitimate current conditions/forecasts and compound suitability; restart cannot change accepted tyres, conditions or qualifying attempts.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented opt-in shared qualifying-attempt-v2 midpoint conditions, durable simultaneous typed starting-tyre locks after the complete grid commits, disclosed frozen AI/fallback, shared-device timing/pause/recovery, and automatic-standing-start-v3 with a separate immutable compound map and continuous shared weather origin. Dry qualifying v1 and opening v1 Conserve/v2 Standard remain compatible. See [runtime rules](../../../docs/wet-qualifying-start.md) and [verification/evidence](../evidence/04-wet-qualifying-start.md). Ordinary live weather remains disabled. Full advice and broader recovery remain ticket 05.
