# Season economy and permanent development

Status: ready-for-agent
Blocked by: campaign, calendar and championships; personnel acquisition and budget ledger; initial car construction and effective-attribute composition.

## Problem Statement

Preseason spending has no continuing opportunity cost and players cannot improve their cars between events. Unbounded development or generous income would reward repeated logins and let early leaders accumulate advantages everywhere.

## Solution

Extend the authoritative credit ledger with modest event income and one between-event development decision per team. Offer bounded permanent refinements and explicit saving, with meaningful costs, personnel-driven efficiency and versioned outcome evidence. Preserve Base Car inputs and apply completed improvements to future weekends, while leaving package creation and Rush for their own spec.

## User Stories

1. As a player, I want preseason costs retained, so that expensive personnel constrain later choices.
2. As a player, I want modest event income, so that finances progress without erasing initial trade-offs.
3. As a player, I want a readable transaction history, so that I can explain my balance.
4. As a player, I want one development decision between events, so that frequent logins grant no extra actions.
5. As a player, I want to save explicitly, so that spending is optional.
6. As a player, I want permanent refinements, so that investment affects subsequent races.
7. As a player, I want speed, handling, tyre and reliability options, so that different philosophies remain possible.
8. As a player, I want costs and compromises before commitment, so that I understand the decision.
9. As a player, I want diminishing returns, so that endlessly stacking one attribute is not automatically best.
10. As a player, I want Manager and Technical Engineer contributions explained, so that staffing has visible value.
11. As a player, I want development uncertainty disclosed, so that outcomes are not presented as guaranteed.
12. As a player, I want a committed choice to survive restart, so that it cannot be rerolled.
13. As a player, I want my current and next-weekend performance distinguished, so that pending work is clear.
14. As a player, I want active-weekend inputs frozen, so that asynchronous work cannot change a race in progress.
15. As a player, I want unaffordable requests rejected clearly, so that malformed choices do not become saving automatically.
16. As a player, I want private budget and plans protected, so that competitive preparation remains private.
17. As an Assisted player, I want spending advice with opportunity costs, so that help does not simply recommend the largest upgrade.
18. As a host, I want AI constrained by the same income and action count, so that season play is fair.
19. As a returning player, I want preparation status clear, so that I know whether I have already used my decision.
20. As a player, I want the final event to close the economy sensibly, so that unusable post-season development is not requested.
21. As a designer, I want season-wide spending experiments, so that runaway advantages can be detected.
22. As a designer, I want price and efficiency settings recorded, so that economic findings can be replayed.

## Implementation Decisions

