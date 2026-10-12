# Effective weekend presets v1

`WeekendPresetCatalog.Open(circuitId, firstDriverId, secondDriverId)` opens supplied circuit and driver identities for a neutral two-car team. `WeekendPreparation.Prepare(preset, laps)` validates and snapshots the source and returns readable circuit/two-car `Summary`, immutable source ratings, and effective race inputs. `CreateRace(effective, seed)` consumes those inputs directly; it never applies the mapping a second time. There is no session transition in this slice. Setup choices, practice, persistence and qualifying lock belong to subsequent tickets.

The public simulation seam was approved in the parent specification. Effective JSON snapshots can be deserialized and raced without reopening the catalog or reading source files. Source content and effective coefficients are retained separately. Unsupported preparation content or effective model versions are rejected explicitly.

## Units and supported consumers

Every detailed car, driver and circuit demand input is a finite dimensionless design score in **1–100**. Car neutral reference is 82. Higher capability scores are better; higher demand scores mean greater importance, not a better circuit. They are not km/h, acceleration, force, wear percentages or measured talent. The mappings below are authored tuning hypotheses.

| Locked car vocabulary | Supplied key | v1 consumer |
| --- | --- | --- |
| Top Speed | topSpeed | Straight-segment gain |
| Acceleration / Energy Deployment | accelerationDeployment | Straight-segment gain; no explicit battery simulation |
| High-Speed Aero | highSpeedAero | Corner-segment gain |
| Low-Speed Aero | lowSpeedAero | Corner-segment gain |
| Mechanical Grip | mechanicalGrip | Corner-segment gain |
| Braking | braking | Corner-segment gain; no explicit braking zone trajectory |
| Traction | traction | Corner-segment gain; no explicit wheel-slip model |
| Tyre Preservation | tyreWear | Reduced existing tyre wear; **higher is better** |
| Tyre Temperature Management | tyreTemperature | Reduced wear under thermal demand; no explicit temperature state |
| Reliability | reliability | Inactive; no failures |

| Locked driver vocabulary | Supplied key | v1 consumer |
| --- | --- | --- |
| Race Pace | racePace | Whole-lap pace gain |
| Overtaking / Defending | overtaking / defending | Mean mapped to existing Racecraft passing comparison; not separate attack/defence ratings yet |
| Tyre Preservation | tyrePreservation | Existing wear reduction |
| Qualifying Pace | qualifyingPace | Inactive until qualifying |
| Starts | starts | Inactive until standing starts |
| Tyre Warm-up | tyreWarmup | Inactive; it is not silently substituted for preservation |
| Error Control | errorControl | Inactive until incidents |
| Wet-Weather Skill | wetWeatherSkill | Inactive until weather |
| Setup Feedback / Adaptability | setupFeedback / adaptability | Inactive until practice/compatibility |

Preferences remain immutable labelled compatibility traits. They do not produce a bonus in neutral preparation. Race Engineer Setup is a finite 1–100 information-quality score. The authored neutral engineer has Setup 82, version `authored-engineer-v1`; supplied content has no engineer collection. Engineer Setup is inactive here and never grants pace.

Simple car groups remain Speed, Cornering, Mechanical Grip, Tyres and Reliability. Simple driver groups remain Pace, Racecraft, Tyres, Consistency and Technical. Supplied headline composites/stars are presentation only. No headline value is read by this runtime mapping, and no Aggression attribute is introduced.

## Numerical hypothesis and circuit mapping

For each active car pace dimension, the contribution is `(rating - 82) / 10 * detailedCircuitDemand / 100`. The straight coefficient is the mean of Top Speed and Acceleration / Deployment contributions. The corner coefficient is the mean of High-/Low-Speed Aero, Mechanical Grip, Braking and Traction contributions. Circuit `highSpeedCornering` and `lowSpeedCornering` map to the corresponding car aero dimensions. Other pace demand keys match by name.

