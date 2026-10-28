# 02: Race with Intermediate and Wet compounds

> GitHub issue: [#98](https://github.com/rociodevenish/BoxBox/issues/98) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Choose wet-weather tyres and observe coherent suitability, pace, wear and risk trade-offs.

**Blocked by:** 01: Observe evolving weather and stable forecasts; incidents-safety-cars 01.

**Status:** resolved

## Acceptance criteria

- [x] Add Intermediate/Wet suitability, warm-up and degradation with bounded wrong-condition consequences; retain unlimited sets and existing service semantics.
- [x] Map supported Wet-Weather Skill, Tyre Temperature Management and traction/error effects through the effective model; avoid duplicate accident rolls.
- [x] Exempt a driver from two-dry-compound eligibility only when Intermediate/Wet is actually used in the race; forecast or qualifying use alone does not qualify.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Claim

Implemented by the ticket 02 worker on `codex/s10-02`, based on integration `bc07d73`.

## Answer

Explicit versioned wet tyre tuning adds continuous bounded suitability, base wear/warm-up and wrong-condition costs, supported detailed effective attributes and one existing integrated error channel. Absolute-time semantic cells preserve equivalent subdivision and actual process identity. Eligibility requires actual wet race use, including excluding a fitted set still in service. Nullable capability projections unlock full labelled compound controls only when supported. Live weather presets remain disabled. See [model/units/mappings](../../../docs/wet-tyres.md) and [verification and captures](../evidence/02-wet-tyres.md).

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
