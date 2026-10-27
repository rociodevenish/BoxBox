# S09 — Incidents, reliability and Safety Cars

Status: ready-for-human
Blocked by: weekend preparation and effective attributes; qualifying and automatic race start; durable session timing and recovery.

Implementation and independent review are complete. See [final integration evidence](evidence/final/README.md), including verification, opt-in track coverage and outstanding household/hosted acceptance.

## Problem Statement

Races currently run uninterrupted and Push has only pace/tyre consequences. Players cannot respond to mistakes, damage, failures or Safety Cars, and the server cannot stop a Race Phase at a strategically important event without risking incorrect progress, repeated decisions or lost replay consistency.

## Solution

Extend the laboratory and live weekend with contextual incidents and component reliability, an explicit interrupted Race Phase result and a simultaneous Safety Car pit window. Resolve actual timing and physical progress up to the interruption, preserve committed instructions and expose concise factual feedback before players react. Prove the rules and strategic effects in the laboratory before enabling them in browser play.

## User Stories

1. As a player, I want Push to increase appropriate error/component exposure, so that pace has consequences beyond tyres.
2. As a player, I want Conserve to reduce relevant exposure, so that restraint can protect a race.
3. As a player, I want mistakes to depend on driver and race context, so that events feel explainable.
4. As a player, I want Attack and Defend to affect actual encounter risk, so that battle choices have coherent costs.
5. As a viewer, I want lock-ups and spins distinguished from contact, so that race stories are clear.
6. As a player, I want minor damage to affect performance explicitly, so that I understand a slower car.
7. As a player, I want a retired driver marked clearly, so that I do not submit impossible choices.
8. As a player, I want component condition and Reliability to matter, so that car preparation has relevant effects.
9. As a player, I want estimated condition or risk rather than secret exact values, so that information remains fair.
10. As a viewer, I want local incidents to remain local when no interruption is needed, so that races are not constantly stopped.
11. As a player, I want a major incident to end the phase at its actual time, so that I can react immediately.
12. As a player, I want opening incidents to interrupt before normal Phase 1, so that the start follows the same rules.
13. As a viewer, I want exact Classification at interruption, so that animation cannot invent position changes.
14. As a player, I want Safety Car gaps compressed coherently, so that track position remains meaningful.
15. As a player, I want a quick Stay Out or Pit with compound window, so that I can respond to reduced Pit Loss.
16. As a player, I want Safety Car choices simultaneous, so that opponents cannot react to my lock.
17. As a player, I want queued and ongoing stops explained, so that I know which pit changes remain legal.
18. As a player, I want tyre/component use under Safety Car defined, so that neutralised running is predictable.
19. As a player, I want restart and remaining race distance clear, so that the next decision window is understandable.
20. As a host, I want deadlines and shared-device allowance for special windows, so that the household can respond fairly.
21. As an Assisted player, I want advice using observed incident and Safety Car facts, so that help remains legitimate.
22. As a host, I want AI to make legal disruption choices, so that unavailable teams keep racing.
23. As a player, I want interrupted races recoverable, so that a restart cannot repeat an incident or a stop.
24. As a player, I want retired and lapped results handled consistently, so that the finish remains understandable.
25. As a designer, I want individual risks disabled, so that effects can be isolated.
26. As a designer, I want replayable incident identities, so that recalculation cannot reroll an outcome.
27. As a designer, I want Safety Car strategy comparisons, so that reduced stop cost and track-position trade-offs have evidence.
28. As a household, I want event frequency and special-window duration measured, so that disruption does not overwhelm the weekend.

## Implementation Decisions