Driver Race Pace is `(racePace - 82) / 10` whole-lap attribute units. Racecraft is `(overtaking + defending - 164) / 20`. Preservation is `((tyreWear - 82) * tyreDegradationDemand / 100 + driverTyrePreservation - 82) / 20`. Temperature management is `(tyreTemperature - 82) * tyreThermalDemand / 100 / 10`. The existing engine combines preservation and management in its bounded wear denominator; this is a coarse wear consumer, not thermodynamics. AttributeSeconds defaults to one second per whole-lap attribute unit. SegmentSimulation apportions gains by segment baseline-time fraction. It does not repeat a whole-lap gain for each segment.

Supplied tracks contain no segment geometry or baseline lap seconds. The catalog authors a representative 90-second, two-segment layout using supplied track length (km converted to metres). Straight baseline-time/length share is mean straight demand divided by the sum of mean straight and mean corner demands. Corner share is the complement. Straight uses speed demand 1 / corner demand 0; corner uses speed demand 0 / corner demand 1 and disallows passing. All lengths and seconds are positive. This is explicitly representative geometry, not an accurate circuit reconstruction. Segment timing still supports arbitrary validated supplied caller layouts, including subdivisions.

Track race-characteristic stars, including `pitLaneLoss`, are **not seconds** and are inactive in this mapping. Existing explicit engine defaults provide pit-lane/service seconds, passing tuning and tyre tuning. Weather, safety cars, stress and evolution remain inactive.

## Versions, source packaging and compatibility

The preparation model is `effective-weekend-v1`. Driver and track source versions are `1.0.0/2026.10.08-initial-balance`; both original JSON files are embedded resources in Game.Simulation. IDs are resolved explicitly against those files, with their own supplied slug and live display name retained. Unknown identities are rejected. Headline ratings, economy, reference team metadata and calendar claims have no physics consumer. These design JSON records are provisional; no real-world factual calibration is claimed.

Neutral car version is `car-spec/1.0.0/2026.10.08-initial-model`, based on the car specification's reference neutral value 82 in all ten dimensions. The separate reviewed preseason-construction menu gives Reliability 84; this ticket deliberately uses the car specification's uniform neutral preset instead of claiming a constructed car. No construction selections, permanent development or race-package modifiers are applied. `neutral-base-v1`, `neutral-package-v1` and `neutral-setup-v1` identify the unchanged composition context. A caller may supply explicit bounded car ratings for design experiments while retaining the declared vocabulary/version; evidence labels its probes.

Legacy aggregate `Driver` fixtures are already **effective** inputs, with Speed, Cornering and Pace in engine attribute units, not 1–100 detailed scores. They stay on the existing aggregate/segmented model and randomness contracts. `RaceEngine`, `RaceReplay`, `SimulationReplay` and their saved formats are unchanged. Existing replay tests pass. Legacy inputs are not reinterpreted as card stars or automatically converted into detailed presets: a lossless inverse from aggregate Speed/Cornering to seven detailed pace dimensions does not exist. Use the explicit legacy replay path for those saves, and the new effective snapshot path for new presets.

## Assets and presentation scope

Inspected the current collection: 28 driver PNG originals and 23 circuit PNG originals, including the Max Verstappen portrait and Monza track image. Source provenance is the repository's supplied `assets/driver images` and `assets/track images` collection; originality/licensing facts beyond that are not inferred. Originals are unchanged. Stable driver IDs resolve to their supplied slug (`driver-max-verstappen` → `max-verstappen`); circuit IDs resolve independently (`track-monaco` → `circuit-de-monaco`, `track-monza` → `monza`). Do not derive a slug from the ID by trimming a prefix.

This simulation slice packages JSON and live text only. It introduces no image URLs or frontend screen and makes no phone/display layout claim. Existing frontend portrait variants are untouched. The next preparation UI slice should package suitable circuit variants from these originals, retain live names and text fallbacks, and use the retained IDs/slugs for identity mapping. Images must not supply geometry or race rules.

## Reproducing evidence

Run the bounded `tools/WeekendPresetEvidence` console project with one output-directory argument. It uses public catalog/preparation/race operations, records immutable sources/effective inputs, and compares speed/corner probes on Monza and Monaco over paired seeds 101–120 in equal/mixed two-car fields, with zero/noisy clear-air controls. These probes change ratings directly, not Weekend Setup. They are not a free-upgrade setup design or an optimisation claim. The committed evidence and focused test records are linked from ticket 01.
