# Segment track fit and passing evidence

40-lap, 22-car dry races; paired seeds 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26. All selected difficulty values: 0, 0.4, 0.8. No tuning was selected after inspecting outcomes.

Equal-all means identical drivers AND cars; mixed-car-fit alternates straight/corner car strengths and varies base driver Pace by up to 0.2 seconds/lap. Other driver attributes are unchanged. The first strength pair has equal overall clear-air pace. Both layouts have 90-second baseline laps, 5000 metres and integrated Speed/Cornering demand 0.5/0.5; open has two eligible visits and restricted one. PassingChance stays 0.4. Coefficients are hypotheses, not real-world calibration.

## How many actual on-track overtakes with equal cars?

- open: baseline 0.95 (SD 0.83; 0-2) overtakes/race; target early-stop alternative 2.15 (SD 0.93; 1-4). These count Overtake events only; pit-cycle rank changes and blue flags are excluded.
- restricted: baseline 0.50 (SD 0.69; 0-2) overtakes/race; target early-stop alternative 1.15 (SD 0.81; 0-3). These count Overtake events only; pit-cycle rank changes and blue flags are excluded.

Each distribution shows mean, sample SD and range across all published paired seeds. Attempts and queue loss are separate outcomes; an attempt is not an overtake. Aggregate attempt counts are unavailable, rather than zero.

| Layout | Field | Model/control | Difficulty | Baseline passes | Alternative passes | Attempts mean B/A | Queue loss seconds mean B/A | Blue flags mean B/A |
| --- | --- | --- | ---: | --- | --- | --- | --- | --- |
| open | equal-all | segmented | 0.0 | 1.40 (SD 1.05; 0-3) | 3.40 (SD 1.47; 1-6) | 853.90/855.30 | 3.48/18.45 | 0.00/0.00 |
| open | equal-all | segmented | 0.4 | 0.95 (SD 0.83; 0-2) | 2.15 (SD 0.93; 1-4) | 854.75/857.20 | 3.55/20.07 | 0.00/0.00 |
| open | equal-all | segmented | 0.8 | 0.25 (SD 0.44; 0-1) | 0.65 (SD 0.59; 0-2) | 853.75/854.85 | 3.79/22.09 | 0.00/0.00 |
| open | equal-all | zero-noise | 0.4 | 0.00 (SD 0.00; 0-0) | 0.00 (SD 0.00; 0-0) | 13.00/39.00 | 2.07/23.01 | 0.00/0.00 |
| open | equal-all | aggregate | 0.4 | 0.90 (SD 0.79; 0-2) | 2.10 (SD 1.17; 0-4) | n/a/n/a | 5.74/25.41 | 0.00/0.00 |
| open | mixed-car-fit | segmented | 0.0 | 51.20 (SD 6.11; 40-61) | 55.30 (SD 6.04; 44-66) | 921.25/927.70 | 113.83/124.80 | 0.00/0.00 |
| open | mixed-car-fit | segmented | 0.4 | 39.60 (SD 6.49; 27-56) | 44.25 (SD 8.53; 31-61) | 971.50/962.40 | 121.90/135.88 | 0.00/0.00 |
| open | mixed-car-fit | segmented | 0.8 | 22.35 (SD 3.66; 16-29) | 22.00 (SD 3.68; 14-27) | 1057.35/1042.50 | 133.56/141.01 | 0.00/0.00 |
| open | mixed-car-fit | zero-noise | 0.4 | 0.00 (SD 0.00; 0-0) | 0.00 (SD 0.00; 0-0) | 1180.00/1234.00 | 148.32/151.08 | 0.00/0.00 |
| open | mixed-car-fit | aggregate | 0.4 | 12.25 (SD 3.27; 8-18) | 13.15 (SD 2.72; 7-17) | n/a/n/a | 106.56/119.33 | 0.00/0.00 |
| restricted | equal-all | segmented | 0.0 | 0.95 (SD 1.19; 0-4) | 1.90 (SD 1.45; 0-5) | 424.30/425.85 | 3.51/20.71 | 0.00/0.00 |
| restricted | equal-all | segmented | 0.4 | 0.50 (SD 0.69; 0-2) | 1.15 (SD 0.81; 0-3) | 423.45/425.35 | 3.70/21.55 | 0.00/0.00 |
| restricted | equal-all | segmented | 0.8 | 0.05 (SD 0.22; 0-1) | 0.10 (SD 0.31; 0-1) | 422.70/423.15 | 3.91/22.65 | 0.00/0.00 |
| restricted | equal-all | zero-noise | 0.4 | 0.00 (SD 0.00; 0-0) | 0.00 (SD 0.00; 0-0) | 8.00/21.00 | 2.07/23.01 | 0.00/0.00 |
| restricted | equal-all | aggregate | 0.4 | 0.90 (SD 0.79; 0-2) | 2.10 (SD 1.17; 0-4) | n/a/n/a | 5.74/25.41 | 0.00/0.00 |
| restricted | mixed-car-fit | segmented | 0.0 | 39.65 (SD 4.94; 33-50) | 43.50 (SD 5.45; 36-53) | 478.95/481.95 | 136.44/147.12 | 0.00/0.00 |
| restricted | mixed-car-fit | segmented | 0.4 | 30.20 (SD 3.89; 21-37) | 32.10 (SD 4.29; 23-39) | 498.85/492.30 | 145.67/155.28 | 0.00/0.00 |
| restricted | mixed-car-fit | segmented | 0.8 | 15.60 (SD 2.21; 11-18) | 15.30 (SD 2.64; 11-19) | 550.65/538.30 | 151.64/158.54 | 0.00/0.00 |
| restricted | mixed-car-fit | zero-noise | 0.4 | 0.00 (SD 0.00; 0-0) | 0.00 (SD 0.00; 0-0) | 611.00/609.00 | 167.73/172.76 | 0.00/0.00 |
| restricted | mixed-car-fit | aggregate | 0.4 | 12.25 (SD 3.27; 8-18) | 13.15 (SD 2.72; 7-17) | n/a/n/a | 106.56/119.33 | 0.00/0.00 |

