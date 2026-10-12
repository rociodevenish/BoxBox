# S08 — Qualifying and automatic race start

Status: ready-for-agent
Blocked by: weekend preparation and effective attributes; durable shared-session contracts.

## Problem Statement

Players start from a fixed rolling order, so preparation has no qualifying payoff and the weekend lacks the excitement of earning grid position and watching an automatic start. A complete Grand Prix needs understandable simultaneous risk choices without adding qualifying cards or another complex driving game.

## Solution

Run Q1/Q2/Q3 with one Safe, Push or All-Out choice per remaining driver, produce a deterministic grid and automatically resolve the standing-start opening into the existing race loop. Show concise qualifying and start feedback while retaining authoritative timing, private choices, recovery and the shared decision barrier.

## User Stories

1. As a player, I want preparation locked before qualifying, so that weekend choices have consistent consequences.
2. As a player, I want Q1/Q2/Q3, so that qualifying feels recognisable.
3. As a player, I want Safe, Push and All-Out per remaining driver, so that I can choose risk independently.
4. As a player, I want clear risk explanations, so that I understand ceiling and error exposure.
5. As a player, I want one simultaneous submission, so that I cannot react to opponents' current choices.
6. As a player, I want eliminated drivers clearly identified, so that I only choose for eligible cars.
7. As a player, I want exact recorded qualifying times, so that progression and grid order are understandable.
8. As a player, I want ties and failed attempts resolved consistently, so that grid decisions are fair.
9. As a viewer, I want concise results after each session, so that I can follow both drivers.
10. As an Assisted player, I want legal qualifying advice, so that I can understand risk without privileged information.
11. As a host, I want AI to make qualifying choices, so that unclaimed teams participate fairly.
12. As a player, I want missed decisions handled by the disclosed policy, so that qualifying cannot stall indefinitely.
13. As a player, I want reconnect to retain my lock and results, so that qualifying interruption is recoverable.
14. As a viewer, I want the final grid shown clearly, so that I understand the start order.
15. As a player, I want no separate start-strategy choice, so that the opening remains automatic.
16. As a viewer, I want a short start sequence, so that I can follow gains and losses.
17. As a player, I want driver Starts and car launch fit to matter, so that the opening reflects the prepared field.
18. As a player, I want the correct Classification after the opening, so that my first decisions use real race state.
19. As a viewer, I want reduced-motion or skipped presentation to retain the same results, so that animation is optional.
20. As a designer, I want replayable qualifying and start outcomes, so that reports can be diagnosed.
21. As a designer, I want risk trade-offs tested across seeds, so that All-Out is not assumed always best.
22. As a household, I want qualifying to fit roughly four to five minutes, so that the weekend fits its time target.
23. As a developer, I want the opening to hand off to the existing race engine, so that start and race results share one truth.
24. As a player, I want opening incidents handled when that system exists, so that a major start event can create an immediate reaction window.

## Implementation Decisions

