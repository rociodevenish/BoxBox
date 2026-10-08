# Segment race model and controlled variability

Status: resolved

## Problem Statement

The dry-race laboratory demonstrates strategy trade-offs, but a weighted whole-lap trajectory cannot explain where a car gains through corners, loses on straights, catches another car or has a realistic opportunity to pass. Designers also cannot yet distinguish persistent driver performance variation from short-term noise. Recalculating trajectories must never create extra random opportunities or change results simply because the engine performs more calculations.

The next milestone must establish whether spatial track characteristics, tyre curves and controlled variability materially change our strategy conclusions, while measuring the cost of the more detailed model. Coefficients remain hypotheses, not final balance or real-world calibration.

## Solution

Extend the local laboratory with ordered track segments and segment-aware event resolution. Retain simultaneous decisions at Race Phase boundaries, deterministic hidden randomness, the existing observation boundary and paired strategy comparisons. Provide explicit, configurable scopes for race-long driver form, lap variation, overtaking, pit service and information error.

Publish reproducible experiments covering car/track fit, passing frequency, tyre and pit-window sensitivity, correlated driver performance and performance/memory costs. Use the results to identify what is ready for browser playtesting and what still needs modelling work.

## User Stories

1. As a designer, I want a track represented by ordered segments, so that straights and corners have distinct effects.
2. As a designer, I want segment lengths and baseline travel times, so that position and pace share a meaningful spatial model.
3. As a designer, I want independent Speed and Cornering demands per segment, so that different cars gain in different places.
4. As a designer, I want equal overall lap pace with different segment strengths, so that local advantages can create catches without inventing an overall pace advantage.
5. As a designer, I want segment-specific passing opportunities, so that a catch does not automatically permit an overtake everywhere.
6. As a strategist, I want a faster car to follow through a restricted section and attempt a pass in an eligible zone, so that track layout affects racecraft and pit strategy.
7. As a designer, I want catch predictions limited to the next trajectory-changing event, so that a straight-line speed advantage is not projected through a corner.
8. As a designer, I want tyres to wear in proportion to distance and configurable segment demands, so that extra segment boundaries do not create extra degradation.
9. As a strategist, I want Pace Instructions to apply across segments and after stops, so that the existing decision model remains understandable.
10. As a strategist, I want Attack and Defend to influence actual eligible encounters, so that they do not consume tyre condition when no battle occurs.
11. As a strategist, I want pit exit and rejoining traffic resolved at a defined track position, so that early stops can genuinely gain or lose through traffic.
12. As a designer, I want lapping and Classification to remain correct across different segments, so that local progress does not corrupt race order or gaps.
13. As a designer, I want simultaneous crossings and segment events handled consistently, so that event ordering does not grant accidental advantages.
14. As a designer, I want exact replay from inputs, model version and seed, so that reported outcomes can be reproduced.
15. As a designer, I want a race-long driver form modifier distinct from lap variation, so that a driver can sustain a good or poor race without changing their base attributes.
16. As a designer, I want lap variation sampled once per driver and lap, so that calculation frequency cannot change performance.
17. As a designer, I want passing randomness attached to a defined encounter opportunity, so that repeated recalculation cannot reroll a failed pass.
18. As a designer, I want pit-service and estimate randomness documented separately, so that mechanical execution and information uncertainty are not confused with pace.
19. As a designer, I want all variability magnitudes configurable and zeroable, so that effects can be isolated in deterministic controls.
20. As a strategist, I want observations to exclude hidden form, exact hidden tyre condition and future outcomes, so that assistance uses legitimate team information.
21. As an analyst, I want paired seeds to retain corresponding driver form and lap noise across strategies, so that comparisons isolate strategy choices as far as possible.
22. As an analyst, I want random scopes and model versions in diagnostic replay inputs, so that changes in randomness can be distinguished from tuning changes.
23. As a designer, I want tyre-curve and warm-up sweeps, so that the fresh-tyre payoff is measured rather than assumed.
24. As a designer, I want pit-window sweeps on multiple track layouts and fields, so that conclusions do not depend on one selected stop lap.
25. As a designer, I want passing-frequency measurements, so that open tracks and restricted tracks produce defensible differences.
26. As a designer, I want comparisons with and without persistent form, so that its effect on strategy uncertainty and sustained performance can be assessed.
27. As an analyst, I want eligibility, completed distance, sample counts, seeds and variability reported, so that incompatible finishes are not treated as comparable time gains.
28. As an analyst, I want segment and encounter histories, so that the cause of gains, queues and passes can be inspected.
29. As a developer, I want simulation latency measured separately from report generation and retained history memory, so that server budgets are based on evidence.
30. As a designer, I want a concise explanation of supported outcomes and remaining limits, so that the next browser playtest has a clear purpose.

## Implementation Decisions

