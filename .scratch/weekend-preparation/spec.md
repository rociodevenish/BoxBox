# Weekend preparation and effective attributes

Status: ready-for-agent
Blocked by: race continuity, AI and assistance; reviewed segment-model evidence and browser playtest findings.

## Problem Statement

Players can manage a race but cannot prepare a car for its circuit or understand how driver preferences and car strengths shape the weekend. The current aggregate laboratory attributes cannot explain the three locked Weekend Setup axes or support meaningful qualifying and start behaviour.

## Solution

Add a locally verifiable and hosted weekend-preparation flow using preset cars, drivers and Race Engineers. Players inspect circuit demands, choose one team-wide provisional Weekend Setup, receive lightweight simulated practice feedback and confirm the setup used for qualifying and racing. Establish an explicit effective-attribute model, with tunable effects and evidence rather than final card ratings.

## User Stories

1. As a player, I want a circuit summary, so that I understand its performance demands.
2. As a player, I want my car's strengths explained, so that I can reason about circuit fit.
3. As a player, I want driver preferences distinguished from ratings, so that I understand compatibility.
4. As a player, I want one setup for both cars, so that preparation remains manageable.
5. As a player, I want Aero choices from Speed to Downforce, so that I can trade straight performance against cornering.
6. As a player, I want Mechanical choices from Responsive to Stable, so that I can consider handling and driver fit.
7. As a player, I want Tyres choices from Warm-Up to Preservation, so that I can trade early response against stint life.
8. As a player, I want clear compromises, so that setup does not appear to give free upgrades.
9. As a player, I want practice feedback for both drivers, so that I can see their different reactions.
10. As a player, I want one lightweight practice assessment, so that preparation does not become a separate driving session.
11. As a player, I want to revise my provisional setup after feedback, so that practice informs my final choice.
12. As a player, I want feedback labelled with confidence, so that I do not mistake advice for exact hidden measurements.
13. As a player, I want better engineer Setup and driver Setup Feedback to improve information, so that personnel have understandable value.
14. As a player, I want Adaptability to affect preference mismatch, so that different drivers respond differently.
15. As a player, I want to inspect the final two-car summary, so that I can confirm the intended setup.
16. As a player, I want setup locked when qualifying starts, so that later choices cannot rewrite weekend performance.
17. As a player, I want refresh and restart to preserve preparation, so that interruption does not lose my choices.
18. As a player, I want other teams' private preparation hidden, so that shared play preserves its privacy mode.
19. As an Assisted or Guided player, I want legitimate setup advice, so that I can prepare without understanding every attribute.
20. As a host, I want AI teams to prepare from the same observations, so that opponents do not receive privileged setups.
21. As a player, I want readable driver and circuit imagery, so that the preparation screen fits the game's visual language.
22. As a designer, I want reproducible setup comparisons, so that effects and trade-offs can be measured.
23. As a designer, I want Simple groups separated from effective inputs, so that card values do not silently become physics coefficients.
24. As a developer, I want one effective-performance model, so that practice, qualifying and races cannot disagree about the same setup.

## Implementation Decisions