## Hypotheses and uncertainty

- Open minus restricted baseline on-track passes, equal-all: paired mean 0.450; sample SD 1.191; approximate 95% mean interval [-0.072, 0.972]. Inconclusive.
- Open minus restricted baseline on-track passes, mixed-car-fit: paired mean 9.400; sample SD 6.969; approximate 95% mean interval [6.346, 12.454]. Supported in this sample.
- Increasing difficulty reduces mean passes, open/equal-all: supported descriptively. Dynamic encounter sets mean individual races need not be monotonic.
- Increasing difficulty reduces mean passes, open/mixed-car-fit: supported descriptively. Dynamic encounter sets mean individual races need not be monotonic.
- Increasing difficulty reduces mean passes, restricted/equal-all: supported descriptively. Dynamic encounter sets mean individual races need not be monotonic.
- Increasing difficulty reduces mean passes, restricted/mixed-car-fit: supported descriptively. Dynamic encounter sets mean individual races need not be monotonic.

Intervals are descriptive normal approximations for paired count means, not final balance estimates. Twenty seeds can leave small effects inconclusive. Zero-noise also sets PassingVariation to zero: its fixed 0.5 threshold makes the current <=0.4 passing chances fail, while still exposing failed opportunities/queues. Blue flags remain explicit. Aggregate controls retain the same attributes, 90-second lap and 0.4 track chance but have no spatial passing zones; historical Fast/Technical tuning is a different workload.

## Strategy outcomes and comparability

