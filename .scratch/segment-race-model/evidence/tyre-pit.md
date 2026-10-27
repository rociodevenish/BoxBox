# Segment tyre and pit-window sensitivity

40-lap, 22-car dry races, paired seeds 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26. 36 preselected cases; no coefficients selected from favourable outcomes. Two 90-second/5000-metre layouts and the unchanged equal-all/mixed-car-fit fixtures from ticket 07. Mixed-car-fit includes both alternate car strengths and up to ±0.2 base driver Pace; other driver attributes are unchanged.

Existing tyre curve only: default degradation per equivalent lap 0.025, exhaustion penalty 15 seconds/lap, warm-up magnitude 0.8 seconds. One-factor bounds: degradation 0.0125/0.05, exhaustion 0/30, warm-up 0/2. All other settings stay default and are explicit in machine output. Lap variation 0.2s, stop variation 0.6s, estimate error 0.04, form zero, passing variation one. Passing chance/difficulty stay 0.4/0.4.

Fresh-payoff controls compare all-Medium/no-stop baseline against driver-06's phase-4 Hard stop; baseline is ineligible and every opponent remains on its fixed no-stop script. These are diagnostic raw equal-distance comparisons, not eligible strategy wins. Pit-window cases use legal all-driver phase-4 Hard stops, moving only driver-06 to phase 3 or 5. The instruction takes effect at the next lap-end after the phase boundary; actual entry laps and spatial rejoin encounters appear in histories.

