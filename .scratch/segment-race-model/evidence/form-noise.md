# Persistent form and independent lap-noise evidence

40-lap, 22-car races, paired seeds 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26; 20 preselected cases. Both 90-second layouts and unchanged equal-all/mixed-car-fit fields. Mixed-car-fit varies car strengths AND base driver Pace by up to ±0.2; other driver attributes are unchanged.

Full ranges in seconds/lap: zero (0/0), form-only (0.4/0), lap-only (0/0.2), combined (0.4/0.2), combined-high (0.8/0.4), written form/lap. Form is one bounded zero-centred draw per driver/race; lap noise one independent scoped draw per driver/lap. Stop variation and estimate error are zero; passing variation zero uses the fixed 0.5 threshold, so current 0.4 chance/0.4 difficulty produces no ordinary passes. Tyres and spatial traffic remain active and fixed. No mutable driver attributes or shared team/field factor is introduced.

Baseline legal stops: every driver to Hard after phase 4. Alternative moves driver-06 to phase 3. All corresponding exogenous diagnostic channel/driver/scope keys are compared exactly within each paired seed; mismatches fail the suite. Missing scopes caused by different distances/events are reported, not treated as mismatches. Passing scopes depend on actual encounters and are separate.

## Sustained effective performance

The diagnostic modifier is resolved form + lap offset, before tyres, pace and spatial constraints. Each seed's correlation pools adjacent completed-lap modifier pairs from the same driver/race; it is not the correlation of an individual constant series. Driver-centering removes each driver's race mean first. Constant data has undefined correlation, shown as n/a (never fabricated zero). Means and sample SD below summarize 20 independent seeded races, rather than treating every lap pair as an independent trial.

| Layout/field | Profile | Pooled adjacent correlation mean (SD) | Driver-centered correlation mean (SD) | Mean within-driver temporal SD seconds | Between-driver race-mean SD seconds | Target elapsed-lap SD seconds |
| --- | --- | --- | --- | --- | --- | --- |
| open/equal-all | zero | n/a | n/a | 0.000 (SD 0.000) | 0.000 (SD 0.000) | 3.839 (SD 0.000) |
| open/equal-all | form-only | 1.000 (SD 0.000) | n/a | 0.000 (SD 0.000) | 0.115 (SD 0.014) | 3.843 (SD 0.009) |
| open/equal-all | lap-only | -0.011 (SD 0.038) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.008 (SD 0.001) | 3.841 (SD 0.009) |
| open/equal-all | combined | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.115 (SD 0.014) | 3.845 (SD 0.008) |
| open/equal-all | combined-high | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.115 (SD 0.001) | 0.230 (SD 0.028) | 3.846 (SD 0.014) |
| open/mixed-car-fit | zero | n/a | n/a | 0.000 (SD 0.000) | 0.000 (SD 0.000) | 3.839 (SD 0.000) |
| open/mixed-car-fit | form-only | 1.000 (SD 0.000) | n/a | 0.000 (SD 0.000) | 0.115 (SD 0.014) | 3.835 (SD 0.010) |
| open/mixed-car-fit | lap-only | -0.011 (SD 0.038) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.008 (SD 0.001) | 3.840 (SD 0.011) |
| open/mixed-car-fit | combined | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.115 (SD 0.014) | 3.837 (SD 0.015) |
| open/mixed-car-fit | combined-high | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.115 (SD 0.001) | 0.230 (SD 0.028) | 3.844 (SD 0.022) |
| restricted/equal-all | zero | n/a | n/a | 0.000 (SD 0.000) | 0.000 (SD 0.000) | 3.839 (SD 0.000) |
| restricted/equal-all | form-only | 1.000 (SD 0.000) | n/a | 0.000 (SD 0.000) | 0.115 (SD 0.014) | 3.845 (SD 0.007) |
| restricted/equal-all | lap-only | -0.011 (SD 0.038) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.008 (SD 0.001) | 3.841 (SD 0.009) |
| restricted/equal-all | combined | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.115 (SD 0.014) | 3.844 (SD 0.009) |
| restricted/equal-all | combined-high | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.115 (SD 0.001) | 0.230 (SD 0.028) | 3.847 (SD 0.017) |
| restricted/mixed-car-fit | zero | n/a | n/a | 0.000 (SD 0.000) | 0.000 (SD 0.000) | 3.835 (SD 0.000) |
| restricted/mixed-car-fit | form-only | 1.000 (SD 0.000) | n/a | 0.000 (SD 0.000) | 0.115 (SD 0.014) | 3.835 (SD 0.002) |
| restricted/mixed-car-fit | lap-only | -0.011 (SD 0.038) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.008 (SD 0.001) | 3.838 (SD 0.009) |
| restricted/mixed-car-fit | combined | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.058 (SD 0.001) | 0.115 (SD 0.014) | 3.837 (SD 0.008) |
| restricted/mixed-car-fit | combined-high | 0.783 (SD 0.046) | -0.032 (SD 0.039) | 0.115 (SD 0.001) | 0.230 (SD 0.028) | 3.842 (SD 0.018) |

Elapsed-lap duration includes tyre changes, traffic and Pit Loss. It is a confounded outcome, not an estimate of the injected random channels. Form-only produces sustained driver offsets with zero within-driver diagnostic variation; lap-only changes by lap; combined offsets retain a persistent component even after independent lap variation is added. Driver-centered results help distinguish that persistent component from independent temporal variation. The summary publishes every seed and sample count, including undefined correlations.