- Add public infrastructure-independent qualifying and automatic-start operations to Game.Simulation using the effective weekend inputs. The session coordinator owns barriers, deadlines, persistence and presentation; no physics runs in Angular.
- Atomically lock all teams' final Weekend Setup and preset package context when qualifying begins. Teams must be final-ready or use a disclosed, durably recorded automatic setup policy; do not silently alter a submitted setup.
- For the full 22-car fixture, Q1 retains 16 and Q2 retains 10 for Q3. Eliminated cars occupy their session-defined grid groups; Q3 ranks the remaining ten. Record this initial format as a configurable game rule, not a claim about current sporting regulations.
- Each session accepts one approach for each eligible driver and one complete team submission. Safe has lower ceiling/error exposure, Push is the normal maximum effort and All-Out has higher potential/variance/error exposure; bounded tunable effects require evidence, not fixed balance claims.
- Resolve one abstract timed attempt per driver per session. Combine Qualifying Pace, effective car/circuit fit, setup compatibility, preset engineer preparation information, approach and hidden seeded variability/error outcomes. Do not implement tyre inventories, out-lap traffic or repeated timed-run management.
- Publish a stable ranking rule: valid times precede no-time results; equal valid times and multiple no-time results use fixed pre-session order as tie-break. Elimination groups retain their own session ordering and no later result silently promotes an eliminated driver.
- Zero-time/invalid outcomes must be explicit statuses, not fabricated lap times. All drivers still receive a unique grid slot; invalid commands fail before mutation and are distinct from simulated errors.
- Key qualifying randomness to stable driver/session/attempt/event identities; avoid extra samples from retries, diagnostics or presentation. Store qualifying model/randomness versions, inputs and concrete choices for exact replay.
- AI and assistance use phase/session-start legitimate observations and legal remaining-driver choices, not future times, hidden errors or opponent locks. Freeze automatic decisions before human submissions, preserve accepted locks at expiry and record source/policy version.
- Extend durable state with Q1/Q2/Q3 decision/results, final grid and opening outcome. Commit before acknowledgement/broadcast and recover through public deterministic operations; stale commands, duplicate callbacks and restart cannot qualify or start twice.
- Resolve the opening automatically using grid, Starts, Racecraft-related abilities, Error Control, traction/launch fit and circuit characteristics with tunable bounded randomness. No player start decision or card is added.
- End the automatic opening at a documented common opening-lap boundary with coherent spatial progress and recorded timing gaps. Initialize/advance the existing race engine from that result rather than maintaining a separate race-time model or adding a grid-gap approximation after the fact.
- Before incidents are implemented, start variation is limited to legal position/timing gains and losses; do not invent contact, damage or Safety Cars. The incidents spec extends this same opening flow so an early interruption precedes the first normal decision window.
- Build short allowlisted qualifying/start event projections and exact results using the Broadcast Angular/Tailwind foundation. Support skip/reduced motion; display animation never governs state. Inspect supplied driver/track assets and use data-driven identities and fallbacks.
- Measure session decision, simulation and presentation times separately, including shared-device handoffs. Treat qualifying's four-to-five-minute and full-weekend approximately thirty-minute targets as player measurements, not automated-test assertions.

## Testing Decisions

- User-approved main boundary: public session API/SignalR; verify setup lock, eligible-driver submission, barriers, elimination, grid, deadlines and recovered opening state through requests and observed messages.
- Test new public qualifying/start operations for deterministic results, input rejection, ordering, no-time/tie handling, risk behaviour and handoff into existing public race resolution. Avoid assertions about private performance formulas.
- Reuse replay/random-channel, Classification, phase-boundary and observation tests plus session idempotency/persistence coverage. Check equivalent retry/recovery outcomes and unique complete grids across all 22 drivers.
- Compare approaches with published paired seeds and contrasting effective presets. Report variance, mistakes and inconclusive results rather than asserting one lucky All-Out lap or tight statistical thresholds.
- Verify public projections exclude seeds, exact hidden state, opponent unexecuted choices and diagnostic errors beyond legitimately visible outcomes.
- A small browser flow completes preparation, three qualifying sessions and automatic start into the first Race Phase; include eliminated cars, different choices for teammates, refresh with a lock and reduced-motion presentation.
- Record actual qualifying duration, two-car comprehension and start readability. Preserve later incident integration tests for an opening interruption when that system arrives.

## Out of Scope

- Real-world rule compliance, grid penalties, parc-fermé administration, Sprint weekends, qualifying cards, tyre allocations and manual multi-run qualifying.
- Player-selected launch strategy, continuous launch/steering physics and a Babylon.js board requirement.
- Incidents, damage, mechanical failure and Safety Cars until the next spec; weather qualifying and weather start mechanics until the weather spec.
- Campaign standings, personnel acquisition, finances and final attribute/risk balancing.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Map supported qualifying pace, starts and car-to-track fit explicitly. Attempt coefficients, grid resolution and opening incident rules still need separate tuning.

## Further Notes

This completes the dry qualifying/start path before contextual disruption. Preset inputs and tunable qualifying format are deliberate game simplifications. The user approved the public session, public simulation and focused browser testing boundaries.
