# Complete qualifying/start local evidence

Based on integrated `539bb563b5b332d0e6f8bc7b1fb55842c1632b91`. This closes ticket 05's local engineering work; actual household/external acceptance remains unchecked below.

## Frozen public-operation evidence

`tools/QualifyingEvidence` generates and independently replays complete **22-car, 11-team** weekends through public `Qualifying.Resolve`, `Qualifying.Grid`, `AutomaticRaceStart.Create` and that returned live engine's `ResolvePhase`. The four `track-*.json.gz` files contain 384 weekends: Monza/Monaco × equal/mixed authored fields × Safe/Push/All-Out × published seeds **101–132 inclusive**. Both fixed target drivers are Lando Norris and Oscar Piastri (`team-01-car-1/2`); all other drivers retain Push. Each target uses the selected approach while eligible in every stage. All eleven teams use neutral Weekend Setup and neutral authored car/engineer presets. The equal field retains distinct named identities but gives all drivers Norris's detailed ratings and preferences; mixed retains each supplied detailed driver card. No headline stars enter simulation.

Every compressed file is ordinary gzip JSON, self-contained for replay: source driver/car/circuit/engineer versions and detailed inputs, effective attributes and setup model, explicit configuration and race metadata, ordered eligible identities, rules/retain counts, concrete choices, attempt/session/seed, all recorded qualifying results, final grid, opening model/randomness/tuning/seed, full opening result, concrete Standard/no-pit first-phase plan and complete first Race Phase outcome. This is designer evidence containing hidden inputs, never a public player projection. Source/content is packaged `1.0.0/2026.10.08-initial-balance`; car source is `car-spec/1.0.0/2026.10.08-initial-model`; effective setup, qualifying, opening and engine versions are recorded in each file. Existing authored representative circuit segments are preserved; imagery supplies no geometry.

Replay reads saved bytes directly, with no `WeekendPresetCatalog.Open` call or mutable content lookup. It resolves all three recorded attempt inputs, rebuilds/validates all 22 unique contiguous grid slots, compares the exact saved opening grid, resolves the automatic opening at leader lap one and passes the **same engine** to the first normal Race Phase (leader lap six). It compares the resulting complete record against saved outcomes, including Classification, timing, tyres and events; no private formula is repeated. All **400 weekends** (384 risk cases plus 16 setup controls) passed a second-process saved-file replay. SHA-256 file hashes and bounded command manifests are in `checks.json`.

## Paired risk findings

[Machine-readable statistics](summary.json) preserve valid/no-time counts, conditional mean/min/max/population standard deviation and paired signs. The following values pool the two target drivers across 32 seeds, **64 Q1 attempts per approach per field/circuit**. Later sessions remain in the raw files; their eligible populations are selection-conditioned, so they are not pooled into an unconditional approach comparison. Units below are milliseconds. No-time is a simulated failed attempt with null time, distinct from an invalid command rejected before mutation.

| Field | Approach | Valid / no-time | Mean given valid | Min–max given valid | Population SD given valid |
| --- | --- | --- | --- | --- | --- |
| equal | Safe | 64 / 0 | 89052.75 | 88787–89259 | 134.11 |
| equal | Push | 60 / 4 | 88730.77 | 88038–89253 | 345.64 |
| equal | All-Out | 55 / 9 | 88576.18 | 87346–89491 | 612.07 |
| mixed | Safe | 64 / 0 | 89202.75 | 88820–89558 | 188.09 |
| mixed | Push | 60 / 4 | 88880.77 | 88113–89550 | 362.77 |
| mixed | All-Out | 55 / 9 | 88728.91 | 87479–89791 | 616.70 |

These rows apply separately to both circuits; they are **not additional independent samples**. Relative to Safe on the same driver/seed, Push is faster in 60/60 jointly valid pairs (mean −327.15 ms), with four pairs excluded for no-time. All-Out is faster in 45/55 and **slower in 10/55** (mean −482.69 ms), with nine excluded for no-time. Those ten reversals contradict a claim that All-Out always improves a valid lap. Its wider conditional spread and more no-times prevent its lower conditional mean being interpreted as an unconditional optimum. Zero Safe no-times in this finite set does not establish zero risk. These are descriptive paired observations, with correlated target-driver and shared-seed samples; no significance or calibrated probabilities are claimed.

The neutral car/setup causes identical target qualifying times across Monza and Monaco in this matrix. Thus this matrix is **inconclusive about circuit-specific approach differences**, rather than evidence of a general circuit balance. The effective fit consumer needs a changed setup to expose circuit sensitivity.

## Separate effective-fit control

Four `control-*.json.gz` files preserve 16 further complete weekends and [all concrete Q1 outcomes](controls.json). On seeds **101–104**, mixed supplied drivers, fixed Push choices and all other neutral teams, only target team 01's legal shared Aero setup changes: Speed bias **−2** versus Downforce bias **+2** (Mechanical/Tyres remain zero). Each circuit has eight jointly valid target pairs, no target no-times. Downforce minus Speed is **+840 ms in all eight Monza pairs** and **+164 ms in all eight Monaco pairs**. The smaller Monaco penalty demonstrates that effective circuit fit is consumed. There is **no sign reversal**: this authored qualifying mapping favors Speed in both tested controls. It would be incorrect to claim that Downforce wins Monaco, that a universal setup optimum was established, or that these representative 90-second layouts reproduce real circuit lap times. Coefficient balancing remains future work.

## Existing boundary coverage reused

The focused public simulation suite passed **12 tests**, including literal valid-before-no-time/equal-time fixed pre-order and literal full-22 fixed elimination-group Grid fixtures; invalid/incomplete/duplicate choices and incompatible versions; retry/permuted-input replay; full-field opening replay/invalid grid rejection; physical grid coordinates, actual passing, trailing progress, timing/tyre state and the same-engine first Race Phase. No duplicate private-formula tests were added. Existing real Auth/Firestore API tests retain preparation atomicity, complete-team locks/privacy, durable failure before/after commit, receipt retry, restart, deadline/pause/sweep, no resampling, stale/duplicate callbacks, frozen assistance and opening recovery coverage (tickets 01–04). Root owns one final integrated broad pass and independent reviews.

## Reproduction

From repository root with pinned .NET 10 SDK:

```powershell
dotnet run --project tools/QualifyingEvidence -- .tools/reports/qualifying-evidence
dotnet run --project tools/QualifyingEvidence -- --controls .tools/reports/qualifying-controls
dotnet run --project tools/QualifyingEvidence -- --replay .scratch/qualifying-race-start/evidence/05/track-monza-equal.json.gz
```

Repeat the last command for each of the eight saved gzip files. Generation accesses the packaged catalog once; replay never does. Use `tools/run-bounded.mjs` as recorded in `checks.json` for bounded commands. Test-scoped `NuGetAudit=false` is recorded for the pre-existing SignalR 10.0.12 advisory and does not remediate it. An initial tool compile error and a PowerShell-unquoted MSBuild `-m:1` invocation failed before their corrected commands passed; both manifests remain preserved.

## Acceptance still requiring people/external systems

- [ ] Observe actual household qualifying near four to five minutes, with separate decision/simulation/presentation/handoff measurements.
- [ ] Observe two-car comprehension, shared handoff comprehension and start readability with players.
- [ ] Observe full-weekend pacing, hosted deployment behavior and actual cost.

Automated elapsed durations and agent image inspection do not establish any of these. Incident interruption remains outside S08 until its own system exists. Browser evidence and current reviewed captures are recorded separately in [browser evidence](browser/README.md).