| Layout/field | Purpose/setting | Raw equal-distance delta mean (SD; range), seconds | Approx. 95% mean interval | Verdict | Target traffic loss mean B/A, seconds | Alternative out-lap mean, seconds | Repaid seeds / first repaid lap mean | Eligible time samples |
| --- | --- | --- | --- | --- | --- | --- | --- | ---: |
| open/equal-all | fresh-payoff/default | 3.87 (SD 1.91; -1-6) | [3.03, 4.71] | Alternative slower in this sample | 0.10/69.16 | 92.00 | 2/20; 39.00 | 0 |
| open/equal-all | fresh-payoff/degradation-low | 13.77 (SD 0.20; 13-14) | [13.68, 13.86] | Alternative slower in this sample | 0.10/0.02 | 92.00 | 0/20;  | 0 |
| open/equal-all | fresh-payoff/degradation-high | 1.34 (SD 2.07; -1-6) | [0.44, 2.25] | Alternative slower in this sample | 0.10/430.95 | 99.82 | 7/20; 37.14 | 0 |
| open/equal-all | fresh-payoff/exhaustion-off | 3.87 (SD 1.91; -1-6) | [3.03, 4.71] | Alternative slower in this sample | 0.10/69.16 | 92.00 | 2/20; 39.00 | 0 |
| open/equal-all | fresh-payoff/exhaustion-high | 3.87 (SD 1.91; -1-6) | [3.03, 4.71] | Alternative slower in this sample | 0.10/69.16 | 92.00 | 2/20; 39.00 | 0 |
| open/equal-all | fresh-payoff/warmup-off | 3.44 (SD 1.83; -1-6) | [2.64, 4.24] | Alternative slower in this sample | 0.10/69.93 | 90.80 | 2/20; 39.00 | 0 |
| open/equal-all | fresh-payoff/warmup-high | 3.78 (SD 1.83; -1-6) | [2.98, 4.59] | Alternative slower in this sample | 0.10/67.27 | 93.80 | 2/20; 39.00 | 0 |
| open/equal-all | pit-window/phase-3 | 2.25 (SD 0.14; 2-2) | [2.18, 2.31] | Alternative slower in this sample | 0.12/0.02 | 92.00 | n/a | 20 |
| open/equal-all | pit-window/phase-5 | 9.15 (SD 0.14; 9-9) | [9.08, 9.21] | Alternative slower in this sample | 0.12/0.02 | 92.01 | n/a | 20 |
| open/mixed-car-fit | fresh-payoff/default | 2.23 (SD 2.42; -2-5) | [1.17, 3.29] | Alternative slower in this sample | 4.91/72.40 | 90.10 | 5/20; 38.60 | 0 |
| open/mixed-car-fit | fresh-payoff/degradation-low | 11.61 (SD 1.31; 10-14) | [11.03, 12.18] | Alternative slower in this sample | 4.89/2.67 | 90.10 | 0/20;  | 0 |
| open/mixed-car-fit | fresh-payoff/degradation-high | -3.15 (SD 4.26; -12-2) | [-5.02, -1.28] | Alternative faster in this sample | 5.57/431.16 | 97.13 | 18/20; 33.28 | 0 |
| open/mixed-car-fit | fresh-payoff/exhaustion-off | 2.23 (SD 2.42; -2-5) | [1.17, 3.29] | Alternative slower in this sample | 4.91/72.40 | 90.10 | 5/20; 38.60 | 0 |
| open/mixed-car-fit | fresh-payoff/exhaustion-high | 2.23 (SD 2.42; -2-5) | [1.17, 3.29] | Alternative slower in this sample | 4.91/72.40 | 90.10 | 5/20; 38.60 | 0 |
| open/mixed-car-fit | fresh-payoff/warmup-off | 2.07 (SD 2.36; -2-6) | [1.04, 3.11] | Alternative slower in this sample | 4.80/73.33 | 88.90 | 5/20; 38.60 | 0 |
| open/mixed-car-fit | fresh-payoff/warmup-high | 2.19 (SD 2.21; -2-5) | [1.22, 3.16] | Alternative slower in this sample | 5.00/70.65 | 91.90 | 5/20; 38.60 | 0 |
| open/mixed-car-fit | pit-window/phase-3 | 2.60 (SD 2.08; -2-7) | [1.68, 3.51] | Alternative slower in this sample | 4.53/4.78 | 90.10 | n/a | 20 |
| open/mixed-car-fit | pit-window/phase-5 | 7.84 (SD 1.45; 5-10) | [7.20, 8.47] | Alternative slower in this sample | 4.53/3.13 | 90.11 | n/a | 20 |
| restricted/equal-all | fresh-payoff/default | 6.12 (SD 1.26; 3-8) | [5.57, 6.67] | Alternative slower in this sample | 0.10/71.41 | 92.00 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/degradation-low | 13.77 (SD 0.21; 13-14) | [13.67, 13.86] | Alternative slower in this sample | 0.10/0.02 | 92.00 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/degradation-high | 5.02 (SD 1.64; 2-7) | [4.30, 5.74] | Alternative slower in this sample | 0.10/419.62 | 92.00 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/exhaustion-off | 6.12 (SD 1.26; 3-8) | [5.57, 6.67] | Alternative slower in this sample | 0.10/71.41 | 92.00 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/exhaustion-high | 6.12 (SD 1.26; 3-8) | [5.57, 6.67] | Alternative slower in this sample | 0.10/71.41 | 92.00 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/warmup-off | 6.12 (SD 1.26; 3-8) | [5.57, 6.67] | Alternative slower in this sample | 0.10/72.61 | 90.80 | 0/20;  | 0 |
| restricted/equal-all | fresh-payoff/warmup-high | 6.12 (SD 1.26; 3-8) | [5.57, 6.67] | Alternative slower in this sample | 0.10/69.61 | 93.80 | 0/20;  | 0 |
| restricted/equal-all | pit-window/phase-3 | 2.24 (SD 0.16; 2-2) | [2.17, 2.31] | Alternative slower in this sample | 0.12/0.02 | 92.00 | n/a | 20 |
| restricted/equal-all | pit-window/phase-5 | 9.14 (SD 0.16; 9-9) | [9.07, 9.21] | Alternative slower in this sample | 0.12/0.02 | 92.01 | n/a | 20 |
| restricted/mixed-car-fit | fresh-payoff/default | 5.22 (SD 1.45; 0-8) | [4.59, 5.86] | Alternative slower in this sample | 5.50/75.99 | 90.10 | 0/20;  | 0 |
| restricted/mixed-car-fit | fresh-payoff/degradation-low | 11.95 (SD 1.00; 10-13) | [11.51, 12.38] | Alternative slower in this sample | 5.63/3.74 | 90.10 | 0/20;  | 0 |
| restricted/mixed-car-fit | fresh-payoff/degradation-high | 2.98 (SD 2.96; -4-6) | [1.68, 4.28] | Alternative slower in this sample | 5.72/423.16 | 90.10 | 5/20; 36.20 | 0 |
| restricted/mixed-car-fit | fresh-payoff/exhaustion-off | 5.22 (SD 1.45; 0-8) | [4.59, 5.86] | Alternative slower in this sample | 5.50/75.99 | 90.10 | 0/20;  | 0 |
| restricted/mixed-car-fit | fresh-payoff/exhaustion-high | 5.22 (SD 1.45; 0-8) | [4.59, 5.86] | Alternative slower in this sample | 5.50/75.99 | 90.10 | 0/20;  | 0 |
| restricted/mixed-car-fit | fresh-payoff/warmup-off | 5.28 (SD 1.15; 3-8) | [4.78, 5.79] | Alternative slower in this sample | 5.68/77.43 | 88.90 | 0/20;  | 0 |
| restricted/mixed-car-fit | fresh-payoff/warmup-high | 5.21 (SD 1.08; 3-8) | [4.74, 5.68] | Alternative slower in this sample | 5.64/74.32 | 91.90 | 0/20;  | 0 |
| restricted/mixed-car-fit | pit-window/phase-3 | 1.29 (SD 2.15; -1-8) | [0.35, 2.23] | Alternative slower in this sample | 5.96/4.92 | 90.10 | n/a | 20 |
| restricted/mixed-car-fit | pit-window/phase-5 | 7.45 (SD 0.86; 6-9) | [7.07, 7.82] | Alternative slower in this sample | 5.96/4.18 | 90.11 | n/a | 20 |

