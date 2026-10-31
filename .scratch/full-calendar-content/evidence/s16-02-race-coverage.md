# S16-02 complete circuit race/replay coverage

All 23 circuits were exercised through complete 22-car, six-lap races using public Weekend Preparation, RaceEngine and SimulationReplay operations. Each configured race and forced-spin Safety Car/pit scenario was serialized/deserialized and replayed with an identical complete phase trace. Every public window advanced time and every race terminated within 200 windows. Classification retained all 22 entries; non-retired cars completed the distance and any retirement retained its retirement time. These are game-model acceptance cases, not real-world lap-time calibration or full-season economy acceptance.

The 46-case matrix passed. The four rain-arrival profiles were then strengthened after a genuine red showed that the old slow ramp did not reach a weather reaction during a six-lap race; all eight affected cases passed again. The table uses those newer results for China, Canada, Britain and São Paulo. After the distance-based pit-route correction, all 23 forced-pit cases passed again, alongside 23 controlled public two-car nominal Pit Loss cases. The table overlays those final forced-pit outcomes. The 19 unaffected natural races were not repeated; their inputs never request a pit stop.

| Round | Circuit | Configured race seconds | Weather reactions | Forced SC race seconds | Safety Car windows | Pit entry observed | Serialized replay |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 1 | `track-albert-park` | 556.694 | 0 | 641.919 | 1 | Yes | Identical |
| 2 | `track-shanghai` | 670.310 | 1 | 671.956 | 1 | Yes | Identical |
| 3 | `track-suzuka` | 568.464 | 0 | 651.521 | 1 | Yes | Identical |
| 4 | `track-miami` | 559.734 | 0 | 613.310 | 1 | Yes | Identical |
| 5 | `track-circuit-gilles-villeneuve` | 534.474 | 1 | 609.238 | 1 | Yes | Identical |
| 6 | `track-monaco` | 500.053 | 0 | 580.341 | 1 | Yes | Identical |
| 7 | `track-barcelona-catalunya` | 615.349 | 0 | 615.532 | 1 | Yes | Identical |
| 8 | `track-red-bull-ring` | 460.736 | 0 | 529.157 | 1 | Yes | Identical |
| 9 | `track-silverstone` | 706.757 | 2 | 776.474 | 3 | Yes | Identical |
| 10 | `track-spa-francorchamps` | 660.496 | 0 | 749.287 | 1 | Yes | Identical |
| 11 | `track-hungaroring` | 536.342 | 0 | 606.142 | 1 | Yes | Identical |
| 12 | `track-zandvoort` | 515.595 | 0 | 591.863 | 1 | Yes | Identical |
| 13 | `track-monza` | 506.318 | 0 | 575.222 | 1 | Yes | Identical |
| 14 | `track-madring` | 566.340 | 0 | 632.688 | 1 | Yes | Identical |
| 15 | `track-baku` | 701.826 | 0 | 707.636 | 1 | Yes | Identical |
| 16 | `track-sepang` | 618.356 | 0 | 796.699 | 1 | Yes | Identical |
| 17 | `track-marina-bay` | 623.882 | 0 | 729.814 | 1 | Yes | Identical |
| 18 | `track-cota` | 602.042 | 0 | 670.441 | 1 | Yes | Identical |
| 19 | `track-mexico-city` | 523.736 | 0 | 593.580 | 1 | Yes | Identical |
| 20 | `track-interlagos` | 508.030 | 1 | 581.477 | 1 | Yes | Identical |
| 21 | `track-las-vegas` | 596.024 | 0 | 662.990 | 1 | Yes | Identical |
| 22 | `track-lusail` | 530.052 | 0 | 598.229 | 1 | Yes | Identical |
| 23 | `track-yas-marina` | 566.335 | 0 | 632.596 | 1 | Yes | Identical |

Weather observations are active in every configuration. Dry, intermediate and wet starting states follow the authored profile; Sepang is the steady-wet example. Weather-reaction counts are public window counts, not forecasts or real-world weather likelihood. Forced scenarios use the supported diagnostic boundary only; their spin at 60 seconds is never installed in a campaign. Pit entries are reported as observed, because repeated public snapshots may contain the same event; these counts are not a pit-stop balance metric.

Bounded records and per-case TRX are preserved under `.tools/reports/s16-02/` outside the worker worktree. Matrix: `s16-02-all23-circuit-races.json`, dotnet PID 46220, 173,709 ms, 46 passed. Affected weather rerun: `s16-02-rain-arrival-regression.json`, 8 passed.
