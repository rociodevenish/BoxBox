# S12 — Personnel acquisition

Status: resolved
Blocked by: campaign, calendar and championships; effective attribute and personnel-information contracts.

## Problem Statement

Preset rosters do not let players shape their team or weigh personnel cost against future flexibility. An unrestricted auction would impose valuation work that conflicts with the intended approachable family game.

## Solution

Add a competitive preseason carousel of unique fixed-price personnel, small offers, limited Scout tokens and rotating category priority. Complete each team's roster within a meaningful starting budget, then carry those people and their implemented effects into the campaign. Establish the authoritative budget ledger here; the economy spec later adds income and development.

## User Stories

1. As a host, I want preseason acquisition before the first event, so that everyone builds a roster together.
2. As a player, I want two or three offers, so that comparisons remain manageable.
3. As a player, I want printed fixed prices, so that I do not need auction valuations.
4. As a player, I want people to be unique, so that acquisition is competitive.
5. As a player, I want two Drivers, so that both cars have a roster identity.
6. As a player, I want two Race Engineers, so that each driver has information support.
7. As a player, I want a Manager, so that organisational choices matter.
8. As a player, I want two Technical Engineers initially, so that development specialisations can differ.
9. As a player, I want limited Scouts, so that I can replace unsuitable offers.
10. As a player, I want refreshed people returned to the pool, so that scouting does not destroy options.
11. As a player, I want priority rotated across categories, so that one team does not always pick first.
12. As a player, I want my remaining budget visible, so that expensive hires have clear consequences.
13. As a player, I want required-slot reserves, so that I cannot accidentally make roster completion impossible.
14. As a player, I want attributes and preferences explained, so that comparisons use meaningful differences.
15. As a player, I want future-only staff effects labelled, so that unimplemented bonuses do not mislead me.
16. As an Assisted player, I want honest comparison advice, so that help uses offered information only.
17. As a host, I want AI to acquire under the same constraints, so that empty teams compete fairly.
18. As a player, I want refresh to preserve my offers, so that reconnect cannot reroll choices.
19. As a player, I want a confirmed purchase to survive restart, so that a person cannot be sold twice.
20. As a player, I want a final roster summary, so that I know which personnel enter the season.
21. As a designer, I want acquisition-order experiments, so that priority and Scout compensation can be evaluated.
22. As a player, I want personnel cards to use supplied assets, so that the roster shares the established visual language.

## Implementation Decisions

- Extend campaign preseason states and add infrastructure-independent acquisition rules for pool, offer reservations, priority, Scouts, roster slots and spending. Keep persistence/authentication outside domain rules.
- Initial slots are one Manager, two Drivers, two Race Engineers and two Technical Engineers. Pit Crew remains a team attribute, not an acquired card. Validate sufficient unique content and affordable completion for every active team before acquisition begins.
- Offer two or three available people for the current category/slot. Reserve offers for the active picker until acquisition/scout/expiry resolution, then return unchosen people to the pool. Sequential category picks avoid conflicting hidden reservation policies.
- Rotate category starting priority over a seed-derived stable initial team order and alternate multi-slot pick traversal. Store the schedule and its version; category rotation does not depend on client join/submission speed.
- Scout spends one token and replaces the offer where alternatives exist; returned cards remain available for later picks. No-cost refresh/reconnect cannot reroll. If alternatives are insufficient, disclose that before spending a token and preserve state.
- Configure bounded Scout allocation and modest later-priority compensation as tuning, publishing fairness evidence. Scout counts and prices are not final ratings or market values.
- Establish a shared integer-credit budget ledger with starting credit and immutable acquisition debits. Ownership, roster assignment and debit commit atomically under command/campaign revision. Reject unaffordable/stale choices without partial effects.
- Enforce a conservative affordable-completion reserve for remaining mandatory slots and the minimum viable initial-car option. Validate content/prices against all teams' completion capacity; provide sufficient eligible affordable personnel instead of inventing duplicate real people when a pool is exhausted.
- Freeze printed prices/content version for a campaign. No auctions, resale or personnel transfers. Buying a driver does not change constructor identity or imply their real-world team affiliation.
- Map real driver inputs through the effective-attribute contract. Race Engineer Setup/Tyre Reading/Strategy affect legitimate confidence/estimates where supported; Communication must not imply a new error mechanic unless explicitly implemented and evidenced. Manager and Technical Engineer development/resource effects are deferred consumers until economy delivery and visibly marked as such.
- Explain role attributes and compatibility separately from quality. Personnel may improve information, options, efficiency and execution in their supported systems rather than globally stacking pace bonuses.
- AI chooses from its actual offer, budget and legal actions with recorded policy/version; assistance recommends without accessing future pools, opponent choices or private budgets. Handle absence with a disclosed deadline/auto-pick policy, preserving accepted purchases.
- Persist offers, priority cursor, Scout draws, ledger commands and assignments before acknowledgement. Restart and duplicate scheduler deliveries reuse the same offers and committed purchases; incompatible content versions fail clearly without losing data.
- Extend the Angular/Tailwind card system to Manager, Race Engineer and Technical Engineer comparisons, fixed prices, Scouts, reserves and roster readiness. Recheck supplied driver and future personnel assets, document provenance, retain live text and accessible missing-image fallbacks.
- Publish reproducible acquisition simulations across seeds for completion, budget spread and priority effects. Prices and ratings remain balancing hypotheses until campaign/economy evidence exists.

## Testing Decisions

- User-approved main boundary: public campaign API/SignalR, public acquisition domain operations for deterministic rules and focused browser flows, with existing emulator recovery.
- Verify unique ownership, affordable roster completion, Scout conservation/return, fixed offers on refresh, priority schedule and atomic debit/assignment through observable state.
- Reuse session authority, concurrent claim, deadline and ledger-style revision tests; do not assert private shuffle algorithms or pool storage strings.
- Inject failed writes and restart around offer/scout/purchase commits. Duplicate purchases never debit twice or assign a person to two teams.
- Browser tests cover comparison, Scout, required-slot reserve errors, roster finish and shared-device privacy. Verify AI/advice hidden-input independence and truthful future-only staff labels.

## Out of Scope

- Auctions, resale, salaries, contracts, transfers, sponsorship and final personnel pricing.
- Purchased development effects before the economy spec, separate Pit Crew drafting and full roster photography creation.
- Unlimited refresh, adversarial trading and public-market matchmaking.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.
- [f1_strategy_preseason_car_construction_2026.json](../../.docs/data/f1_strategy_preseason_car_construction_2026.json) - shared preseason construction menu, baseline, option modifiers, costs and example builds.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Use supplied driver cards and portraits without restricting drafting to reference teams. Reconcile construction reserves with the sample menu; staff entries, acquisition prices and a complete affordable pool still need authoring.

## Further Notes

The acquisition ledger is foundational and does not wait for in-season economy. Reserve rules prevent dead ends without silently granting credits or changing fixed prices. The user approved the public session/domain and browser testing boundaries.

## Answer

Implemented on `codex/personnel-acquisition`, based on completed S11. All five tickets are resolved; independent Standards and Spec reviews have no unresolved findings. See the [ticket map](map.md) and [final verification, review and evidence record](evidence/final/README.md). Prices, Scout compensation and personnel value remain provisional; reproducible completion evidence does not claim validated fairness or future economy effects.
