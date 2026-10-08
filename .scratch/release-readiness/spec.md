# Season balance and release readiness

Status: ready-for-agent
Blocked by: all preceding delivery specs, including Race Packages, full-calendar content and race presentation; integrated campaign evidence.

## Problem Statement

Individual systems and automated checks do not establish that a full family campaign is understandable, fair, accessible or dependable on real devices. Release needs evidence that season finances and personnel choices remain viable, sessions survive interruption and graphical/hosting costs fit household use.

## Solution

Validate the integrated game through reproducible full-season experiments and actual household campaigns, improve onboarding and accessibility, measure supported-device performance and hosting operations, and publish a versioned release candidate with documented supported conclusions and limitations. Correct failures before claiming readiness.

## User Stories

1. As a new player, I want a short introduction, so that I can join without reading technical documents.
2. As a player, I want a guided first decision flow, so that I understand both cars and simultaneous locks.
3. As a host, I want setup and privacy choices explained, so that the household starts correctly.
4. As a player, I want attribute meanings and estimates explained, so that information is not misleading.
5. As a player, I want keyboard and touch access, so that device choice does not restrict play.
6. As a player, I want text alternatives and reduced motion, so that colour or animation is not required.
7. As a TV viewer, I want readable information at living-room distance, so that shared play is practical.
8. As a player, I want supported browsers/devices documented, so that I know what has been verified.
9. As a host, I want dependable reconnect and saved campaigns, so that multi-day play survives interruptions.
10. As a host, I want safe deployment and rollback, so that updates do not destroy active seasons.
11. As a host, I want costs and limits measured, so that family hosting is sustainable.
12. As a player, I want several viable roster/car philosophies, so that preseason does not have one obvious answer.
13. As a player, I want permanent versus package investment to remain meaningful, so that development choices persist through the season.
14. As a player, I want early success not to create runaway accumulation, so that later events still matter.
15. As a player, I want AI to be credible and fair, so that unclaimed teams support rather than dominate family play.
16. As an Assisted player, I want advice that teaches without cheating, so that help remains fair.
17. As a household, I want qualifying and races paced appropriately, so that a weekend fits roughly thirty minutes.
18. As a player, I want final results and champions clear, so that the season concludes satisfyingly.
19. As a maintainer, I want reproducible release evidence, so that later tuning can be assessed against a baseline.
20. As a host, I want known limitations explicit, so that release claims match actual evidence.
21. As a player, I want helpful recovery errors, so that temporary failure does not require developer intervention.
22. As a designer, I want inconclusive findings retained, so that tuning is not based only on favourable examples.

## Implementation Decisions