## Strategy distributions and uncertainty

Alternative minus baseline; negative time/position delta is favourable. Intervals are descriptive paired normal approximations for the mean, not balance or enjoyment claims.

| Layout/field | Profile | Eligible time delta mean (SD; range), seconds | Approx. 95% mean interval | Verdict | Eligible position delta mean (SD) | Time samples / groups |
| --- | --- | --- | --- | --- | --- | --- |
| open/equal-all | zero | 2.34 (SD 0.00; 2-2) | [2.344, 2.344] | Earlier stop slower in this sample | -5.000 (SD 0.000) | 20/1 |
| open/equal-all | form-only | -0.51 (SD 2.38; -5-3) | [-1.560, 0.530] | Inconclusive | -2.550 (SD 1.701) | 20/1 |
| open/equal-all | lap-only | 2.23 (SD 0.17; 2-2) | [2.154, 2.303] | Earlier stop slower in this sample | -4.700 (SD 0.470) | 20/1 |
| open/equal-all | combined | -0.79 (SD 2.58; -6-3) | [-1.922, 0.338] | Inconclusive | -3.000 (SD 1.376) | 20/1 |
| open/equal-all | combined-high | -5.12 (SD 4.90; -14-2) | [-7.262, -2.970] | Earlier stop faster in this sample | -3.750 (SD 1.410) | 20/1 |
| open/mixed-car-fit | zero | 1.09 (SD 0.00; 1-1) | [1.087, 1.087] | Earlier stop slower in this sample | -1.000 (SD 0.000) | 20/1 |
| open/mixed-car-fit | form-only | 0.35 (SD 2.76; -5-4) | [-0.861, 1.559] | Inconclusive | -2.400 (SD 1.667) | 20/1 |
| open/mixed-car-fit | lap-only | 1.34 (SD 1.17; 0-4) | [0.825, 1.851] | Earlier stop slower in this sample | -0.750 (SD 0.444) | 20/1 |
| open/mixed-car-fit | combined | 0.20 (SD 2.67; -5-4) | [-0.972, 1.372] | Inconclusive | -2.850 (SD 1.531) | 20/1 |
| open/mixed-car-fit | combined-high | -3.67 (SD 5.40; -14-4) | [-6.037, -1.300] | Earlier stop faster in this sample | -3.000 (SD 1.622) | 20/1 |
| restricted/equal-all | zero | 2.34 (SD 0.00; 2-2) | [2.345, 2.345] | Earlier stop slower in this sample | -5.000 (SD 0.000) | 20/1 |
| restricted/equal-all | form-only | -0.86 (SD 2.52; -5-3) | [-1.962, 0.244] | Inconclusive | -2.800 (SD 1.824) | 20/1 |
| restricted/equal-all | lap-only | 2.23 (SD 0.17; 2-2) | [2.155, 2.304] | Earlier stop slower in this sample | -4.700 (SD 0.470) | 20/1 |
| restricted/equal-all | combined | -0.47 (SD 2.43; -6-3) | [-1.532, 0.597] | Inconclusive | -2.850 (SD 1.387) | 20/1 |
| restricted/equal-all | combined-high | -4.60 (SD 4.85; -14-2) | [-6.723, -2.475] | Earlier stop faster in this sample | -3.800 (SD 1.824) | 20/1 |
| restricted/mixed-car-fit | zero | 1.44 (SD 0.00; 1-1) | [1.436, 1.436] | Earlier stop slower in this sample | -1.000 (SD 0.000) | 20/1 |
| restricted/mixed-car-fit | form-only | 0.16 (SD 2.86; -5-5) | [-1.098, 1.410] | Inconclusive | -2.350 (SD 1.663) | 20/1 |
| restricted/mixed-car-fit | lap-only | 1.29 (SD 0.40; 1-2) | [1.113, 1.468] | Earlier stop slower in this sample | -1.000 (SD 0.000) | 20/1 |
| restricted/mixed-car-fit | combined | -0.07 (SD 3.35; -7-5) | [-1.537, 1.397] | Inconclusive | -2.300 (SD 1.625) | 20/1 |
| restricted/mixed-car-fit | combined-high | -3.65 (SD 5.37; -15-5) | [-6.004, -1.295] | Earlier stop faster in this sample | -2.900 (SD 1.586) | 20/1 |

The machine summary includes all eligibility/distance groups, actual on-track passes, attempts, traffic losses and blue flags, every magnitude/configuration and matched/unmatched draw counts. Increasing variability can change queues and encounters; strategy outcomes need not vary monotonically. A nonzero point estimate whose interval crosses zero is inconclusive. Corresponding draws stay stable even when strategy changes the event set.

## Interpretation boundary

Persistence exists within one driver's race only. The next seeded race samples another scope; this model supplies no latent factor shared by teammates/field and no form persistence across weekends. Finite sample correlations between different drivers do not establish a shared factor. Form diagnostics are designer replay data and do not enter team observations or player event flows. Coefficients remain bounded modelling hypotheses.

Reproduce: `dotnet run -c Release --project tools/SimulationRunner -- --suite form-noise --runs 20 --output reports/segment-form-noise`. Each case exports replay inputs and seed 7's complete final spatial/diagnostic history. Regenerate other histories with --replay. Two cases execute concurrently; published case and seed order is deterministic.