- Extend Game.Domain and Game.Simulation with immutable preset car, driver, circuit and personnel inputs, a validated three-axis Weekend Setup and explicit effective weekend inputs. Keep session orchestration, persistence and presentation outside simulation.
- Define meanings, units, ranges and consumers for the working attribute set using the locked vocabulary: car Top Speed, Acceleration / Energy Deployment, High-/Low-Speed Aero, Mechanical Grip, Braking, Traction, Tyre Preservation, Tyre Temperature Management and Reliability; driver Qualifying/Race Pace, Overtaking, Defending, Starts, Tyre Preservation/Warm-up, Error Control, Wet-Weather Skill, Setup Feedback and Adaptability. Attributes whose consumers arrive in following specs remain explicit inactive inputs, not claimed implemented effects.
- Preserve the Simple car groups Speed, Cornering, Mechanical Grip, Tyres and Reliability and driver groups Pace, Racecraft, Tyres, Consistency and Technical. Document presentation aggregation separately; never infer engine values from illustrative card stars or invent Aggression as an attribute.
- Map existing aggregate fixtures deliberately into versioned presets and retain replay support or report explicit incompatibility. Expand the existing segment-demand model where needed for implemented setup effects; do not build continuous vehicle physics or multiply whole-lap gains with segment count.
- Setup redistributes performance with bounded compromises, not permanent gains. Use a small validated discrete choice set per axis and publish mappings as tuning hypotheses. Preserve base inputs and compute effective inputs once from the selected setup and explicit modifiers.
- Use one team-wide setup for both drivers. Preferences are compatibility traits; Adaptability moderates mismatch. Engineer Setup and driver Setup Feedback affect confidence/information, not an unexplained free pace bonus.
- Supply a neutral preset package/base-car context. Keep a future explicit composition of Base Car, Permanent Development, one Race Package and Weekend Setup without implementing acquisition or development modifiers now.
- Provide a public deterministic preparation/practice operation accepting configuration, setup and seed and returning legitimate feedback plus separate designer diagnostics. One practice assessment follows a confirmed provisional setup; final revision is allowed without unlimited rerolls or extra feedback. Repeated requests return the same assessment.
- Practice is lightweight simulated assessment, not a playable session or a mutation of race tyres/components. Feedback covers circuit fit, both drivers and setup compromises with uncertainty; no opponent hidden values or future qualifying/race outcomes are exposed.
- Session states distinguish provisional setup, practice feedback and final-ready setup. Final confirmation does not itself start qualifying; the authoritative qualifying-start transition atomically freezes every team's setup and preset package context. The following qualifying spec supplies that transition.
- Persist setup revisions, assessment inputs/output and model versions. Validate team authority, lifecycle, complete choices and command identity; stale/duplicate commands and recovery cannot change confirmed feedback or a qualifying lock.
- Extend observation-driven AI/assistance for provisional/final setup using legitimate circuit/team information. Maintain per-player help modes, explicit uncertainty and privacy-mode filtering, with versioned recorded automatic choices.
- Extend the Angular/Tailwind foundation with circuit summaries, setup choice groups and practice feedback. Inspect the current shared asset collection for supplied driver/track imagery, map identity through data, retain live labels and missing-image fallbacks and document packaging/provenance.
- Publish deterministic and paired-seed evidence on contrasting segmented circuits and equal/mixed fields. Show setup trade-offs, different driver fit and information-quality effects; retain inconclusive findings and tuning rather than claiming a universal optimum.

## Testing Decisions

- User-approved main integration boundary: public session API and SignalR. Submit provisional/final setups, observe legitimate feedback and recover saved preparation through existing authenticated session flows.
- Add the highest necessary public simulation operation for preparation/practice; test effective inputs and externally observable race effects through it and existing race contracts, not private mappings or formulas.
- Reuse existing replay, segment-demand, tyre, observation and paired-comparison tests and the planned session persistence/idempotency harness. Verify unchanged base inputs, rejected setup atomicity, deterministic assessment, bounded trade-offs and no resampling on refresh.
- Verify different preferences/Adaptability and information-quality presets through outputs, avoiding exact private feedback-calculation assertions. Equal legitimate observations must produce equal policy outputs regardless of hidden diagnostic data.
- Verify privacy modes, stale commands, duplicate confirmation, restart and the prohibition on changes after qualifying lock. Test final lock integration when the qualifying transition is delivered.
- Add a small rendered browser flow for setup, both-driver feedback and confirmation, including keyboard/touch, Guided presentation and real supplied assets/fallbacks. Review phone/tablet/TV layouts.
- Record laboratory evidence separately from player comprehension and preparation duration; retain deterministic zero-noise controls and versioned replay inputs.

## Out of Scope

- Personnel acquisition, prices, budgets, initial car construction, permanent development and Race Package creation/selection UI.
- Per-driver Full Mode setup, a playable practice session, unlimited practice optimization and real-world calibration.
- Qualifying resolution, standing starts, incidents, reliability failures, Safety Cars and weather progression; inactive attribute definitions do not implement these systems.
- Full calendar content, final individual card ratings and Babylon.js race-board implementation.

## Further Notes

This begins the complete-weekend milestone using presets. Existing concept and architecture remain the baseline. Attribute meaning is a contract; numerical effects are recorded hypotheses. Completion requires working preparation, replayable evidence and reviewed player-facing feedback rather than the full personnel catalogue.

The user approved public API/SignalR, public simulation operations and a small browser suite as the testing boundaries for all four weekend specs.
