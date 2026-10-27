# S10 complete-weekend evidence and host activation

Automated evidence is complete; household acceptance remains **ready-for-human**. This report separates simulation outcomes, mechanism correctness and human observations.

## Frozen experiment

[Ten frozen groups](06/) contain **560 complete 22-car simulation weekends**: two contrasting supplied circuits (Monza and Monaco), five profiles, seeds 101–108 and seven paired arms. Each includes prepared neutral setup inputs for all eleven teams, Q1/Q2/Q3, a complete grid, explicit v3 starting compounds, the automatic opening and the race through final classification. The race distance is ten laps. These are full-field simulation weekends, not timed household playtests or evidence for a thirty-minute duration.

Arms are `observed` (baseline), `fixed-Medium`, `fixed-Intermediate`, `fixed-Wet`, `delay-one-window`, `quality-low` and `quality-high`. Opponents always use `weather-observation-ai-v1`. Only the two team-01 cars receive the fixed-compound/no-stop or first-proposed-stop deferral intervention. Delay skips the first proposed stop for each target driver, then resumes the policy; it does not invent a new pit opportunity. The fixed arms intentionally prohibit target stops and can be ineligible. The quality arms change information quality from 0 to 1, retaining the same physical inputs and observation-only choices.

There are **80 seeded weekend pair sets**, with seven dependent arms in each. Only **48 paths have stochastic weather**: all-dry and drying have zero volatility, so their eight race seeds repeat the same weather/forecast trajectory within a track/profile. Do not treat those repetitions as eight independent weather forecasts. The 22 drivers, two target cars, repeated issue times and two horizons are also not independent samples. Seed-level paired target-team means, ranges, descriptive approximate 95% t half-widths (7 df where eight seeds are available), individual target distance/eligibility groups and missing common-lap counts are retained in [comparisons.json](06/comparisons.json). These intervals are small-sample descriptive estimates, unadjusted for multiple comparisons; they do not establish final balance.

Every arm retains identical exogenous weather source identity, seed, process rates, interval timing and clock origin within its pair. The summarizer refuses a mismatch. Full effective driver/car/setup/circuit inputs and concrete operation choices are stored, so replay never opens the mutable catalog. Every complete engine result is checked against its full outcome hash. The compact final result retains classification, eligibility, tyres, observed state and lap/pit/compound events; discarded high-volume segment history is covered by those full hashes. The original full archive remains ignored under `.tools/reports/s10-06/raw/`, with generation/compaction manifests. Published groups total approximately 13.5 MB.

[summary.json](06/summary.json) retains full-field distance/eligibility groups, race clocks and raw paired results. [comparisons.json](06/comparisons.json) adds archive SHA-256 hashes, matched lap-nine timing, first pit-entry timestamps, per-seed forecast metrics and uncertainty. Matched timing requires both target cars to reach lap nine in both arms; missing pairs are excluded explicitly, not treated as zero. Classification and eligibility stay separate.

## Findings and limits

The authored profiles produce distinct regimes, without scripted future commands. Rain-arrival means rain can already be observed while meaningful surface wetness develops later. Weather is keyed to absolute simulation time; a strategy can change its arrival time at a lap, not the weather path at a given absolute time. Monza's mapped volatility is .24 and Monaco's .32; steady-wet scales these to .024/.032, while all-dry/drying use zero. All profiles and complete parameters are frozen in the groups.

