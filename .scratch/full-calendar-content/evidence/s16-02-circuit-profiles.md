# S16-02 immutable full-calendar circuit candidate

Content version: `release-2026-full-circuits-v1`. Calendar version: `official-2026-23-events-snapshot-2026.10.30-v1`. Profile version: `authored-four-sector-profiles-v1`. This is a NEW pack; `release-2026-pilot-v1` and the legacy short fixture retain their original factories, order and source contracts. Full mode requires this explicit complete pack; choosing full mode with a short pack is rejected. The initial owned package library still contains only the neutral baseline.

## Factual identity and authored simulation inputs

The [dated primary-source report](../research/provenance.md) fixes the revised 2026 23-event order, event names, weekend dates and circuit mapping at the 2026-10-30 snapshot. Bahrain is the event brand at physical Sepang in Malaysia; Madrid/Madring is separate from Barcelona-Catalunya; Saudi Arabia is excluded. `CampaignEvent` retains a campaign-local result identity plus a stable source-event ID, event name, physical track ID/name, dates and readable summary. Reopening the deployment catalogue is unnecessary on recovery.

The [four-input audit](s16-01-content-audit.md) still applies: all source schema/data versions and detailed 1–100 meanings are preserved. Driver/constructor identities are the season-opening contracted roster. The 23 supplied track lengths and demands remain provisional game inputs, not independently calibrated physical measurements. Kilometres convert once to metres; numeric totals are validated against that packaged conversion. Images never supply geometry, timings or rules.

Every row below is an explicit GAME hypothesis, not an official lap-time, pit-loss, incident or meteorological fact. Each track has four abstract model sectors: start/finish straight, fast complex, second straight and slow complex. They are not claimed to be surveyed circuit sectors or actual overtaking/DRS zones. Per-row distance/time shares are positive and sum to one. Existing `TrackSegment` fractions, `CircuitPreset.Demands`, `IncidentTuning`, `WeatherTuning`, `WetTyreTuning`, `WeatherReactionTuning`, `SafetyCarTuning` and `PitRoute` are reused; no second physics vocabulary or star-to-seconds mapping is introduced.

| Circuit | Baseline seconds | Distance shares | Time shares | Passing parameter | Nominal Pit Loss seconds | Stress coefficient | Weather hypothesis | SC seconds |
| --- | ---: | --- | --- | ---: | ---: | ---: | --- | ---: |
| `track-albert-park` | 92 | [.24,.30,.19,.27] | [.18,.34,.15,.33] | .48 | 21 | 1.00 | changeable | 150 |
| `track-shanghai` | 96 | [.29,.25,.22,.24] | [.20,.30,.16,.34] | .60 | 23 | 1.12 | rain-arrival | 150 |
| `track-suzuka` | 94 | [.21,.39,.17,.23] | [.16,.40,.13,.31] | .35 | 22 | 1.18 | changeable | 150 |
| `track-miami` | 93 | [.30,.23,.24,.23] | [.22,.27,.18,.33] | .55 | 22 | 1.05 | all-dry | 120 |
| `track-circuit-gilles-villeneuve` | 86 | [.29,.22,.26,.23] | [.21,.27,.19,.33] | .62 | 20 | 1.20 | rain-arrival | 150 |
| `track-monaco` | 83 | [.16,.26,.13,.45] | [.12,.27,.10,.51] | .12 | 20 | 1.04 | all-dry | 180 |
| `track-barcelona-catalunya` | 91 | [.25,.34,.16,.25] | [.18,.36,.12,.34] | .37 | 22 | 1.15 | all-dry | 150 |
| `track-red-bull-ring` | 76 | [.31,.22,.25,.22] | [.22,.28,.18,.32] | .62 | 19 | 1.16 | changeable | 120 |
| `track-silverstone` | 94 | [.23,.41,.16,.20] | [.17,.43,.12,.28] | .44 | 23 | 1.22 | rain-arrival | 150 |
| `track-spa-francorchamps` | 109 | [.28,.33,.22,.17] | [.20,.37,.16,.27] | .62 | 24 | 1.28 | changeable | 180 |
| `track-hungaroring` | 89 | [.20,.33,.13,.34] | [.15,.35,.10,.40] | .24 | 21 | 1.08 | all-dry | 150 |
| `track-zandvoort` | 85 | [.18,.39,.14,.29] | [.13,.42,.10,.35] | .26 | 21 | 1.17 | changeable | 150 |
| `track-monza` | 84 | [.33,.18,.30,.19] | [.23,.27,.21,.29] | .68 | 22 | 1.30 | all-dry | 150 |
| `track-madring` | 94 | [.26,.29,.19,.26] | [.19,.32,.14,.35] | .46 | 22 | 1.10 | all-dry | 150 |
| `track-baku` | 104 | [.36,.18,.21,.25] | [.24,.25,.15,.36] | .67 | 23 | 1.27 | all-dry | 180 |
| `track-sepang` | 102 | [.29,.29,.23,.19] | [.20,.34,.16,.30] | .57 | 23 | 1.25 | steady-wet | 180 |
| `track-marina-bay` | 103 | [.21,.24,.17,.38] | [.15,.27,.12,.46] | .28 | 25 | 1.22 | changeable | 180 |
| `track-cota` | 100 | [.26,.33,.21,.20] | [.18,.36,.15,.31] | .52 | 23 | 1.20 | all-dry | 150 |
| `track-mexico-city` | 87 | [.30,.22,.22,.26] | [.21,.28,.16,.35] | .57 | 22 | 1.24 | all-dry | 150 |
| `track-interlagos` | 82 | [.28,.28,.20,.24] | [.20,.32,.14,.34] | .59 | 21 | 1.14 | rain-arrival | 150 |
| `track-las-vegas` | 99 | [.38,.17,.25,.20] | [.26,.25,.17,.32] | .72 | 23 | 1.21 | all-dry | 150 |
| `track-lusail` | 88 | [.23,.41,.15,.21] | [.17,.43,.11,.29] | .37 | 22 | 1.32 | all-dry | 150 |
| `track-yas-marina` | 94 | [.29,.25,.22,.24] | [.20,.30,.16,.34] | .51 | 22 | 1.13 | all-dry | 150 |

