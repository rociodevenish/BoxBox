# S13 immutable Base Car confirmation

The parent spec approves the public authenticated API, construction/effective-input domain operations, browser interactions and real persistence emulator seams. Implementation used the TDD skill: the first public confirmation test failed to compile before `ConstructionConfirm`/`CarConstruction.Confirm` existed, then passed with the atomic operation. No private simulation coefficient seam was added.

## Durable purchase

`POST /api/campaigns/{id}/construction/teams/{teamId}/confirm` accepts a GUID, saved draft revision, authorised team and catalogue version. It freezes the selected options, draft/final revisions, materialised ratings, handling projection, source history and identity. Its fingerprint includes team, revision and frozen catalogue; the revision identifies the exact saved choices. An identical accepted command returns that car; another command, stale revision, changed catalogue or unaffordable build cannot replace it. Drafts remain free.

The confirmation record, exact construction debit in the existing ledger, and both cars' frozen presets for **every calendar event** commit in one conditional campaign write before acknowledgement. Personnel command/scout history remains separate. Acquisition validation requires the matching accepted car, command, identity and exact option cost for every construction debit. A completed 18-credit baseline may leave zero; this removes only the already-paid car reserve, never mandatory personnel requirements or arbitrary ledger validation.

The first lobby's construction guard remains until all campaign cars are durable. Its independent write installs those exact presets and removes the guard together. Reads and identical retries repair a lost handoff acknowledgement. The lifecycle advances after the lobby write. Preparation and direct lobby starts remain closed while purchases are pending. This manual path supplies the safe handoff reused by ticket 03's readiness policy.

## Runtime mapping and sources

All four provisional JSON inputs and the 51 supplied asset PNGs were inspected; ticket 01's identity/provenance mappings remain applicable. No car artwork was supplied. Driver portraits and track manifests retain their existing stable IDs and readable fallbacks. No image geometry becomes simulation input.

`ConfirmedBaseCar` retains the complete frozen catalogue, its source/mapping versions, accepted choices, cost and ratings; source history also records car attributes `car-spec/1.0.0/2026.10.08-initial-model` and `construction-handling-axis-v1`. This preserves ticket 01's 625 combinations, 18–24 actual-credit costs and bounded detailed deltas; source stars are never converted into physics.

`ConstructedBaseCar.Apply` materialises ratings rather than adding them to the inherited neutral ratings. Both drivers share the accepted identity. Personnel, assigned engineer and other preset fields are copied unchanged; campaign Pit Crew/conditions are untouched. Reapplication with identical identity/values returns the preset; replacement rejects. Car input versions use `constructed-base-v1/{mechanicalAxis}/{catalogueVersion}`. Existing neutral saved presets retain their previous preparation arithmetic and replay outputs.

The six source handling labels use ticket 01's recorded responsive/stable projection: `(rearStability + tractionConfidence - frontResponse - rotation) / 2`, bounded to -2..2 and rounded away from zero. Construction and Weekend Setup mechanical axes combine once, capped to the existing -2..2 fit range. Existing Adaptability/preferences then supply the pace mismatch once. Setup still adjusts detailed car ratings once. Braking-entry and high-speed handling labels remain content, not invented independent mechanics. Permanent development and the single package remain separate future layers; this purchase does not create either.

Reliability feeds the already-supported explicit incident profile when that policy is enabled. No new incident process or weather/incident foresight is introduced; unsupported legacy records are not reinterpreted.

## Verification and visual review

- Focused domain/preset/personnel regression: **34 passed**, approximately 3 seconds test time; `.tools/reports/domain/s13-02-domain.trx`. Includes zero-credit completion, debit tampering, immutable retry, shared materialisation, source/setup preservation, combined fit, explicit reliability profile and deterministic public race outcomes.
- Initial authenticated confirmation/draft recovery: **4 passed**, 26 seconds test time / 40.146 seconds bounded total; `.tools/reports/s13-02-api.json`.
- Extended confirmation/CAS/lobby handoff recovery: **4 passed**, 38 seconds test time / 50.573 seconds bounded total; `.tools/reports/s13-02-api-final.json`. Actual Auth/Firestore on owned 22609/22600, hub 22610, logging 22660, websocket 22650.
- Browser comparison/edit/affordability rejection/lost draft response/lost confirmation response/reload/identical retry/all-team handoff: **1 passed**, 20.8 seconds test time / 44.674 seconds bounded total; `.tools/reports/s13-02-browser.json`, owned UI 4698/API 4798.
- Reviewed `.tools/reports/gallery/s13-02-confirmed-360.png`: 360×800, clear paid-once/final-revision/locked wording and driver/group content, no horizontal overflow. Review prompted removing disabled editing instructions from the confirmed state and moving the technical car identity into an optional saved-record disclosure.
- Reviewed `.tools/reports/gallery/s13-02-handoff-1920.png`: 1920×1080, readable campaign status and two contrasting circuit cards; private comparison is hidden after refresh. Full-calendar content remains deferred.

Final preparation/race initialization recovery and post-review browser capture results are appended below.

Final results:

- **34 domain/personnel/neutral-preset checks passed**, 4 seconds; `.tools/reports/domain/s13-02-domain-final.trx`. Added independent unaffordable-final-confirmation rejection with an actual 18-credit balance and a previously affordable 24-credit saved draft.
- **Four API recovery cases passed**, 46 seconds test time / **64.096 seconds** bounded total, including before/after committed campaign confirmation, final lobby handoff, preparation initialization and actual constructed race initialization. They also exercise concurrent coordinator retries, outsider/stale/conflicting rejection and the exact single debit after restart. Records: `.tools/reports/s13-02-api-recovery.json` and `.tools/reports/api/s13-02-api-recovery.trx`; bounded launcher PID 18852 owns the completed emulator/test process tree.
- Post-review browser flow **passed**, 19.6 seconds test time / **42.299 seconds** bounded total; `.tools/reports/s13-02-browser-reviewed.json`, launcher PID 34580. Final 360px capture reviewed: paid-once and locked wording is readable, inappropriate editing instructions are removed, and the stable record identity is optional. The 1920×1080 handoff retains the reviewed clear public calendar/status layout. Published only the two inspected captures: [phone confirmation](s13-02-confirmed-360.png) and [public handoff](s13-02-handoff-1920.png).
- Angular application typecheck passed; `git diff --check` passed after trimming generated trailing blank lines. No broad verifier or full backend suite was run in this worker.
Reliability comparison handoff: ticket 03/04 must explicitly enable the existing incident policy in public experiments to measure reliability effects. Standard legacy/default incident settings are unchanged. The player disclosure now states this opt-in limitation directly.
