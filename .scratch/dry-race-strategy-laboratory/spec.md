# Dry-race strategy laboratory

Status: ready-for-agent

## Problem Statement

The game's concept and technical architecture are established, but the numerical race model is unproven. We need evidence that pace, tyres, traffic and pit timing create understandable strategic trade-offs before building the multiplayer presentation or season economy.

## Solution

Deliver a local C# laboratory that resolves complete dry races and compares scripted strategies. It provides deterministic race histories, diagnostic reports and paired multi-seed comparisons. Its simulation remains independent of networking, persistence and presentation so it can later power the authoritative game server.

## User Stories

1. As a designer, I want one documented local run command, so that I can run the laboratory without configuring cloud services.
2. As a designer, I want a field of 11 teams and 22 cars with fixed starting order and gaps, so that I can investigate the intended race scale.
3. As a designer, I want fresh starting tyres and a rolling-order start, so that standing-start variation does not obscure strategy experiments.
4. As a designer, I want equal-performance and mixed-performance fixtures, so that I can isolate decisions and investigate realistic traffic.
5. As a designer, I want two contrasting track profiles, so that I can compare passing-friendly races with races where track position matters more.
6. As a designer, I want configurable race distance, so that I can run short rule checks and complete races.
7. As a strategist, I want decisions at five-leader-lap boundaries, so that I can plan using a consistent cadence.
8. As a strategist, I want Conserve, Standard and Push, so that I can trade immediate speed against tyre degradation.
9. As a strategist, I want Pace Instructions to last the Race Phase, including after a stop, so that decisions have predictable duration.
10. As a strategist, I want Soft, Medium and Hard Tyre Compounds with warm-up and degradation, so that compound choice changes stint performance.
11. As a strategist, I want unlimited fresh tyre sets, so that I can investigate race strategy before modelling allocations.
12. As a strategist, I want estimated tyre condition, so that conditional decisions use legitimately available information.
13. As a designer, I want exact tyre condition in diagnostics, so that I can explain estimates and tune degradation.
14. As a strategist, I want exhausted tyres to become substantially slower, so that extending a stint has consequences.
15. As a strategist, I want a Pit Instruction to take effect at my driver's next lap-end after the decision boundary, so that stop timing is unambiguous.
16. As a strategist, I want track-specific Pit Loss and small stop-time variation, so that stopping has meaningful cost.
17. As a strategist, I want tyre changes and re-entry into actual traffic, so that undercuts and overcuts emerge from timing.
18. As a strategist, I want catching and passing to be separate, so that superior pace does not guarantee immediate progress.
19. As a strategist, I want following losses and track-dependent overtaking, so that clear air and track position matter.
20. As a strategist, I want contextual Neutral, Attack and Defend Battle Instructions, so that I can influence nearby battles at a tyre cost.
21. As a designer, I want lapping and blue-flag time losses, so that a full race maintains coherent Classification.
22. As a designer, I want correct finish handling for lapped drivers, so that race results reflect distance completed.
23. As a designer, I want scheduled and conditional decision scripts per driver, so that strategies can be reproduced and compared.
24. As a designer, I want clear errors for invalid input or decisions, so that experiments cannot silently change intent.
25. As a designer, I want a two-compound eligibility check with raw results retained, so that illegal strategies remain diagnosable.
26. As a designer, I want identical inputs and seed to reproduce the same race, so that failures and outcomes can be replayed.
27. As a designer, I want alternatives paired over matching seeds with fixed opponents, so that comparisons isolate the selected driver's strategy.
28. As a designer, I want unrelated random draws to remain stable when a strategy changes, so that comparisons avoid random-stream drift.
29. As a designer, I want finishing-position and elapsed-time distributions, so that I can distinguish useful effects from isolated lucky outcomes.
30. As a designer, I want phase gaps, tyre histories, pit stops and passes in readable and machine-readable reports, so that results can be inspected and analysed.
31. As a designer, I want traffic and pit time-loss breakdowns, so that race stories are supported by evidence.
32. As a designer, I want tunable performance coefficients and attribute effects, so that numerical hypotheses can evolve without changing rules.
33. As a developer, I want rule checks and repeatable strategy experiments, so that subsequent changes preserve a useful simulation.

## Implementation Decisions