- Extend the existing domain configuration, race engine, paired comparison runner and report contracts. Keep simulation independent of networking, persistence and presentation. Preserve the high-level Race Phase entry point rather than exposing an internal integration API.
- Add the user-requested focused public segment simulation seam, shared by the race engine and diagnostic callers. Given validated segment configuration, immutable effective driver/tyre/pace inputs, already-resolved form/lap modifiers and current spatial state, it returns travel/progress projections and an optional catch prediction bounded by the next segment exit of either car. It owns segment trajectory rules, not race sequencing, strategy evaluation, random sampling or passing outcome resolution. Inputs and outputs carry explicit units; repeated calls are pure and deterministic. Do not maintain a separate diagnostic implementation of segment physics.
- A track contains a non-empty, ordered, repeating sequence of segments with stable identities, positive finite length and baseline time, bounded Speed/Cornering weighting, non-negative tyre demand and explicit passing eligibility/difficulty. Initial segment types describe straight or corner demands; they are abstractions, not a vehicle dynamics model.
- Validate the complete track and tuning before resolution. Define units in input/report documentation. Use physical cumulative distance internally and expose normalized lap progress for existing Classification consumers. Preserve time gaps using recorded trajectories rather than a track-average conversion.
- Use bounded piecewise trajectories. Resolve the earliest segment exit, catch, lap crossing, pit exit or Race Phase boundary; recalculate affected predictions after each event. A catch prediction is valid only until either car reaches its next trajectory boundary. A caught car follows without overlap until a valid passing opportunity or other event changes the constraint.
- Passing is permitted only in configured zones. Define an opportunity as an attacker/defender pair entering an eligible segment visit while in encounter range, or first catching within that visit. At most one draw per pair and opportunity; a persistent queue receives a new opportunity on a subsequent eligible visit, not on every recalculation. Blue-flag handling is explicit and separately reported.
- Segment pace distributes base driver Pace, Pace Instruction effects, tyre penalties, race form and lap noise by baseline travel-time share so their aggregate does not multiply with segment count. Speed and Cornering contributions follow each segment's demands. Splitting an otherwise identical segment must not create extra base pace, wear or randomness; passing opportunity definitions must explicitly preserve identity in equivalent subdivisions.
- Retain a one-segment compatibility mode for existing inputs. With new variability disabled, it preserves existing aggregate behaviour and strategy semantics. Version segmented replay inputs and outcomes; do not claim historical seeded results are interchangeable across model versions.
- Keep the current pit-entry semantics: a queued Pit Instruction takes effect at the driver's next lap end. Define the initial pit exit at the start/finish coordinate, with existing Pit Loss and stationary service; exit traffic is resolved spatially. Configurable alternate pit coordinates are deferred.
- Preserve five-leader-lap Race Phase boundaries for this laboratory, simultaneous decisions, stop eligibility and chequered-flag behaviour. Drain all events at a boundary timestamp before accepting the next phase's decisions, using documented stable tie handling.
- Tyre wear integrates distance and segment demand without dependence on event count. Use configurable existing degradation, exhaustion and warm-up controls; distribute warm-up across the first out-lap rather than charging it afresh per segment. Measure sensitivity before adopting different tyre-curve families.
- Introduce optional zero-centred bounded race form in seconds per lap, sampled once per driver at race initialization and retained throughout that race. Default its magnitude to zero for compatibility. Keep base driver attributes immutable. This proposal adds temporal correlation within a driver's race; it does not imply shared team or field-wide correlation.
- Keep independent zero-centred bounded lap variation once per driver/lap. The configured magnitude is the full range, matching the current contract. Distribute the cached value across segments; neither a phase change nor a recalculation samples again.
- Use named deterministic random channels keyed by race seed and stable semantic identities: driver/race for form, driver/lap for lap noise, driver/stop identity for pit service, attacker/defender/opportunity for passing, and driver/Race Phase for estimate error. No mutable global random stream or calculation-count keys. Diagnostic documentation states magnitude, distribution, sampling scope and consumers for each channel.
- Preserve observation error as bounded plus/minus configured error with condition clamping; repeated reads in the same Race Phase reuse the draw. Hidden values, seeds and draw records belong only in designer diagnostics, never team observations.
- Paired comparisons retain corresponding exogenous draws for stable identities; strategy changes may create or remove encounter/stop identities. Do not promise identical event sets or physical opponent outcomes. Summarize position and eligible equal-distance time separately.
- Extend histories with segment identity, spatial position and encounter identity where needed. Replay metadata includes explicit tuning, track layout, model/randomness contract version, seed and strategy inputs. Raw diagnostic randomness must not enter player-facing event flows.
- Supply equal and mixed 22-car fixtures on at least two layouts with comparable lap duration but different demands and passing zones. Include corner-strong versus straight-strong cars, zero-noise controls, baseline-versus-segment comparisons, tyre/warm-up sweeps, passing difficulty sweeps, pit-window sweeps and race-form/lap-noise isolation experiments.
- Use at least 20 published paired seeds for principal claims. Report means, sample variability, eligibility/distance groups and failed or inconclusive hypotheses. For sweeps, choose bounded ranges before interpreting results; record all selected settings rather than only favourable outcomes.
- Benchmark warm simulation-only phase resolution separately from report generation. Measure mean, p95 and maximum latency, allocated bytes and retained history/report sizes across increasing segment counts and full-distance 22-car races. Record runtime/hardware, seeds, workload and repetitions. Compare against the previous laboratory; do not infer multiplayer capacity from a local benchmark.

