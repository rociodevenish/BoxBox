# Independent Spec review

Compared `51c18a5e8db556372ca4021ad9dfc75dac5d8fd6` to `a2d7097d7ff74fee8aaaae0322eb6a3246bcb608`. Read the parent spec, all six issues, map, evidence and runtime contracts; inspected simulation, qualifying/start, observation policies and frozen evidence generation. Read-only review; no tests, services or edits to the repository.

## Implemented incorrectly

- **P2 — Deferred suitability transitions disappear when rain changes direction.** `src/backend/Game.Simulation/RaceEngine.WeatherReactions.cs:21–26`. Spec: “Define significant weather transitions by configurable suitability bands with hysteresis and a minimum simulated separation.” The runtime contract further promises: “If minimum separation delays a crossing, the first still-applicable time after that separation is used.” A crossing during the separation period is discarded when its delayed time exceeds the current rain knot. At the next knot the code considers only the new derivative: zero rate returns infinity; reversed rate searches the opposite edge. Consequently, conditions can remain beyond the outward threshold at separation expiry without opening the promised reaction, leaving the remembered band stale until another favorable rain interval. Evaluate already-exceeded hysteresis edges independently of slope and schedule the first eligible still-applicable time; retain derivative-based prediction for future crossings. Add public-boundary coverage for a crossing delayed across a rain knot followed by flat and reversed rates. This finding is established by control-flow inspection, not a newly executed reproducer.

## Missing/partial requirements

- Human acceptance remains explicitly pending, correctly recorded at `.scratch/weather-strategy/issues/06-wet-weekend-evidence.md:13`. Spec: “Measure added decision/presentation time, forecast understanding and simultaneous-trigger clarity in full weekends.” Screenshot/browser and machine-runtime evidence do not fulfill those measurements. Conduct the documented household wet/drying/mixed-trigger sessions before declaring the complete spec accepted. This is a disclosed acceptance gap, not a misleading completion claim.

## Scope and evidence

No additional actionable scope-creep finding. Host activation is necessary to deliver the requested complete weekends. The report accurately distinguishes 560 complete 22-car simulation weekends/hash replay from household measurements, discloses unchanged quality-arm race outcomes and the ten-lap drying profile's failure to reach the Dry threshold, and avoids claiming calibrated forecast probabilities or final balance. No other actionable spec mismatch found in the inspected paths.