- Treat this as integrated verification and corrective product work, not permission to invent new mechanics. Use existing campaign, weekend, economy, policy and renderer contracts; trace every release finding to user-visible behaviour and a concrete correction or documented limitation.
- Freeze a release candidate containing code, content, tuning, schema/model/randomness and AI/assistance versions. Preserve seed lists, campaign choices, ledger histories and workload descriptions so experiments and failures can be replayed.
- Publish a bounded season experiment matrix spanning representative personnel budgets, Base Car philosophies, permanent investment, package libraries/refinement/Rush, saving, circuit variety and assistance/AI combinations. Hold exogenous inputs paired where possible and report sample counts, distributions, eligibility and distance groups.
- Examine early-lead amplification, financial dead ends, acquisition priority, viable strategy alternatives and personnel opportunity cost. Tune only through explicit versioned changes and repeat affected evidence; do not imply equal finish rates or all options being identical is the balance goal.
- Assess AI against representative engaged-player strategies across contrasting circuits and full seasons, retaining roughly midfield as an evidence target rather than a guaranteed position. Verify no performance/information bonuses and that advice reacts only to legitimate observations.
- Run actual household campaigns across separate days, including private phones, shared tablet, open TV and Mixed mode, different help levels and absent players. Record joining friction, two-car workload, handoff time, comprehension, engagement and where developer intervention was needed.
- Measure preparation, qualifying decision/simulation/presentation, race decision/simulation/presentation, reconnect and recovery time separately. Evaluate four-to-five-minute qualifying, twenty-to-twenty-five-minute races and approximately thirty-minute weekends from real players; publish unmet targets and corrections, not automated-click substitutes.
- Add concise host onboarding, contextual rule explanations, first-use two-car guidance and help for forecasts, compound eligibility, setup/package locks and development opportunity cost. Tutorials use the real rules with clearly labelled practice fixtures and no extra live actions.
- Complete manual/automated accessibility review toward WCAG 2.2 AA, including contrast, semantic labels, focus order/visibility, keyboard operation, text alternatives, touch targets, reduced motion and readable errors. Record tested coverage and remaining limits rather than making unsupported certification claims.
- Publish a supported browser/device matrix based on actual phone/tablet/TV hardware. Verify long names, dense standings, card comparisons, loaded supplied assets, graphics fallback and viewing distance; document asset provenance and deliberate visual gaps.
- Measure frontend bundle sizes, display frame/memory behaviour, simulation/replay latency, Firestore sizes/requests, scheduler callbacks, reconnect and Cloud Run startup/idle behaviour at family scale. Keep controller renderer isolation and graceful fallback intact.
- Exercise authenticated join/subscriptions, assignment revocation, privacy modes, storage failures, scheduled retries, expired-window recovery and content/version compatibility. Logs/diagnostics retain no credentials or hidden data in player-facing channels.
- Document deployment, rollback, backup/restore, compatible save migration and active-campaign handling. Restore a retained test campaign and rehearse a failed deployment without discarding committed results; incompatible versions fail actionably.
- Configure observed cost limits, retention and actionable operational alerts. Verify idle and active family-scale costs; approximately zero monthly cost remains a measured goal, never a guarantee.
- Maintain a release checklist with evidence links, severity/owner of open issues and explicit supported scope. Release readiness requires required checks and material failures resolved; publishing spec documents alone does not certify implementation readiness or deploy the game.

## Testing Decisions

- Reuse the user-approved public API/SignalR, public domain/simulation operations, browser flows and persistence-emulator boundaries. Broaden only for new integrated failures; avoid a parallel test model or private formula assertions.
- Run the relevant integrated regression suite and replay release fixtures after tuning changes. Add tests for discovered externally observable defects rather than mirroring every implementation detail.
- Rehearse restart/write failures, duplicate deadlines, permission revocation, missed presentation and supported-version restores through real requests and observed outcomes; verify no repeated spending, points or resolution.
- Full-season statistical evidence reports uncertainty and failed hypotheses separately from deterministic correctness. Do not encode tight random finishing thresholds or hardware timing in unit tests.
- Browser and manual checks cover onboarding, accessible complete decision flows, help/detail/privacy combinations, real supplied assets and graphics fallback on the supported device matrix.
- Perform hosted smoke and household campaign tests with measured costs, startup/recovery timings and actual viewing-distance findings. Local emulator/benchmark success does not establish hosted dependability or player enjoyment.

## Out of Scope

- Public matchmaking, large-scale multi-instance hosting, formal high-availability certification and paid-service economics.
- Race cards, Red Flags, continuous driving physics, new campaign systems and multi-season careers.
- Claims of real-world numerical fidelity, universal accessibility certification or guaranteed zero hosting cost.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.
- [f1_strategy_preseason_car_construction_2026.json](../../.docs/data/f1_strategy_preseason_car_construction_2026.json) - shared preseason construction menu, baseline, option modifiers, costs and example builds.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Record reviewed data and asset versions in balance/household evidence. Audit entries, missing content, factual assertions and affordability; provisional records alone do not establish release acceptance.

## Further Notes

This completes the planned release scope with evidence, corrective work and operations rather than another speculative mechanics layer. Implementation/playtests may produce additional bug or corrective tickets without requiring a new feature spec. The approved testing boundaries remain unchanged.