## Supported mechanics and honest public descriptions

The two straight sectors have named passing-zone identities. Main-straight passing is available; the second straight is disabled on the low-passing profiles (parameter below 0.35). Corner complexes disallow passing. The passing parameter is an authored input to the existing encounter model, not a promised overtaking probability or an interpretation of illustration geometry. Speed/cornering/tyre sector demands are explicit existing fractions.

Pit service is 3 seconds. The authored pit route bypasses 12% of lap DISTANCE. Lane transit is `nominal Pit Loss + integrated baseline time of bypassed distance - stationary service`, integrating the actual nonuniform segment times. A public two-car worked example exposed the earlier whole-lap-time shortcut: Monza would have lost 25.054545 seconds while its card said 22. The corrected route produces the quoted nominal loss on all 23 profiles in controlled public races. Actual losses vary with car pace, compounds, weather, service variation and Safety Car conditions; the player summary therefore says typical green-flag loss. No source 1–5 pit-lane ordinal is silently converted to seconds.

Incident tuning uses explicit hazards/error/actual-encounter contact inputs: error 0.00002 per running second, failure 0.000001 per running second, contact parameter 0.0003 per encounter, default bounded component wear, and the row stress coefficient. Major incidents can open the existing Safety Car window; the row duration uses the existing neutralisation model. These are authored balance inputs, not claims of real incident frequencies. Source Safety Car Risk/Mechanical Stress ordinals are not direct probability/seconds conversions. Source Track Evolution remains inactive; no new track-evolution mechanic is invented.

Weather profiles reuse the existing public versioned weather operations. All-dry has zero rain/volatility/wetness. Changeable begins at intermediate wetness; steady-wet begins wet (Sepang). Rain-arrival explicitly uses mean rain 0.75, volatility 0.10, wetting 0.0015 per second and drying 0.00015 per second so the supported six-lap distance can reach a real dry/intermediate reaction; all four affected tracks exercise that transition. Other profile coefficients use existing WeatherWeekendProfiles hypotheses, including its explicit ordinal-to-volatility mapping, never headline stars. Forecast uncertainty and future rain draws remain hidden; circuit summaries expose only actionable passing, nominal Pit Loss and setup compromises.

## Validation, freezing and coverage

Campaign creation checks exact verified order/event-to-track/date mapping, all 23 profiles, all named references, explicit non-null pit/weather/wet-tyre/reaction/incident/SC contracts, source versions, group meanings and existing typed bounds before reserving the campaign. Every team reaches the public race constructor; one positive public progression per shared circuit validates time advancement without redundantly simulating the same profile eleven times. Complete 22-car races and replays are separate evidence. Missing circuit/profile, reversed order, wrong Bahrain venue, invalid weather and missing pit route are rejected. A corrected deployment can retry an unreserved command.

The campaign freezes event metadata, presets and conditions. Preparation consumes frozen conditions, and the opening cannot override a frozen full-profile policy with a mutable live deployment policy. Saved full profiles are validated through the public race constructor without reopening a deployment catalogue. An unsupported saved weather model fails as a non-destructive compatibility conflict; restoring the compatible saved inputs recovers the campaign. Legacy understood source-labelled short campaigns remain supported.

[Race coverage](s16-02-race-coverage.md) records complete 22-car configured races and forced Safety Car/pit scenarios, with serialized identical replay, for every circuit. [Focused verification](s16-02-verification.md) records API, browser, image-gap and persistence checks. Full-season economy/history growth and all 23 real campaign transitions remain #25 acceptance; this ticket does not claim that a manifest or initial save alone proves those. The integrated bounded lossless storage prerequisite is reused, with no history dropped.

## Current assets

All 51 original images were re-audited against the [stable asset inventory](s16-01-asset-inventory.json); hashes and the 23 track runtime paths are unchanged in both the worktree and current primary asset collection. All 23 supplied track illustrations are adopted through stable track IDs. Originals and existing WebP variants are preserved. No new bitmap processing or geographic-rights claim is introduced. Missing imagery produces a named neutral fallback while preserving event brand, circuit name, dates and summary. The six inspected phone/TV captures are published with this evidence.
