# S09 rule and strategy evidence (ticket 06)

Engineering status: ready for human acceptance. Household comprehension, real decision-window duration, shared-device pacing and hosted acceptance remain pending. This report establishes bounded laboratory behavior; it does not calibrate real-world incident rates.

## Frozen experiment and reproducibility

`06-data/frozen.json.gz` contains **904 runs**, including full configuration, source profile versions, drivers, decisions, diagnostic scenarios where applicable, model/randomness metadata, phase histories, final distance/eligibility and designer diagnostics. `06-data/summary.json` contains arm summaries, seed-by-seed paired risk differences with both distance/eligibility outcomes, and matched lap-two strategy comparisons. These are two-car contrasting-circuit experiments, not full 22-car weekend frequency measurements.

- 768 risk runs: Monza and Monaco × seeds 101–164 (64 paired seeds) × six arms (Standard baseline, Conserve, Push, Reliability 1, Reliability 100, legal Attack/Defend). Each race has 12 laps and two cars. Repeated arms and driver outcomes are paired observations, not independent samples. Reliability extremes are explicit controls replacing the detailed source value 82; Error Control remains the frozen detailed driver input. The second car receives +10 Speed and a one-second initial offset to create actual encounters. Battle is applied only when publicly legal.
- 128 strategy runs: two circuits × seeds 101–116 (16 paired seeds) × green/neutralised continuation × target Pit Hard/Stay Out. A forced spin at 36s creates the identical starting interruption. The green counterfactual continues racing after committing the same legal choices; the neutralised arm uses actual Safety Car travel. These forced scenarios measure mechanisms and conditional strategy, never spontaneous deployment frequency.
- Eight isolated mechanism runs: each authored route × green/neutralised × Pit Hard/Stay Out, seed 1. Zero pace/tyre/random/traffic differences, unchanged Hard tyres, a retired remote trigger, and 600-second neutralisation isolate actual matched-distance stop loss. Four arms share one deterministic mechanism; they are not four independent trials.

Replay without content catalog access:

```powershell
dotnet run --project tools/DisruptionEvidence -p:NuGetAudit=false -- --replay .scratch/incidents-safety-cars/evidence/06-data/frozen.json.gz
```

Generate a new comparison into an empty output directory (existing nonempty destinations are rejected):

```powershell
dotnet run --project tools/DisruptionEvidence -p:NuGetAudit=false -- .tools/reports/new-disruption-evidence
```

The final exact replay passed all 904 runs. Frozen outcomes are checked by complete serialized equality, not summary agreement. Generator and replay are bounded by `tools/run-bounded.mjs --timeout-ms 60000`; saved process records are in `06-checks/`. Generation PID 29888 took 3.416s; final compiled replay records its own PID/elapsed duration. These are machine execution times.

## Mechanism diagnostics

Authored physical routes bypass 0.2 lap with 38s lane transit (Monza) and 0.15 lap with 32s transit (Monaco); stationary service remains 3s. No aggregate PitLaneLoss becomes transit, and imagery supplies no geometry. At the same target lap-two crossing:

| Route | Green actual Pit Loss | Neutralised actual Pit Loss |
| --- | ---: | ---: |
| Monza | 23.0s | 8.6s |
| Monaco | 21.5s | 10.7s |

The two literal public simulation regression cases independently check these numbers. Route elapsed time itself is **not** Pit Loss. Main-route avoided time and actual rejoin progress are included by the paired crossing measurement.

Existing 01–05 public-boundary evidence covers mid-segment, phase/lap boundary, stop, finish precedence, concurrent triggers, multiple retirement, equivalent subdivision/exposure, queued/ongoing pits, opening and durable retry/restart. Ticket 06 reuses those suites rather than duplicating them. See `01-local-incidents.md`, `02-actual-time-interruption.md`, `04-neutralised-running.md`, and `05-opening-recovery.md` for exact checks and limits.

## Empirical outcomes and uncertainty

Per 64 two-car races, observed local event consequence counts (lock-up, spin, contact per involved car, mechanical failure; retirement is not counted a second time):

| Circuit | Standard | Conserve | Push | Reliability 1 | Reliability 100 | legal Battle |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Monza | 96 | 72 | 141 | 99 | 96 | 130 |
| Monaco | 96 | 73 | 144 | 99 | 96 | 131 |