## Testing Decisions

- The user confirmed a focused public segment simulation seam in addition to the existing race engine and paired comparison runner. Test local segment travel, progress and boundary-limited catch predictions through that public contract; test complete integrated outcomes through race/phase outputs and paired comparisons.
- Test observable trajectories, events, Classification, decisions and observations. Avoid tests that duplicate private pace formulas or assert internal event-queue structures. Existing replay, traffic, tyre, pit, phase-boundary and comparison tests provide prior art.
- Deterministic fixtures demonstrate local corner gains, subsequent straight losses, a catch before a segment boundary, a projected catch invalidated at that boundary, restricted following and passing at the next eligible opportunity. Include both cars entering different segments and multiple nearby opponents.
- Verify pit re-entry, lapping, simultaneous timestamps, phase changes inside segments and final crossings in complete races. Verify distance-based wear and whole-lap effects remain invariant under equivalent segment subdivision.
- Verify fixed seed/input/version replay, repeated observation stability and recalculation stability through public behaviour. Disable each channel independently; race form persists across laps while lap noise differs by lap. Validate keyed draw reuse across corresponding paired runs and failure-to-pass reuse within one opportunity.
- Reject malformed track/tuning before mutation. Keep hidden form and future draws out of team observations. Regression-test the one-segment compatibility mode with all new variation disabled.
- Use complete 22-car races for integrated evidence and runner smoke checks for input validation, machine/readable outputs, replay metadata and reproducible experiment execution. Statistical evidence reports uncertainty; ordinary unit tests should not depend on one lucky random outcome or tight wall-clock thresholds.

## Out of Scope

- Continuous steering, braking, racing lines, collision geometry or real-world physics calibration.
- Mechanical Grip, high/low-speed Cornering attributes, braking and traction attribute expansion; segment demands provide a future extension point without implementing these systems now.
- Weather, track evolution, incidents, damage, reliability, Safety Cars, qualifying, opening-lap starts and early phase interruption.
- Multiplayer, cloud deployment, persistence, browser UI, season economy and presentation animation.
- Shared team/field-wide random factors, longitudinal form across race weekends and additional component/tyre randomness.
- Final balance, real circuit data and a guaranteed server capacity target.
- Proving the 20–25 minute player race duration without a browser playtest.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Track cards inform demands and passing characteristics; explicit validated segments, travel times and passing zones still need configuration. Keep deferred detailed attributes out of this milestone.

## Further Notes

Current implementation audit: base driver attributes are fixed throughout a race. There is no race-level form draw. Lap duration uses a deterministic seed/driver/lap draw with offset plus/minus half LapVariation; the runner normally sets LapVariation to 0.2 seconds, while the domain default is zero. Pit service uses seed/driver/completed stop lap with plus/minus half StopVariation (default 0.6 seconds). Passing uses seed/attacker/current lap/opponent; repeated encounters with that same opponent in the same lap therefore reuse the same draw, even if the chance changes. Tyre estimate error uses seed/driver/Race Phase with plus/minus EstimateError (default 0.04 condition), clamped to valid condition. Tyre wear itself is deterministic. Re-running calculations or reading observations does not advance a random stream.

The proposed passing opportunity identity deliberately refines that existing per-opponent/per-lap scope for location-specific passing. The proposed race-form channel is new, configurable and disabled by default. Every change must retain the locked hidden seeded randomness principle: identical initial inputs, decisions, seed and model version yield identical outcomes.

The previous evidence supports aggregate strategy behaviour, not corner-level realism. This milestone should explain which conclusions survive segmentation, which are sensitive to tuning or persistent form, and which remain unproven. A subsequent browser playtest should assess decision clarity, useful feedback and the intended player race duration.

## Completion evidence

All ten approved tickets are resolved on codex/segment-race-model. The final integration passes 108 public-boundary tests and runner replay/validation smoke checks. Published track/passing, tyre/pit, form/noise and benchmark evidence and the browser-playtest recommendation are under evidence/. The two-axis review found no actionable Standards issues and two Spec issues; both were corrected and independently verified. See review.md for findings, resolution and benchmark revision limits.