- Create Game.Domain, Game.Simulation and SimulationRunner. The domain and simulation have no dependency on HTTP, SignalR, Angular, Babylon.js, Firebase, databases or browser APIs.
- Game.Domain defines race concepts and legal decisions. Game.Simulation resolves authoritative race state and an ordered event history. SimulationRunner supplies fixtures and scripts and writes reports and comparisons.
- The proposed primary test boundary is the public simulation contract: initial state, configuration, seed and phase decisions produce authoritative state and events. Tests may advance successive phases through a complete race. A small runner smoke check verifies command invocation and report generation.
- Resolve individual driver progress against a shared race clock. Normal Race Phases end every five leader laps; the final phase ends with race completion and may be shorter.
- Begin from fixed rolling order, explicit gaps and fresh tyres. Provide 22-car fixtures for equal and mixed performance and two contrasting track profiles.
- Pace Instructions persist throughout a phase, including after a stop. Conserve reduces pace and degradation; Push increases pace and degradation. Initial coefficients are tunable hypotheses.
- Soft, Medium and Hard have distinct pace, warm-up and degradation behaviour. Tyre sets are unlimited. Exhaustion creates a steep pace penalty; punctures are excluded.
- A selected pit stop occurs at the driver's next lap-end after the boundary, with existing tyres used until pit entry. Apply pit-lane travel loss, small seeded stationary-service variation, fresh tyres and re-entry traffic. Pit-crew quality is equal initially.
- Catching does not automatically mean overtaking. Following incurs time loss; passing depends on pace advantage, Racecraft and track difficulty.
- Contextual Battle Instructions are available when proximity at phase start qualifies. Attack and Defend change relevant passing chances and consume tyres; they do not affect irrelevant battles. Proximity thresholds and costs are tunable.
- Classification orders drivers by laps completed and progress. Display lap gaps for lapped drivers. Blue flags permit leaders to pass with a small tunable loss; detailed backmarker battles are excluded.
- The leader finishes at configured distance. Other drivers finish at their next line crossing after the leader finishes. Final records include completed laps and finish time; retain lapped status rather than treating every driver as completing full distance.
- Strategy rules use public state and team tyre estimates, with identical estimation quality initially. Exact hidden condition is available only to diagnostics, not strategy execution.
- Support scheduled and conditional decisions. Reject invalid scripts and decisions with clear errors. Deliberate omissions of pace or pit choice default to Standard or Stay Out respectively; malformed choices are not omissions.
- A completed result that fails the two-compound requirement is ineligible. Retain its raw diagnostic result; no silent correction or automatic mandatory stop.
- Initial attributes cover car speed, cornering and tyre preservation; driver pace, Racecraft and tyre management; track performance weights and passing difficulty. Stronger relevant attributes should confer an advantage across repeated runs.
- Seeded random draws are associated with driver, lap and event type, with distinguishable occurrences where required, so adding a stop does not consume an unrelated driver's future draws.
- A paired comparison changes one driver's strategy and holds initial field, opponent scripts, track and tuning fixed over matching seeds. Opponent conditional rules remain fixed even when their resulting actions respond to changed race conditions.
- Write readable summaries and structured machine-readable reports containing inputs needed for replay, Classification, phase gaps, tyre history, stops, passes and traffic/pit losses. Distinguish observable facts from causal claims requiring comparison evidence.
- The run and comparison flows execute locally and write local report files. No deployment or online account setup is required.

## Testing Decisions

- Test observable outputs through the simulation's public phase-resolution boundary, composing complete races when needed. Prefer this single boundary over tests of internal formulas, helpers or random-number implementation.
- Test Game.Domain rules through accepted and rejected simulation requests, and test Game.Simulation through resulting authoritative state and events. Use a small runner smoke check for invocation and readable/structured output.
- There is no existing application code or test suite to reuse. Establish short deterministic fixtures with explicit expected race outcomes and invariants.
- Verify replay for identical initial state, decisions, tuning and seed, including conditional strategies and event history.
- Verify phase duration, shortened final phases, phase-long instructions and first-next-lap pit timing for drivers at different progress and lap counts.
- Verify tyre age, fresh-tyre changes, warm-up, degradation, exhaustion and instruction costs through externally observable histories.
- Verify catching without automatic passing, blocked traffic, contextual battles, pit re-entry and blue-flag handling.
- Verify unique complete Classification, valid distance/progress, coherent gaps, lap gaps and finish times, including equal-time edge cases with stable ordering.
- Verify input rejection, legal compound changes, explicit default handling and two-compound ineligibility without losing raw results.
- Verify strategy execution cannot access diagnostic hidden state, and random draw identity prevents unrelated stream drift.
- Verify comparison outputs retain paired seeds and distinguish eligible results and distance completed, so elapsed times of differently lapped drivers are not misread as like-for-like performance.
- Demonstrate reproducible scenarios where Push's early gain can be offset by later tyre costs, a well-timed stop repays Pit Loss, and traffic or track characteristics change strategy performance.
- Supplement deterministic rule checks with bounded multi-seed comparisons reporting variability. Record tuning and seeds with results; choose numerical targets during modelling rather than inventing balance thresholds in this specification.

## Out of Scope

- Standing starts, qualifying and personnel acquisition.
- Season calendars, championships, economy and development.
- Setup, Race Packages and the full personnel/attribute model.
- Component wear, incidents, damage, failures, punctures, Safety Cars and weather.
- Tyre allocation inventories and detailed backmarker battles.
- Production AI and assistance recommendations; laboratory scripts are experimental strategies.
- Browser controllers, timing-tower UI, 3D presentation, networking, cloud deployment, authentication and database persistence.
- Proving that the complete family game is fun; this milestone establishes strategy evidence for a later playable browser test.

## Further Notes

The concept and technical architecture remain the baseline. This milestone implements a deliberately limited part of their roadmap. No tuning coefficient is claimed to be final or a faithful real-world F1 calibration.

The design interview, simulation test boundary and eight-ticket breakdown are approved. Implement the tickets in dependency order.
