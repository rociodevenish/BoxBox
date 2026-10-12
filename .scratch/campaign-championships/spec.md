# S11 — Campaign, calendar and championships

Status: ready-for-agent
Blocked by: weather strategy and the complete-weekend evidence; durable shared-session identity, recovery and control contracts.

## Problem Statement

A household can complete an individual weekend but its results have no continuing championship significance. Players cannot return on another day, see the next circuit or prepare asynchronously without manual state management.

## Solution

Connect weekends into a durable campaign with preset teams, a versioned calendar, Constructors' and Drivers' standings, post-race reports and an asynchronous preparation period. Prove a short representative campaign before enabling the full real-world calendar, preserving one authoritative simulation and existing live-session privacy and recovery.

## User Stories

1. As a host, I want to create a campaign, so that several weekends belong to one season.
2. As a player, I want stable team ownership, so that my constructor identity persists.
3. As a player, I want both drivers retained, so that their results accumulate coherently.
4. As a host, I want a short test calendar, so that campaign progression can be verified quickly.
5. As a host, I want a full-calendar option when content is validated, so that the finished campaign meets its intended scope.
6. As a player, I want upcoming circuit information, so that I can prepare ahead.
7. As a player, I want Constructors' standings, so that I understand the primary competition.
8. As a player, I want Drivers' standings, so that individual performance also matters.
9. As a player, I want points and tie-breaks explained, so that championship order is understandable.
10. As a player, I want ineligible and retired results distinguished, so that scoring is predictable.
11. As a player, I want a post-race report, so that I can understand changes in standings.
12. As a player, I want to revisit previous results, so that the season has a readable history.
13. As a player, I want between-race preparation saved, so that I can act when convenient.
14. As a host, I want readiness visible, so that I know when the next weekend can begin.
15. As a player, I want private preparation protected, so that asynchronous play respects our privacy mode.
16. As a host, I want absent teams covered by disclosed AI policy, so that a season can continue.
17. As a returning player, I want authorised reclaim, so that I can resume my team safely.
18. As a player, I want server restart to retain results and preparation, so that a multi-day season is dependable.
19. As a host, I want final champions and season completion, so that the campaign ends clearly.
20. As a designer, I want reproducible campaign histories, so that scoring or progression failures can be investigated.

## Implementation Decisions

- Add an application campaign coordinator over the existing weekend session and infrastructure-independent domain rules. Campaign, team, driver and event identities are stable and distinct from browser devices and live session identities.
- Store a versioned immutable ordered calendar at creation. Initially provide a short fixture calendar with contrasting validated tracks; full-calendar mode requires a complete validated content manifest and remains disabled until available. Calendar content is not fetched silently at runtime.
- Freeze preset rosters/car configurations for this first campaign flow. Constructor identity does not determine personnel or performance. Preseason acquisition and development are later extensions rather than blockers for preset campaigns.
- Define lifecycle states for creation, event preparation, active weekend, committed event result, between-event preparation and season complete. The host starts a weekend from authoritative readiness; duplicate requests never create two active events.
- Publish an explicit configurable game scoring table, initially 25/18/15/12/10/8/6/4/2/1 for the first ten eligible finishers, with no fastest-lap bonus. Ineligible/retired drivers score zero in this initial rule; positions are not reassigned after eligibility exclusions. Record this as a versioned game simplification, not current sporting regulations.
- Constructors sum both drivers' awarded points. Rank equal totals by counts of higher eligible finishing positions, then fixed campaign starting order as a final stable tie-break. Driver points attach to stable person identity; preset rosters avoid transfers here.
- Commit a weekend result and its scoring contribution exactly once under campaign/event revision. Standings derive from committed contributions and can be rebuilt; partial writes or rebroadcast do not award points twice. Preserve raw Classification, distance, status and eligibility beside scored results.
- Persist event creation inputs, configuration/model versions, result references, calendar cursor, assignments and preparation. Replay existing weekend histories through their public deterministic operations; do not serialize private engine internals or discard incompatible saves.
- Between events allow saved Weekend Setup drafts and legitimate circuit/team observations, but no extra development actions or simulated practice rerolls from repeated logins. Practice and final lock follow the preparation spec's policy.
- Keep player/team/device separation and authenticated membership across events. Host-directed AI takeover/reclaim uses existing boundary rules; future-event readiness cannot overwrite current committed locks. Opening the next event uses disclosed missing-team preparation fallback, never unannounced hidden choices.
- Keep public results/standings separate from private setup, recommendations and diagnostics. Honour Competitive/Family/Mixed modes for preparation just as for live choices.
- Extend Angular/Tailwind with calendar, standings, event reports, preparation readiness and season finish. Inspect supplied track/driver assets at implementation time; identity mapping is data-driven and missing imagery does not obscure names, points or readiness.
- Run a short multi-day household campaign and replayable full-calendar fixture simulation once content exists. Record progression reliability, returning-player clarity and persistent storage/hosting costs separately from balance claims.

## Testing Decisions

- User-approved main boundary: public API/SignalR requests and observed campaign state, with real persistence through the existing emulator/restart harness and small browser flows.
- Prefer public campaign rule operations for scoring and deterministic progression; exercise their visible outputs through committed events rather than private counters or storage layout.
- Reuse weekend result/replay, retirement/eligibility, membership and idempotency coverage. Verify scoring groups, tie-breaks, rebuildable standings and exactly-once result contributions with concurrent/duplicate requests and write failures.
- Verify readiness, absent-team policy, cross-campaign authorisation and private asynchronous preparation. Restart during event creation, result commit and between-event drafts.
- Browser checks cover two events, standings/report navigation, returning after interruption and season finish; record actual household findings separately from automated correctness.

## Out of Scope

- Acquisition, car construction, development economy, transfers, contracts, sponsorship and component replacement budgets.
- Sprint formats, penalties/appeals and real-world championship rule fidelity.
- Full-calendar asset creation, final season balancing and public matchmaking.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Build the candidate calendar from event-to-track references and reviewed preset rosters/cars. Verify dated real-world assertions before release and freeze approved campaign content.

## Further Notes

Constructors' Championship is the primary player victory condition; Drivers' Championship is secondary. Complete the preset campaign before adding scarcity or spending. Public API/SignalR, public domain/simulation operations and focused browser flows are the user-approved testing boundaries.
