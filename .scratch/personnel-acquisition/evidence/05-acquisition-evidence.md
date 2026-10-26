# S12-05 — Reproducible acquisition evidence

Basis: `97bd125a943c021c27d4203f7158d960a7f06598`, branch `codex/s12-05`. Approved seams: public acquisition domain operations, authenticated campaign API/SignalR, focused browser interactions and the existing failed-write/restart seam. The parent spec already authorises these seams.

## Reproduction and archive

Build and run `tools/AcquisitionEvidence` following its README. Published inputs and raw traces are `05-experiments/acquisition.json.gz`; derived tables are `05-experiments/summary.json`. Replay uses frozen content rather than the current catalog. SHA256 of the compressed archive: `DAD8BDFE5385D6A514A329118CFEBEC84E916D13CA185319A651C43DDF0FB564` (409726 bytes).

Archive `acquisition-evidence-v1` retains domain `personnel-acquisition-v3`, reserve `global-demand-envelope-v1`, schedule `carousel-rotation-v1`, allocation `bounded-scouts-v1`, experiment policy `legal-offer-experiments-v1`, complete materialised content/prices/source versions, exact order/schedule, command IDs/revisions, every actual offer/action with legal reserve previews and own credits/tokens, plus final assignments and immutable ledger. It preserves source driver `1.0.0/2026.10.08-initial-balance` detailed ratings/preferences, provisional staff and prices, and the construction `1.0.0/2026.10.09-preseason-car-build-v1` 18-budget-unit-to-18-credit mapping. Headline stars are not physics. Asset provenance/runtime mappings remain in 04 evidence; no originals or existing replay contracts were changed.

Eight seeds: -1, 0, 1, 7, 42, 72, 123, 2026. Eleven stable synthetic teams fill seven mandatory places each. For each seed, three legal offer-only policies run with later-priority bonus 0 and default 1 (base2, max3): cheapest, highest legal price, and a heterogeneous cohort that Scouts when possible then selects cheapest/highest legal price by stable identity with seed-rotated assignment. Each pair keeps seed, content, team identities and policy fixed. Every accepted hire validates public invariants. The archive test first failed because the tool did not exist, then passed generation/replay, refusal to overwrite, and rejection of changed actions, schedule, balances, unknown schema and incomplete run set. A follow-up assertion checks the published summary's sample counts and reserve/completion invariants.

## Observations

All **48/48 runs and 528/528 teams completed**, with 3696 hires and zero duplicate owners within any campaign. The lowest observed post-hire budget-minus-reserve margin was zero; no credits were granted and every final team retained at least the 18-credit car reserve.

| Policy | Bonus | Final credits min–max | Mean | Population SD | Scouts spent | Blocked offered choices |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Cheapest | 0 | 22–40 | 31.26 | 3.98 | 0 | 0 |
| Cheapest | 1 | 22–40 | 31.26 | 3.98 | 0 | 0 |
| Highest legal price | 0 | 18–31 | 24.06 | 3.41 | 0 | 9 |
| Highest legal price | 1 | 18–31 | 24.06 | 3.41 | 0 | 9 |
| Mixed Scout | 0 | 18–40 | 26.09 | 7.14 | 176 | 40 |
| Mixed Scout | 1 | 18–40 | 25.81 | 7.24 | 216 | 29 |

Each row contains 8 campaigns / 88 teams. Blocked offered choices count public legal-hire previews, not failed commands. Raw trace retains all unavailable-Scout explanations and unused token counts; no unavailable Scout spends a token. Existing API checks separately submit reserve-rejected commands and prove no effects.

Initial-priority mean balances across positions range 28.375–33.75 for cheapest, 21.375–25.625 for highest legal price, 21.625–31.5 for mixed without bonus and 21.75–30 with bonus. Each position has eight samples. Full per-slot pick-position paid-price distributions (eight samples per position) are in the summary; these describe the rotated/reversed saved schedule rather than inferring order from client speed.

With mixed Scout, the paired bonus-minus-zero final-credit change across all 88 teams ranged **-7 to +10**, mean **-0.284**. Bonus recipients at initial positions7–11 (n40) averaged +0.475 credits; other positions (n48) averaged -0.917 because additional Scout draws also change later offers. Bonus spent40 extra tokens. The two no-Scout controls had identical hires/budgets across compensation settings.

These findings are **inconclusive for fairness and optimal compensation**. Lower ending credits can mean deliberate expensive purchases rather than worse outcomes. Fixed provisional prices, paid-price distributions and stored ratings are not actual roster strength, market prices, race results or human preference. Eight seeds are a small deterministic sample; homogeneous policies remove cohort confounding but do not model people, while mixed policy assignments remain confounded with seed/priority. No exhaustive strategy search, causal balanced-position experiment, campaign/economy performance or household understanding was measured. Keep current prices and the modest bonus as tuning hypotheses, not validated balance.

## Focused checks

- Standalone tool build passed; archive/summary contract test passed (25.9s), including replay without catalog access and corruption/overwrite rejection. Retained reports: `.tools/reports/s12-05/build-summary.json`, `archive-summary-test.json`, `published-experiments.json`.
- Reused 14 existing public domain cases passed: Scout replacement/returned people and token conservation, fixed seeded schedule, saved cursor/retry, immutable debit/assignment, reserve rejection and complete unique 77-pick rosters across legal policies. TRX under `.tools/reports/s12-05/domain-scenarios/`. No duplicate private-rule tests added.
- Reused 11 existing authenticated public API cases passed against isolated real Auth/Firestore emulators (Auth22329, Firestore22320, websocket22321, hub22330, logging22380): competing independent coordinators sell/debit once; fixed first offer and Hire/Scout retries survive failed writes or lost acknowledgements/restart; six final-write interruption stages preserve complete roster propagation and atomic ledger assignment. Existing cases submit reserve-breaking commands and verify unchanged observable state. Reports: `.tools/reports/s12-05/api-tests.json`, `api-emulators.json`; TRX under `api/`. The existing 04 callback/sweep/deadline records remain the automatic-pick evidence and were not duplicated.

The final focused acquisition browser run passed 2/2, including the fixed delayed-response privacy regression, revoked-device403, broken-image live fallback, truthful effect labels and complete360px roster flow. Browser assessment and capture paths are in `05-browser.md`; the integration coordinator independently inspected the actual1920×1080 public top and full360px comparison/ready captures before broad verification. All identities are synthetic test fixtures; the archive contains content and synthetic team IDs/command GUIDs, no user identities or credentials. The default gallery now excludes the acquisition integration folder; its dedicated config owns emulators and API. Broad verification is reserved for the integration coordinator after visual review.