- Extend the existing integer-credit ledger, campaign preparation coordinator and public domain development rules. Use immutable transaction identities and configuration versions; do not create separate budgets for acquisition, construction and development.
- Starting credit remains meaningful and fixed at campaign creation. Add a small configurable flat credit grant per completed event for all teams, independent of finish order, help level and login frequency. Grant once with the committed event contribution; no arbitrary live bonuses or negative balances.
- Provide one development opportunity between each pair of events. The final event opens no additional development opportunity. Saved drafts can change before commitment, but one accepted investment or Save consumes that opportunity; refresh, takeover and retries cannot create another.
- Initially offer small permanent refinements in implemented car attributes, including relevant speed, cornering/mechanical, tyre and reliability effects. Each option states base cost, effective cost, expected outcome range, prerequisites and any compromise. Bound gains and stacking with explicit caps/diminishing returns.
- Resolve a bounded normal permanent-refinement outcome at preparation close using a versioned public development operation with hidden keyed randomness and zeroable designer controls. Initially support normal success, partial success and delay; richer breakthrough/compromised concept outcomes and Rush remain deferred to the package/outcome spec.
- Reserve/debit the quoted price atomically at accepted commitment, with no second charge for delayed completion. Save incurs no cost. Record committed project identity, personnel inputs, outcome contract/version and intended completion event before acknowledgement; recovery cannot resample.
- A delayed project completes at the following event's preparation close under its saved contract. It does not grant another action, silently cancel the next opportunity or require repayment. Independent opportunities can coexist with pending work, with documented stacking caps and stable completion ordering.
- Completed refinements create immutable permanent modifiers, preserving original Base Car and construction history. Snapshot Base Car plus completed modifiers and selected preset package context before practice/qualifying; late completions never change an active weekend. Unfinished work remains clearly labelled.
- Give Manager Efficiency/Development and relevant Technical Engineer specialisations explicit bounded effects on quoted resource efficiency and outcome quality where implemented. Leadership/Operations meanings remain documented without fabricated raw pace bonuses or unsupported effects. Publish supported consumers and inactive attributes honestly.
- Personnel benefits must retain trade-offs and cost floors. No free projects, repeated hidden discounts or multiplicative stacking that bypasses caps. Race Engineer information effects remain in the observation systems rather than adding development speed arbitrarily.
- AI and assistance operate on legitimate roster, budget, circuit calendar and public development options, with the same action count and costs. Recommendations explain saving and opportunity cost, not future draws or private opponent investments; policies and concrete automatic choices are recorded.
- Between-event preparation remains asynchronous. The host closes it after visible readiness or a disclosed fallback policy; missing teams receive recorded AI/Save choices. Client login time never changes outcome quality or grants more actions.
- Persist project/ledger/preparation transitions with conditional revisions. Reject stale, unauthorised, unaffordable and duplicate conflicting commands atomically; failed writes never acknowledge spending or apply an unsaved modifier. Compatible replay reconstructs balances and completed gains.
- Honour existing public/private modes: standings and readiness are public; budget, project details and private advice stay authorised unless intentionally open. Designer economic diagnostics are separately accessible and never raw player payloads.
- Extend Angular/Tailwind with balance/history, development option comparisons, pending/completed projects and effective-car summaries. Inspect supplied car, personnel and track assets at implementation, keep identity data-driven and retain accessible labels/fallbacks.
- Publish short-campaign and full-calendar fixture experiments once content exists. Compare expensive versus economical personnel, saving versus steady investment and specialised versus broad development using paired seeds, ledger/outcome versions and complete failed/inconclusive results. Measure early-lead amplification rather than asserting caps alone prove fairness.
- Keep the development action contract extensible so the following Race Package spec adds package creation/refinement as alternatives consuming this same opportunity and ledger. Do not add a second per-event action when that feature arrives.

## Testing Decisions

- User-approved main boundary: public campaign API/SignalR, public economy/development domain operations and existing effective-input/race simulation contracts, with focused browser flows and emulator restart coverage.
- Verify one-time income, exact balances, one committed decision per opportunity, Save, rejection atomicity, bounded personnel effects and correct pending/completed summaries through observable outputs.
- Reuse acquisition ledger, campaign scoring idempotency, immutable effective inputs and deterministic random-channel tests. Verify delayed completion ordering and immutable active-weekend snapshots without private formula/storage assertions.
- Restart after commitment, outcome and modifier application; repeated callbacks do not debit, grant income or improve performance twice. Hidden draw changes cannot affect advice when legitimate observations are held fixed.
- Browser tests cover transaction inspection, comparing an affordable option, rejected overspend, Save, delayed work and next-weekend car changes, including private/shared-device modes.
- Record actual player understanding and between-event workload. Season simulations report budget pressure and runaway-risk evidence separately from deterministic correctness and remain explicit tuning hypotheses.

## Out of Scope

- Race Package creation/selection/refinement, Rush, major concept breakthroughs/compromised outcomes and research trees.
- Sponsorship, prize-money escalation, loans, resale, salaries, midseason personnel transfers and monetisation.
- Component inventory/replacement finances, final economy prices and full-season release balance.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.
- [f1_strategy_preseason_car_construction_2026.json](../../.docs/data/f1_strategy_preseason_car_construction_2026.json) - shared preseason construction menu, baseline, option modifiers, costs and example builds.
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Use development domains and change-magnitude guidance alongside the construction baseline. Income, costs, staff effects and outcome probabilities still need tuning; sample budgetUnits are not automatically campaign credits.

## Further Notes

This closes the first permanent-development loop without implementing every later development outcome. Acquisition and construction establish spending inputs; the next package spec reuses this ledger and single opportunity. The user approved public API/SignalR, public domain/simulation operations and focused browser testing boundaries.