- open-equal-all-segmented-0.0: eligible position delta -2.650 (n=20, SD=1.424); eligible equal-distance time delta 2.241s (n=20, SD=0.154); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-equal-all-segmented-0.4: eligible position delta -3.250 (n=20, SD=1.251); eligible equal-distance time delta 2.245s (n=20, SD=0.143); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-equal-all-segmented-0.8: eligible position delta -4.000 (n=20, SD=0.918); eligible equal-distance time delta 2.238s (n=20, SD=0.156); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-equal-all-zero-noise-0.4: eligible position delta -5.000 (n=20, SD=0.000); eligible equal-distance time delta 2.344s (n=20, SD=0.000); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-equal-all-aggregate-0.4: eligible position delta -3.150 (n=20, SD=1.694); eligible equal-distance time delta 1.969s (n=20, SD=0.461); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-mixed-car-fit-segmented-0.0: eligible position delta 0.350 (n=20, SD=2.641); eligible equal-distance time delta 3.486s (n=20, SD=1.690); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-mixed-car-fit-segmented-0.4: eligible position delta -0.600 (n=20, SD=2.891); eligible equal-distance time delta 2.596s (n=20, SD=2.080); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-mixed-car-fit-segmented-0.8: eligible position delta -2.650 (n=20, SD=1.843); eligible equal-distance time delta 0.899s (n=20, SD=1.592); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-mixed-car-fit-zero-noise-0.4: eligible position delta -1.000 (n=20, SD=0.000); eligible equal-distance time delta 1.087s (n=20, SD=0.000); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- open-mixed-car-fit-aggregate-0.4: eligible position delta -1.550 (n=20, SD=1.932); eligible equal-distance time delta 1.477s (n=20, SD=1.249); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-equal-all-segmented-0.0: eligible position delta -3.500 (n=20, SD=1.539); eligible equal-distance time delta 2.240s (n=20, SD=0.156); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-equal-all-segmented-0.4: eligible position delta -3.950 (n=20, SD=1.099); eligible equal-distance time delta 2.239s (n=20, SD=0.155); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-equal-all-segmented-0.8: eligible position delta -4.300 (n=20, SD=0.733); eligible equal-distance time delta 2.239s (n=20, SD=0.155); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-equal-all-zero-noise-0.4: eligible position delta -5.000 (n=20, SD=0.000); eligible equal-distance time delta 2.345s (n=20, SD=0.000); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-equal-all-aggregate-0.4: eligible position delta -3.150 (n=20, SD=1.694); eligible equal-distance time delta 1.969s (n=20, SD=0.461); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-mixed-car-fit-segmented-0.0: eligible position delta -1.000 (n=20, SD=2.103); eligible equal-distance time delta 2.216s (n=20, SD=1.743); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-mixed-car-fit-segmented-0.4: eligible position delta -2.400 (n=20, SD=1.930); eligible equal-distance time delta 1.293s (n=20, SD=2.147); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-mixed-car-fit-segmented-0.8: eligible position delta -2.400 (n=20, SD=1.635); eligible equal-distance time delta 0.940s (n=20, SD=1.270); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-mixed-car-fit-zero-noise-0.4: eligible position delta -1.000 (n=20, SD=0.000); eligible equal-distance time delta 1.436s (n=20, SD=0.000); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.
- restricted-mixed-car-fit-aggregate-0.4: eligible position delta -1.550 (n=20, SD=1.932); eligible equal-distance time delta 1.477s (n=20, SD=1.249); 1 eligibility/distance groups. Alternative minus baseline; machine summary publishes every group, including incompatible/ineligible groups.

Moving the target stop from the next crossing after phase 4 to phase 3 preserves corresponding driver/lap draws and ordinal stop draws. It can change encounter identities and opponents' physical outcomes. Position changes are separate from on-track passes and equal-distance time gains.

Reproduce: `dotnet run --project tools/SimulationRunner -- --suite track-passing --runs 20 --output reports/segment-track-passing`. Each case exports full comparison replay inputs and one seed's final spatial/encounter history; full histories can be regenerated with --replay. Summary includes explicit per-case tuning, shared fields/plans/seeds and all outcome groups.