- Extend Game.Domain and Game.Simulation with component condition, damage/retirement status, incident events and a phase completion reason distinguishing normal boundary, Safety Car interruption and race finish. Session deadlines, persistence and playback remain outside simulation.
- Reuse the public race/phase contract and event-resolution engine. Resolve earliest event time, drain simultaneous events with documented stable tie rules and return every car's coherent shared-clock state at interruption; never resolve the planned phase first then roll it back.
- Define normal phase targeting explicitly by actual leader laps from each newly opened normal window, bounded by race finish. An interruption consumes only elapsed racing; a resumed normal window starts from current progress and receives the configured normal lap span. Safety Car windows are distinct decision types rather than ordinary phases with guessed durations.
- Add tunable context-driven risks for lock-ups/spins, contact during actual encounters and mechanical failures. Use Error Control, relevant Pace/Battle Instructions, tyre state, effective Reliability and track Mechanical Stress; do not add Aggression or random dramatic events unrelated to context.
- Model component wear over elapsed running/distance and operating demand, not event-loop calculations. Reliability moderates risk; Push/Conserve trade exposure against performance. Race-long condition starts from explicit preset values; season component carry-over is deferred.
- Use bounded, documented consequences: time loss, additional tyre wear, persistent performance-limiting damage or retirement. Ordinary pit stops change tyres, not silently repair damage; complex repair decisions and pit execution mistakes are deferred.
- Sample incident opportunities with stable semantic identities and named versioned channels. Lap/segment hazards must integrate exposure so equivalent subdivision/recalculation does not add risk; encounter contact uses a defined encounter opportunity. Zeroable channels support deterministic controls and explicit forced diagnostic scenarios outside player commands.
- Differentiate diagnostics from public events and team estimates. Show observed outcomes and justified condition/risk estimates without exact hidden wear thresholds, future failure draws or opponent diagnostics. AI and assistance operate on the same legitimate observations and legal choices.
- Trigger Safety Car only for configured severity/track criteria requiring neutralisation. Minor errors can continue within a normal phase. Document simultaneous competing triggers and finish precedence; if the race is already complete, do not open a new special decision window.
- On deployment, stop at the incident time and open one quick simultaneous Stay Out/Pit-plus-compound window for active teams. Preserve accepted choices/revisions, freeze AI from its starting observations and use disclosed deadline/fallback and shared-device policies.
- Preserve a queued Pit Instruction rather than silently cancel or duplicate it. Special-window commands may add a stop only where the engine's legal options permit; an already queued/ongoing stop remains committed and consumes no second tyre change. Retired cars require no instruction.
- Keep next-lap-end pit entry semantics. Pit Loss depends on actual neutralised travel timing plus stationary service, rather than a free arbitrary refund. Demonstrate substantially lower effective loss while retaining re-entry ordering and traffic coherence.
- Resolve a bounded configurable Safety Car period against the shared clock and a target following spacing, not instantaneous teleportation. Prohibit racing overtakes except documented pit/retirement/blue-flag ordering effects. Record elapsed laps, wear, gaps and any continuing failures.
- Initially keep lapped cars in their actual relative order without a separate unlapping procedure. Report lap deficits clearly and document this game simplification. Restart opens a normal decision window from authoritative current state; no extra player restart tactic is introduced.
- Finish may occur under Safety Car. Active drivers retain existing next-line-crossing finish semantics; retired cars receive explicit status, completed distance and retirement time, with stable ranking by distance/progress and documented tie handling. Preserve raw compound eligibility without claiming championships points are implemented.
- Route major opening incidents from the automatic-start operation into the same interruption/window flow before the first normal decisions. Preserve grid/start replay and avoid a separate opening Safety Car implementation.
- Persist committed incident/interruption outcomes, complete special-window choices and neutralised results before broadcast. Restart reproduces the same incident identities and continuation; duplicate callbacks cannot reapply damage, stops or gap compression.
- Extend the Broadcast Angular/Tailwind screens with incident status, retirement, Safety Car banner, special-window controls and concise event summaries. Use current supplied driver/track assets where appropriate, accessible status text and reduced-motion support.
- Publish deterministic rule scenarios plus bounded paired-seed experiments on contrasting circuits for risk, reliability, stop timing and track position. Record coefficients, sample counts, event frequency, eligibility/distance groups and uncertain conclusions; do not calibrate to claimed real-world rates.

## Testing Decisions

- User-approved main boundary: public session API/SignalR. Verify special-window commands, barriers, deadlines, lock preservation, payload filtering and durable recovery through observed behaviour.
- Extend the existing public simulation phase boundary for interruption, Safety Car and retirement checks; reuse automatic-start operations for opening incidents. Avoid private hazard/formula/event-queue assertions.
- Reuse segmented simultaneous-event, pit re-entry, lapping, Classification, replay and random-channel tests. Cover a trigger mid-segment, near lap/phase boundary, during a stop, at finish and with several cars retired.
- With deterministic scenario inputs, verify only elapsed progress resolves, damage is applied once, legal pit timing survives interruption and no double stop occurs. Check zero-risk compatibility and equivalent segment subdivision without extra exposure.
- Verify neutralised travel, restricted overtaking, actual reduced Pit Loss, lapped ordering, restart and finish under Safety Car through authoritative histories.
- Restart after incident commit, special locks and neutralised resolution; inject writes/callback retries and assert exactly one logical result and preserved control authority.
- A small browser flow handles an opening or mid-race Safety Car, both-car stop choices, reduced-motion events and return to a normal window, including retirement and refresh.
- Multi-seed reports support strategic/risk claims; manual playtests measure comprehensibility, disruption frequency and duration separately from server benchmarks.

## Out of Scope

- Red Flags, Virtual Safety Cars, penalties, protests, full real-world Safety Car/unlapping rules and collision geometry.
- Detailed damage repair, punctures, pit-crew operational mistakes and component inventories or season replacement penalties.
- Weather-triggered events until the weather spec, new player restart tactics and final incident calibration.
- Campaign economy/standings, full graphical animation and continuous driving physics.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Review driver consistency/error inputs, car reliability and circuit characteristics as candidate risk inputs. Incident rates, wear and neutralisation behaviour still need explicit tuning.

## Further Notes

This spec deliberately combines context-driven disruption with its first usable reaction window. Deliver risks and interrupted-state evidence before live Safety Car orchestration; later tickets can split those slices while preserving one phase contract. Some interruption and pit semantics refine the earlier uninterrupted dry-race milestone explicitly. The user approved the shared testing boundaries.