- In all-dry Monaco, fixed Medium reaches matched lap nine about **26.22 seconds earlier** than the observed-policy baseline (descriptive half-width .64s), while Intermediate is **86.21s later** and Wet **179.57s later**. The faster no-stop dry arm is ineligible, so this is not a winning-strategy recommendation.
- At saturation, Wet is the baseline choice. Fixed Intermediate is about **169.76s later** at matched lap nine in Monaco (half-width 1.21s). Fixed Medium commonly misses that matched-distance comparison and remains separately grouped.
- In the drying Monaco weekend, fixed Intermediate matches the baseline, while Medium is **37.23s later** (half-width 1.22s) and Wet **47.87s later** (half-width .97s) at the common lap. This demonstrates a conditional trade-off, not a universal crossover wetness. The ten-lap baseline drying runs do not reach the outward dry reaction edge before finishing; the supplied dedicated mechanism/browser drying checks cover that edge. A longer event or different rate is needed to measure the full drying-to-dry strategy transition.
- Rain-arrival creates one baseline weather window per weekend on both tracks. Deferring the first proposed stop improves matched lap-nine time by **19.89s** on Monza (half-width 12.74s) and **15.65s** on Monaco (8.65s), with mixed signs for some seeds. Eligibility and actual stop times remain visible. This identifies a coarse-policy/timing tuning hypothesis, not an optimum stop window.
- Changeable Monaco averages **.5 baseline weather windows**, with a deferral position difference of **+.0625** (half-width 1.01 positions). Monza's sampled changeable baseline has no reaction window. Variation in rain/wetness does not guarantee a threshold crossing, and these samples do not support a universal changeable-weather timing benefit.

Forecasts are assessed at nine fixed issue times (0–960s every 120s), at 300s/900s horizons. Each quality arm has eighteen correlated forecast observations per seed. The diagnostics compare the estimate and uncertainty range with mean realized rain intensity sampled at fixed ten-second midpoints. This is an intensity proxy/envelope coverage check, **not calibrated probability coverage for a defined binary rain event**. The data retains observations, forecast identities, probabilities, lower/upper bounds and realized means.

Higher quality narrows ranges, but its error/coverage effect is not uniformly beneficial. For changeable Monaco at 300s, high-minus-low mean absolute intensity error is **−.0299** (half-width .0115), while range coverage falls **.0833** (half-width .0430). For steady-wet Monaco at 900s, error rises **.0185** and coverage falls **.7778** (half-width .0702). Seed-level rows and deterministic-path warnings are retained. All quality-arm race results are unchanged: the current policy uses observed suitability and does not make predictive stop choices. This experiment cannot claim a strategic benefit from better prediction, much less perfect forecasts.

## Rules, versions and production availability

The frozen rules are `weather-weekend-presets-v1`, `circuit-weather-v1`, `weather-forecast-v1`, `wet-tyres-v1`, `actual-race-wet-use-v1`, `weather-reactions-v1`, `qualifying-attempt-v2` / `qualifying-common-interval-v1`, `automatic-standing-start-v3` / `opening-keyed-v1` and `weather-observation-ai-v1`. Complete replay metadata/channel magnitudes appear in every run. Physics keeps `segment-race-v1` and `scoped-random-v2-segment+weather-sha256-v1`. No additional accident opportunity is added.

[Host activation](../../../docs/weather-host-activation.md) enables real complete weekends with `Weather__Policy=weather-weekend-presets-v1` plus one named `Weather__Profile`. The existing four public preset names remain unchanged. Unknown/incomplete settings fail startup. Defaults retain null weather, wet tyres and reactions. Preparation applies the profile to the **actual selected circuit**, preserves origin/quality, and freezes all three capabilities for every team through setup/practice/automatic preparation and qualifying. Q midpoints remain 90/270/450s from the frozen origin and the race begins at origin +540s. No player supplies a hidden future path, seed change or scenario command. Legacy opening v1/v2 and S09 staged-v1/travel-v2 replay dispatch remain unchanged.

The laboratory runs have no incidents or Safety Cars and do not prove incident balance. Existing public mechanism tests and S09 evidence cover actual pit-route neutralisation. Authored Safety Car routes remain restricted to the existing Monza/Monaco policy; weather activation does not infer or add pit geometry. All supplied preparation circuits can use weather, using their existing representative geometry and explicit volatility mapping. Legacy direct-session creation retains the existing segmented laboratory track and explicitly uses a Monza weather context, documented separately from circuit preparation.

Provisional driver/track versions remain `1.0.0/2026.10.08-initial-balance`, car `car-spec/1.0.0/2026.10.08-initial-model`, setup/effective mappings `wet-effective-v1`. Detailed wet-weather skill, tyre-temperature management and effective traction retain the existing documented mapping; no stars become physics. The current asset survey found the same 51 originals in two folders. Stable identity manifests, provenance/hashes, packaged variants and live text/fallbacks are unchanged. No image-derived geometry or new presentation assets were introduced.