Standard therefore observed 1.50 consequences per two-car race; this is not deployment frequency. Standard had seven retirements per circuit, Conserve five, Push eleven, Reliability 1 eleven, Reliability 100 seven. Lower demand reduced observed consequences in this bounded sample; higher demand and legal battle increased them. Reliability 100 did not improve the Standard event total in these seeds. Individual channels, early retirement, changing exposure and paired dependence prevent treating totals as calibrated probabilities or guarantees. Full distance/eligibility groups and seed pairs remain in the summary, including retired/short-distance outcomes.

In the 16 paired strategy seeds, the target's mean lap-two added time was 24.75s green versus 0.98s neutralised at Monza, and 23.30s versus 0.98s at Monaco. These toy-field measurements include catch-up, traffic, tyre and restart effects; they are not isolated physical Pit Loss. Pit target final position was always second. Neutralised Stay Out finished first in nine seeds and second in seven; green Stay Out was always second. Pit Hard established raw compound eligibility while Stay Out did not. These outcomes demonstrate a track-position/eligibility trade-off, not universal optimal advice. The exact results depend on a deliberately small field and these coefficients.

## Explicit live activation and compatibility

Only after the lab output and saved-input replay succeeded, production composition gained an opt-in gate: `Disruptions__Policy=disruption-live-opt-in-v1` (equivalent configuration key `Disruptions:Policy`). Unset retains existing incident-free presets; unknown nonempty policy versions reject startup. The authored policy enables Monza and Monaco only, using the physical routes above and ErrorRate 0.0003/running-second, FailureRate 0.00003/running-second, ContactProbability 0.02/actual encounter, ComponentWearPerSecond 0.00001, and major interruption enabled. Other supplied tracks explicitly return off until their own physical route is authored. This is an experimental opt-in policy awaiting household acceptance, with simulation-enforced tuning bounds; it is not a claim that all supplied circuits support live disruptions.

New enabled openings snapshot `DisruptionPolicyVersion`, route, tuning and detailed source profiles in immutable configuration/input; retries/replays use those saved values rather than current environment configuration. The policy version is `disruption-live-opt-in-v1`; incident channel remains `local-incidents-v1`. Automatic-standing-start-v2 still uses Standard; historical v1 still uses Conserve. No new opening rules are invented, and old sessions restore their recorded inputs. The new optional version is omitted when null, preserving old incident-free serialization. Existing `+pit-route-v1` and incident model metadata distinguish enabled physics.

Source Error Control and Reliability are 1–100 detailed design points, not headline stars. Supplied driver/track schema/data versions are 1.0.0 / 2026.10.08-initial-balance; car source is car-spec/1.0.0/2026.10.08-initial-model. Track mechanical-stress ordinal 1/2/3/4/5 maps by an explicit authored hypothesis to 0.5/0.75/1/1.25/1.5. These source versions are frozen by `WeekendPresetCatalog.IncidentInputs` and opening profiles. Existing supplied image originals/provenance/runtime assets remain untouched.

## Browser and human evidence

The focused opening/disruption browser flow passed once after desktop compaction (54.7s automated test wall clock). It verifies refresh, all 22 authoritative rows, row 22 bottom ≤1080 and page width ≤1920; `06-public-fit-1920.png` is the actual 1920×1080 viewport, not a full-page image. Root inspected it: all rows, banner, event summary and table readable; development details may continue below. Root already inspected the complete active and retired 360px flows in ticket 05; no phone layout changed.

Configured neutralisation is 180 simulation seconds in strategy/live defaults; mechanism controls use 600. The reviewed special decision policy remains 90 seconds plus 30 seconds shared-device allowance. Neither configuration nor automated test runtime measures household response time. Actual household event-frequency assessment, decision timing, comprehension and shared-device usability remain explicitly pending. No automated result substitutes for them.

Focused completion checks: one live policy contract case, two literal route mechanism cases, exact replay of all904 saved runs, and one opening/disruption browser flow. The broad integration pass is owned by root and was not run here. Browser process/config observations are in `06-checks/browser.json`; API/Java/Chromium PIDs were not captured, so the record states that limit rather than inventing identities. The browser servers refused occupied ports and Playwright owned their lifecycle. Existing root-reviewed phone images remain the 05 evidence. `06-checks/artifacts.json` pins SHA-256 hashes of the frozen input archive, summary and reviewed viewport.
