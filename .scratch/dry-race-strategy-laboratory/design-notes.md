# Dry-race strategy laboratory: design interview

Status: design-confirmed

## Agreed scope

- First deliverable: runnable C# simulation comparing pace and pit strategies across complete dry races. Browser playtesting follows.
- Fixed test rosters and starting grids; 11 teams and 22 cars.
- Two contrasting track profiles: easier passing and greater importance of track position.
- Personnel selection, qualifying and season development are outside this milestone.

## Success criteria

- Push gains time early, but tyre costs can hurt later.
- Fresh tyres can repay pit loss when the stop is timed well.
- Traffic and track characteristics can change which strategy wins.
- Demonstrate reproducible examples and comparisons across multiple seeds; one lucky outcome is insufficient.

## Interview status

All interview rounds are answered. The user confirmed progression to specification by invoking to-spec.

Specification is in progress; implementation follows the approved spec and tickets.

## Agreed phase and tyre rules

- Normal phases cover five laps; the final phase is shorter when needed. Race length is configurable.
- A Pit instruction takes effect at the end of the first lap of the phase. That lap is completed on the existing tyres.
- Conserve, Standard or Push applies for the whole phase, including after a pit stop; a new instruction is chosen at the next boundary.
- Conserve trades immediate pace for lower degradation; Push gains immediate pace at higher degradation.
- Component wear and incident risk are deferred for this milestone.
- Dry compounds: Soft, Medium and Hard, with warm-up and degradation.
- Unlimited fresh tyre sets; a completed race requires using at least two compounds.

## Agreed traffic and pit rules

- Catching and overtaking are separate outcomes. Following can cost time; passing depends on pace advantage, Racecraft and track difficulty.
- Include Neutral, Attack and Defend, offered when a driver starts a phase close to another car. Proximity thresholds are tunable.
- Battle instructions last the phase and affect only relevant battles. Attack improves passing chances; Defend reduces the following car's chances. Both incur tyre costs.
- Pit stops include track-specific pit loss, small seeded stationary-stop variation and re-entry into actual traffic.
- Pit-crew quality is identical initially.
- Support lapping: classification is ordered by laps completed, then progress; display lap gaps for lapped cars.
- Blue flags allow leaders past with a small configurable time loss. Detailed backmarker battles are deferred.

## Agreed scenarios, comparisons and reports

- Begin from a fixed rolling order with explicit gaps and fresh tyres; standing starts are deferred.
- Include an equal-performance controlled field and a fixed mixed-performance field.
- Each driver's strategy can use scheduled decisions or conditional rules.
- Strategy rules receive public race information and the team's tyre estimates; exact tyre condition is restricted to diagnostic reports.
- Compare alternatives by changing one driver's strategy, holding starting field and opponent strategies fixed, over the same seeds.
- Report finishing position, elapsed time and their variation.
- Associate random draws with lap, driver and event type so extra stops do not shift unrelated random outcomes.
- Produce readable summaries and machine-readable results with phase gaps, tyre histories, stops, passes and time lost to traffic and pits.
- Reports distinguish observable outcomes from causal claims; strategy comparisons must support causal claims.

## Agreed information, validation and delivery

- Teams receive seeded, imperfect tyre-condition estimates with equal estimation quality initially. Diagnostics can expose actual condition.
- Exhausted tyres incur a steep pace penalty; punctures are deferred.
- Invalid scripts or decisions produce clear errors rather than silent corrections.
- Completing a race without using two compounds makes the result ineligible; retain raw diagnostic results. No automatic mandatory stop.
- Stay Out and Standard apply only when a script deliberately leaves those choices unspecified.
- Use a shared race clock. Phase boundaries follow the leader's laps. A pit instruction applies at each driver's next lap-end after the boundary; this clarifies first-lap pit timing for cars at different points on track.
- The leader finishes at configured race distance; each other driver finishes at their next line crossing. Record laps completed and finishing time.
- Initial performance attributes: car speed, cornering and tyre preservation; driver pace, Racecraft and tyre management; track performance weights and passing difficulty.
- Stronger relevant attributes should help across repeated runs. Defer setup, packages and remaining personnel attributes.
- Numerical coefficients are tunable configuration and initial hypotheses.
- Deliver one documented local run command and local report files.
- Automated checks cover deterministic replay, legal classification, pit timing, tyre changes, lapping and finishing.
- Use deterministic scenarios to verify rules and multi-seed comparisons to assess strategy.
- Cloud deployment and browser interfaces follow this milestone.