## Verification and human acceptance

The existing [01](01-weather-model.md), [02](02-wet-tyres.md), [03](03-weather-reactions.md), [04](04-wet-qualifying-start.md) and [05](05-weather-advice.md) evidence is reused. In particular: actual mid-segment onset; falling thresholds; hysteresis/noise and minimum separation; queued/ongoing stops; coincident Safety Car and weather; repeated weather while neutralised without resetting its deadline; finish at trigger; subdivision/read stability; actual wet-use eligibility; common qualifying clock; accepted starting locks; forecast/trigger/result recovery with before/after-commit failures, retry and no duplicate advancement. These remain mechanism correctness checks, not the statistical evidence above.

No UI changed in 06. The reviewed 05 full 360px Guided phone flow and actual 1920×1080 mixed public viewport remain applicable. All 22 public rows, unified cause banner, observed events and mandatory control/committed-stop facts are readable; private forecast content remains absent. The phone flow is a long scroll. Existing wet/drying browser flows and real Auth/Firestore recovery results are recorded in 03–05; they are not counted as new checks here.

**Unmeasured human outcomes:** forecast understanding, added decision/presentation time, simultaneous-trigger comprehension and complete household duration. Automated wall-clock runtime and screenshot review do not substitute for these. Before accepting those criteria, a household should complete wet, drying and mixed-trigger weekends, distinguish observed conditions from uncertain predictions, explain a committed stop and merged cause window, and record decision/presentation intervals and comprehension observations. No participant or measured acceptance result is invented.

Focused 06 checks and exact replay results are recorded in the completion manifest below. Root owns the single broad verification and independent Standards/Spec reviews after integration.

## Reproduction

```powershell
# Generate only into a new/empty directory; never overwrite published groups.
dotnet run --project tools/WeatherEvidence -p:NuGetAudit=false -- .tools/reports/new-weather-evidence
node tools/WeatherEvidence/summarize.mjs .tools/reports/new-weather-evidence
# Replay published full inputs without catalog reads:
Get-ChildItem .scratch/weather-strategy/evidence/06/track-*.json.gz | ForEach-Object {
  dotnet run --project tools/WeatherEvidence --no-build -- --replay $_.FullName
}
node tools/WeatherEvidence/summarize.mjs .scratch/weather-strategy/evidence/06
```

Use pinned .NET10.0.401 and Node24.19.0. Local VSTest/build IPC needed escalation in this environment. Invocation-only NuGetAudit=false leaves the existing SignalR dependency advisory unchanged. No browser/emulator port was opened by 06, no new custom emulator configuration was needed, and no generic process kill, push or PR occurred. Runtime reports and original full archives remain ignored.

## Completion manifest

[completion.json](06/completion.json) records the bounded commands, exact group archive hashes, runtimes and launched PIDs. Unique new checks: **1 profile simulation test** and **15 host/coordinator API tests** passed. The latter includes ten activation cases and five actual-track prepared-weekend cases; the earlier helper's ten passing cases are not counted again. The initial profile test failed for the missing API, then passed. The helper's five activation cases also failed before registration existed, then passed. Prepared-weekend regression tests verify all eleven teams, an explicit setup revision, actual circuit provenance/capabilities and common Q1/Q2/Q3 origin.

All **560 saved weekends replayed exactly**, across ten sequential bounded group commands, without catalog access. Full Q results, opening hash, every operation's full outcome hash, compact final outcomes and fixed-time forecasts matched. Replay commands took **161.582 seconds** in total. Generation took **193.605 seconds** including serialization (simulation loop 159.8s); compaction took 27.168s. Published archives total **13,452,740 bytes**. These are machine runtimes, not player time. Simulation test PID33560 took 2.843s; host/preparation PID34844 took 7.378s. Generation PID19548 and compaction PID34820, plus every replay PID/time, are retained in the manifest. The transient full-history split was an archive transformation, not another simulation experiment. All commands completed and no owned network listener was opened or retained.
