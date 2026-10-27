# Ticket 01 — First hire, frozen pool and credit ledger

Approved seams: public campaign HTTP API, public acquisition domain operations and focused browser flow (parent S12 spec Testing Decisions). Worktree `codex/s12-01`, base `54c36dd`.

## Contract and scope

- Opt-in `CreateCampaignInput.PersonnelMode = acquisition`; existing/default `preset` campaigns retain their original route. Acquisition campaigns remain `acquiring` and both campaign and direct lobby preparation reject attempts to bypass acquisition.
- `PersonnelAcquisition.Open/Hire/Validate` are infrastructure-independent public operations. Campaign storage commits the frozen pool, reserved offer, immutable credit entries, command receipt and assignment in one compare-and-swap record before acknowledgement. Remaining balance is derived from integer ledger entries, never persisted separately.
- Starting credit is 100 per constructor. The initial Manager offer reserves three unique candidates for team-01. A successful first hire releases unchosen cards by retaining the full pool and recording only actual ownership. The next offer remains unset for ticket 02's stored rotating carousel. No Scout, schedule, completion reserve enforcement, AI or personnel performance consumer is claimed in this slice.
- Mandatory slots: Manager; Driver 1/2; Race Engineer 1/2; Technical Engineer 1/2. Pit Crew has no acquired slot.
- Offers/ledger/balance are available only through the approved current device's private team route. Public campaign projection contains progress and current picker, with no private balances or offers. The page requires explicit opening of personnel, closes them on refresh, and retains an uncertain hire command for idempotent retry after reload.

## Supplied data, mappings and assets

Inspected `.docs/data/f1_strategy_driver_cards_2026.json` schema1.0.0/data2026.10.08-initial-balance: all28 unique drivers, independently draftable from constructor affiliation. Cards retain existing `WeekendPresetCatalog` detailed1–100 ratings and preference mapping in their frozen `DriverPreset`; no headline stars become physics. Driver prices are provisional12/18/24 credits from detailed racePace tiers below82 /82–89 /90+. Driver runtime effects wait for ticket04's roster handoff.

Inspected car attribute schema1.0.0/data2026.10.08-initial-model and construction schema1.0.0/data2026.10.09-preseason-car-build-v1. Construction recommends20 budgetUnits and publishes a minimum18 across four categories. The acquisition pool records18credits as the proposed one-budgetUnit/one-credit minimum; no construction is charged or applied here. Ticket03 owns executable conservative reserves and affordability guarantees.

`PersonnelCatalog` authors15 Managers (8–10credits),26 Race Engineers (6–8) and26 Technical Engineers (5–7): fictional, provisional staff with stable IDs and1–100 printed attributes matching the concept document. With28 supplied drivers the95-person pool has sufficient unique identities for11×7 mandatory slots, plus alternatives. Pool `personnel-pool-v1/1.0.0/2026.10.08-initial-balance`, prices `provisional-prices-v1`, staff source `fictional-staff-v1`, rules `personnel-acquisition-v1`. Frozen cards are saved rather than re-read during recovery; unsupported pool/rule formats fail while preserving the save. Ticket03 additionally validates affordable completion capacity.

Reinspected current `assets/`: only driver images and track images; no staff photography was supplied. Existing driver runtime WebP variants and stable portrait manifest remain preserved for driver presentation in04; campaign track imagery retains the existing track manifest/provenance. First-offer Managers use accessible live text and a decorative initial fallback. No originals were changed, no new staff photographs invented. Future development/resource consumers and absent current performance bonuses are visibly labelled. No new Communication errors, global pace bonuses or budget grants exist.

## Verification

- Domain red: missing public acquisition operations, then green first unique hire/debit/retry. Additional public-domain red rejected unsupported content version; final3tests pass, including mandatory-content rejection and11-team capacity.
- API red: opt-in acquisition still returned `created`; implemented `acquiring`. Focused real Auth/Firestore API cases cover reserved offers surviving refresh, foreign authentication rejection, stale command no partial effect, campaign/direct-lobby preparation guards, failed writes before commit and lost acknowledgement after commit, restart and duplicate purchase preserving one assignment/debit. An intermediate privacy check was corrected to match the JSON property rather than the word `balance` inside the supplied source-version string. A theory attribute typo was corrected before the final run.
- Browser red: missing Personnel selector. Focused green covers360px create/claim/compare, fixed offers on reload, printed future-only staff labels and saved debit. Final flow additionally drops a purchase acknowledgement after the actual server commit and retries the saved command after reload. Desktop capture closes private personnel first.
- Reports are retained under ignored `.tools/reports/s12-01-*.json`; bounded runners record owned launched PIDs, exit status and duration. API/emulator ports21979/21980, websocket9297, hub21981/log21982; browser UI4691/API4791. Runs own and stop their processes; other listeners were not reused or stopped.
- Node24.22.1 cached runtime; .NET10.0.401 selected by global.json; Java21.0.12.1 cached runtime; locked root dependency caches linked into this worktree. Sandbox MSBuild failed silently; focused runs used narrow escalation. Restore used `NuGetAudit=false` only because advisory feed access failed; dependency pins were unchanged.

## Visual assessment

Reviewed the complete360×800 phone flow in s12-01-offer-360.png and s12-01-hired-360.png: all three cards, prices, attribute labels and disabled-effect disclosures remain readable; controls wrap without horizontal overflow; saved hire leaves92credits and one8credit debit. The tall single-column page requires scrolling but has no clipped text or overlapping controls. After the dropped acknowledgement, the browser waits for the duplicate retry response to settle before reload; the final saved capture has no pending retry affordance.

Reviewed s12-01-campaign-1920.png at1920×1080: calendar, constructor controls and household progress are readable, with the private personnel panel explicitly hidden and no remaining team budget exposed. This viewport records the campaign overview at its current scroll position; ticket05 owns the full final public-display flow after carousel completion.

Published only the three reviewed named captures using tools/publish-evidence.mjs. Final checks: public domain3passed; real API3passed (two before/after failed-write recovery cases and the preserved preset create/restart case); focused browser1passed32.4sec including lost acknowledgement/retry. Run manifests retained: s12-01-domain-complete.json, s12-01-api-integrated.json and s12-01-browser-retry-settled.json. Integration base436dc3b was merged before completion. No broad verifier was run.
