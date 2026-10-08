# Scoped randomness contract

Replay names are exposed by `SimulationVersions`. Segmented runs use `segment-race-v1` and `scoped-random-v2-segment`; aggregate runs use `aggregate-race-v1` and `scoped-random-v2-aggregate`. A seed alone does not identify outcomes across model versions.

Every channel uses a pure stable keyed hash of race seed, driver identity, semantic scope and channel tag. There is no mutable stream. Form is resolved during construction, lap variation once at each driver's new lap, passing once per eligible opportunity, service once per actual stop, and estimate error is a pure per-phase value reused by observation reads. Effective resolved form and lap offsets reach the shared `SegmentSimulation.Project` seam; that seam never samples.

| Channel | Magnitude and distribution | Scope | Consumer |
| --- | --- | --- | --- |
| Race form | RaceFormVariation, full range in seconds/lap; uniform bounded zero-centred offset +/- half range; default 0 | Driver/race (scope 0) | Persistent whole-lap pace, distributed by segment baseline time |
| Lap variation | LapVariation, full range in seconds/lap; uniform offset +/- half range; default 0 | Driver/lap | One cached lap offset distributed by segment baseline time |
| Passing | PassingVariation, full threshold range in [0,1]; default 1 | Segmented: attacker/defender/eligible-zone visit; aggregate: attacker/opponent/lap | Compare cached threshold to resolved passing chance |
| Pit service | StopVariation, full range in seconds; uniform offset +/- half range | Driver/ordinal stop in segmented runs; driver/completed stop lap in compatibility aggregate runs | Stationary service duration |
| Estimate error | EstimateError, +/- condition fraction; uniform bounded error then clamp condition into [0,1] | Driver/Race Phase | Team tyre estimate |

Every magnitude can independently be zeroed. Form, lap, service and estimate offsets then become exactly zero. PassingVariation zero fixes the threshold at 0.5: chances strictly greater than 0.5 succeed and lower/equal chances fail. This is a deterministic control, with the configured chance retained; it does not mean passing is disabled. Intermediate passing ranges compress thresholds around 0.5 and are experimental controls, not calibrated probabilities. At default range 1 the threshold is the original unit draw. PassingChance/difficulty remain separate tuning inputs.

The stable hash retains historical lap, estimate, aggregate service and aggregate passing tags. Segmented service now uses the ordinal stop identity, so moving a corresponding stop to a different lap keeps its draw; introducing a stop changes later ordinal identities. This scope refinement is explicitly versioned. Segment passing keeps the validated pair/zone/visit identity, including equivalent subdivisions and wrapped zones. Blue flags have no passing draw.

Paired strategies retain form and corresponding driver/lap offsets. Corresponding segmented ordinal stops retain service draws; encounter sets can differ with strategy, and corresponding encounters reuse their identities. This does not guarantee identical traffic outcomes, completed distances or event sets.

Base Driver attributes remain immutable. Team observations expose legitimate Classification and bounded team tyre estimates; hidden form, exact tyre condition, seeds, draw values and cached future outcomes are absent. PassingAttempt events expose identity/location only, while Overtake is recorded at physical catch. Diagnostic replay metadata is separately versioned.
