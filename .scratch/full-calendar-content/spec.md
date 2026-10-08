# Full-calendar content and card coverage

Status: ready-for-agent
Blocked by: campaign, personnel, car construction and Race Package content contracts; implemented weekend mechanics and visual foundation.

## Problem Statement

Representative fixtures prove systems but do not provide a complete playable season. Missing circuit profiles, personnel entries or inconsistent card meanings can break acquisition, misrepresent simulation capabilities and leave later events unavailable.

## Solution

Publish a validated, versioned release content pack covering the selected real-world season calendar, eleven constructor identities, sufficient unique personnel, supported car/package choices and readable cards/circuit summaries. Reuse supplied assets and complete necessary gaps without treating illustrative ratings or artwork as authoritative simulation data.

## User Stories

1. As a host, I want a complete calendar, so that a campaign can finish without manual content repair.
2. As a player, I want eleven constructor identities, so that all team slots have coherent presentation.
3. As a player, I want circuit demands and race characteristics, so that preparation choices are meaningful.
4. As a player, I want recognisable track imagery, so that events are easy to identify.
5. As a player, I want every acquired person to have a readable card, so that no roster entry is unusable.
6. As a player, I want sufficient unique affordable personnel, so that all teams can complete acquisition.
7. As a player, I want prices and attribute meanings consistent, so that comparisons are trustworthy.
8. As a player, I want preferences separate from ratings, so that compatibility is clear.
9. As a player, I want implemented and inactive effects distinguished, so that cards do not promise nonexistent mechanics.
10. As a player, I want car/package cards to show compromises, so that strengths are not presented alone.
11. As a player, I want missing images to retain usable text, so that content failures do not block play.
12. As a player, I want the full roster usable on phone and TV, so that dense content remains readable.
13. As a host, I want campaigns to retain their content version, so that an update does not silently change an active season.
14. As a designer, I want provenance and tuning recorded, so that factual source data and game hypotheses are distinguishable.
15. As a developer, I want content validated before campaign creation, so that broken references fail clearly.
16. As a designer, I want every circuit exercised through the public simulation, so that a manifest alone does not imply playable coverage.
17. As a player, I want Simple and detailed labels to agree, so that one card does not describe two different models.
18. As a maintainer, I want new supplied assets easy to adopt, so that additional track/personnel art requires no component redesign.

## Implementation Decisions

- Create a versioned content manifest with stable constructor, person, circuit, option, package and asset identities. Reuse existing domain contracts and frontend view models; avoid a second set of physics attributes inside cards.
- Select and record the release calendar season explicitly, using existing 2026 samples as candidate inputs rather than assumed verified facts. At implementation, verify calendar, constructor and roster facts against dated primary sources and retain source/provenance notes; no live lookup changes an active campaign.
- Include the full selected real-world event order with explicit track mapping. Modelled circuit demands, passing zones, Pit Loss, stress, weather and Safety Car tuning are game hypotheses distinct from sourced identities/layout references.
- Validate all units, bounds, segment identities/opportunities, positive travel times, content references, active effect consumers and attribute group labels before enabling the pack.
- Ensure unique acquisition pools support all mandatory slots for eleven teams and affordable reserves under the configured budget/catalogue rules. Deliberately author missing staff entries with clear fictional/illustrative provenance rather than duplicating unique people or implying fabricated real-world facts.
- Finalise release candidate ratings/prices only after their implemented effects and opportunity costs can be tested. Persist content/tuning versions and expose meaningful documented scales; never map sample card stars silently into engine values.
- Complete Driver, Manager, Race Engineer, Technical Engineer, Car, Race Package and Circuit information presentations using the shared anatomy/tokens. Pit Crew remains a team attribute; no qualifying/race-card mechanic is introduced.
- Inspect the current repository asset collection before each content pass, including user-added track and personnel assets. Catalogue identity mapping, provenance, intended usage, dimensions and crop/contrast suitability. Preserve originals and package runtime variants reproducibly; do not assume filename alone establishes identity or rights.
- Provide useful neutral fallbacks for absent/unsuitable imagery. Full content coverage requires readable entries, not photographic imagery for every record; list deliberate visual gaps and review their quality.
- Make content versions immutable for active campaigns, retain compatible assets or documented migration support and reject unsupported historical versions clearly without losing saves.
- Exercise every circuit in complete 22-car races, weather/incident scenarios where configured and a full-calendar preset campaign; record model limits rather than claim real-world calibration.
- Keep designer diagnostics/source metadata separate from public facts and legitimate card descriptions. Player screens show actionable game information, not implementation details or hidden coefficients.

## Testing Decisions

- Reuse approved public content-consuming campaign API/SignalR and public domain/simulation operations; validate loaded pack acceptance/rejection through campaign creation and observable outcomes.
- Test bad references, insufficient unique pools, affordability dead ends, invalid track units and unsupported versions without coupling to manifest storage layout.
- Reuse full-race replay, acquisition completion, effective attributes and calendar progression checks across all release entries. Record bounded integration workloads rather than adding a private content-parser test seam.
- Browser gallery/flows inspect representative and worst-case cards, long names, missing imagery and circuit summaries on phone/tablet/TV. Verify real assets visually; successful loading alone is not visual QA.
- Rebuild a campaign from its saved content version after an update and check identical inputs/results. Record provenance audit and content coverage alongside executable checks.

## Out of Scope

- Inventing new mechanics to justify unused sample attributes, a live sports-data feed, multi-year content updates and full real-world physics calibration.
- Babylon.js scene implementation and release-wide onboarding/operational validation, covered separately.

## Further Notes

The complete content pack enables the full-calendar mode that earlier campaign work kept gated. This spec completes coverage; release readiness owns final integrated balance acceptance. Testing boundaries reuse the user's approved public contracts and browser gallery.