Alternative minus baseline: negative time is faster. Intervals are descriptive paired normal approximations, not final balance estimates. Every eligibility/distance group and both eligible and raw distributions are published, including empty eligible controls. Repaid means the first post-stop crossing with alternative time no later than the baseline at the same completed lap; unrepaid seeds are explicit. Out-lap excludes pit time and includes spatial traffic/warm-up. Mean repay lap includes repaid seeds only. It does not imply eventual finishing superiority.

## Spatial interpretation and limits

Both layouts have distance-weighted TyreDemand 1.0, with different local demands. The restricted layout has fewer passing visits; fresh-tyre gains can be trapped in queues after the start/finish rejoin. Target traffic loss, whole-field attempts/overtakes/blue flags and segment-labelled sample histories separate that mechanism from stationary Pit Loss. Warm-up is distributed once over the out-lap; its penalty can change encounter timing and opponents' outcomes. Identical stop scripts preserve scoped service draws across tuning cases. Changed events need not produce monotonic position results. The evidence measures these existing controls without adding curve families or final tuning.

Exhaustion-off/high at default degradation has no useful exhausted running before the 40-lap finish: condition reaches zero at the final crossing. The neutral result does not establish exhaustion strength; that sensitivity is inconclusive in this workload. High degradation does exercise earlier exhaustion, but this bounded suite does not cross it with every exhaustion setting. Warm-up responses can be non-monotonic because both initial tyres and rejoin encounters change; compare paired time variability and spatial histories rather than infer a universal payoff curve.

Reproduce: `dotnet run -c Release --project tools/SimulationRunner -- --suite tyre-pit --runs 20 --output reports/segment-tyre-pit`. Each case exports replay inputs and seed 7's final segment/encounter history. Regenerate other histories with --replay. Two cases execute concurrently; published case and seed order remains deterministic.

