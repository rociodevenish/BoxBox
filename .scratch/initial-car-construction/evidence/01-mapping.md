# S13 construction catalogue and draft mapping

Ticket 01 uses the approved public construction domain/simulation operations and authenticated campaign endpoints; focused browser tests exercise actual Auth/Firestore emulators. Drafts are saved, free and team-private. They never debit the personnel ledger.

## Frozen content and credits

- Menu source: `.docs/data/f1_strategy_preseason_car_construction_2026.json`, schema `1.0.0`, data `2026.10.09-preseason-car-build-v1`. All four supplied categories and all five options in each are retained (625 combinations). Category minimum costs are 5 + 5 + 4 + 4 = 18.
- Currency mapping remains `budgetUnits-to-credits-v1`: one integer budget unit equals one integer credit. Personnel acquisition already protects 18 credits for the cheapest viable complete car. No credit top-ups, scarcity or constructor identity multipliers exist.
- The source's provisional separate 20-unit envelope is superseded by the authoritative shared credit ledger. Complete builds cost 18–24 credits; a draft must cost no more than its team's actual balance. Spending above 20 is legal if actual remaining credits support it, reducing future flexibility. All 625 builds are available, not necessarily affordable for every team.
- Catalogue `construction-catalogue-v1/1.0.0/2026.10.09-preseason-car-build-v1` is captured in each new acquisition campaign when rosters complete. Existing records without `ConstructionRequired` preserve their existing path. Preset campaigns and legacy neutral Base Cars remain unchanged.

## Detailed attributes and diminishing returns

`bounded-detailed-deltas-v1` sums the supplied modifiers by supported detailed attribute, applies the first two points of magnitude fully, subsequent points at half strength, caps magnitude at six, then clamps baseline plus bounded delta to 1–100. This is an authored tunable hypothesis, not researched physics. E.g. source straight-line specialist stacks +5 Top Speed; runtime maps to +3.5, producing 85.5 rather than the source additive example's 87. Negative deltas follow the same rule. Source sample builds remain illustrative source examples, not expected runtime coefficients.

The ten supported keys are Top Speed, Acceleration/Deployment, High-Speed Aero, Low-Speed Aero, Mechanical Grip, Braking, Traction, Tyre Wear, Tyre Temperature and Reliability. The supplied construction baseline uses 82 for the first nine and Reliability 84. Existing S11 neutral presets retain Reliability 82. There are no headline-star conversions. The five Simple groups use the locked car attribute specification headlineDerivation weights: Speed Top Speed .5/Deployment .5; Cornering High-Speed Aero .55/Low-Speed Aero .45; Mechanical Grip Grip .4/Braking .3/Traction .3; Tyres Wear .55/Temperature .45; Reliability 1. They summarize the same detailed values and are not inputs back into physics. Source car specification version is 1.0.0/2026.10.08-initial-model.

Reliability is displayed as a design margin. The existing effective race model does not yet consume this detailed rating as a new failure process; ticket 01 adds no undisclosed reliability incidents. Pit Crew and preset versions are preserved. Managers and Technical Engineers retain inactive effects; assigned Race Engineer Setup continues to affect only Practice Assessment information for the assigned driver.

## Legitimate previews

`construction-preview-v1` consumes only the frozen menu, selected options, own saved roster and actual balance. Two validated representative track fixtures, Monza and Monaco, supply static demands; no future campaign weather, incidents, seed or lap-time prediction is exposed. Qualitative strengths and compromises compare against the supplied baseline weighted by supported demand groups.

The six authored handling labels are retained as content. Preview compatibility projects rear stability plus traction confidence minus front response minus rotation onto the existing responsive/stable mechanical axis (divide by two, clamp to -2..2 and round away from zero). It reuses existing driver preference and Adaptability logic for both drivers. Braking Entry Stability and High-Speed Stability are not invented independent physics or fit effects. The presentation explicitly calls this indicative compatibility. Ticket 02 must materialize/apply any supported construction fit model once and version it; ticket 01 does not modify active race inputs.

## Lifecycle and durable transitions

New acquisition campaigns carry explicit `ConstructionRequired=true`; after roster completion, frozen roster/catalogue and `constructing` lifecycle commit before acknowledging. The first lobby has a durable `campaign-construction-pending-v1` guard from creation. Roster handoff sets that guard before removing the personnel guard in the same lobby write; reads/retries repair interrupted handoffs. Both campaign preparation and direct lobby preparation reject while construction is pending. No draft opens a qualifying path.

Each team's draft revision and accepted command receipt are saved before acknowledgement. Duplicate identical commands return the same saved revision; stale or conflicting commands reject. An unaffordable/invalid choice cannot replace the saved draft or change credits. Conditional campaign writes reject cross-instance races. The browser retains uncertain draft commands and explicitly retries the same command after reload.

### Forward handoff to tickets 02/03

`ConstructionState` currently has catalogue and per-team editable drafts. Add final records/contracts there or alongside it; the construction-required flag must remain durable. Never remove the lobby construction guard until the all-team readiness policy is recorded and materialized frozen presets are durable. Keep this repairable across separate campaign and lobby stores.

Personnel ledger validation currently accepts only starting credit and personnel debits, and `RosterReady` requires the unspent 18-credit car reserve. Ticket 02 must explicitly validate construction debit records and separate mandatory roster completion from unspent reserve after purchase; a legal 18-credit car can leave zero credits. Do not silently weaken validation to allow arbitrary debits. Preserve old saved ledger/replay formats.

## Assets inspected

The current supplied collection contains 51 original PNGs in driver images and track images; no car/construction artwork or icons exist. Construction uses live text and named categories rather than guessed asset URLs. Existing stable driver IDs and track IDs resolve through the packaged portrait and track manifests. These retain user-supplied provenance; originals/manifests are unchanged. Failed images hide while names and comparison text remain readable. Image geometry supplies no simulation rules. Representative runtime assets and actual screen captures are reviewed in the ticket evidence.

Full-calendar comparisons remain gated on validated circuit content.

