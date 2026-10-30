# S14 — independent review fixes

Base: `183268b23e9120fefdb14e8bba32a8d572433547` on `codex/s14-review-fixes`.

## P2: respect saved Help Privileges

Development now projects the assigned player's canonical `PlayerAssistanceSettings` from the authorised current/next campaign lobby. Standard returns no Advice and no strategic policy-history explanations, and the recommendation command rejects Standard on the server, including direct requests. Manual options, free drafts, Save, ordinary transaction/project facts and commitment remain available. Assisted/Guided retain their intended recommendations and explanations.

The private UI initializes from the saved server setting, includes Standard, and updates through the existing host-authorised `/api/lobbies/{id}/players/{playerId}/assistance` command. It preserves the existing AutoAcceptRecommendation value, then reloads the authorised private projection. Non-host controllers see their saved level and cannot change it through this UI; existing server host/controller checks remain authoritative. Changing assistance does not modify or commit a saved development draft. Next-event lobby creation carries the existing per-player assistance records forward with the copied player identities, preserving the campaign's saved privileges after the event boundary and restart.

## P3: remove duplicated frontend option rules

Removed the frontend option-id cascades and their implicit default compromise/specialty. The cards render the saved `quote.option.compromise`, with a generic attribute-label transformation for readability. Supplemental private snapshot specialties come directly from the existing production `DevelopmentPersonnelMapping.Specialty` operation. The client no longer reconstructs either rule from option IDs. No domain option, frozen quote field/hash, outcome operation, archive fixture or experiment output was changed.

## Focused red/green verification

- Public API regression first failed in both durability cases because Standard returned the mechanical-refinement recommendation (`Assert.Null` failure). `.tools/reports/s14-review-api-red.json` retains the red run.
- Final **2/2** focused API cases pass: saved Assisted survives next-lobby creation; Standard suppresses guidance and rejects recommend; Assisted/Guided enable it; switching to Standard retains the free saved draft and hides prior advice; saved Standard and Guided both survive fresh-host restart; non-host help updates are forbidden; the pre-existing automatic-spending and durable retry assertions remain intact. Reports `.tools/reports/s14-review-api-green.json` and `s14-review-api-test.json`.
- Final focused browser checks Standard by default, no suggestion/action, free draft retention across saved Standard/reload, host canonical switches through Assisted/Guided, authoritative compromise text, explicit investment, automatic team actions, returning summary and private clearing. Captures and result are recorded below after inspection.
- Angular compiler and API/browser fixture builds pass. No broad suite, archive regeneration or unrelated outcome/recovery matrix was run. Parent owns affected named-step verification after merge.

API final runs owned Auth23109/Firestore23100/websocket23150/hub23410/log23460. A green startup refused occupied22109/22100 diagnostic ports; it did not reuse or stop those listeners, then moved to the new owned configuration. Browser uses owned Auth22209/Firestore22200/websocket22250/hub22510/log22560, UI4496/API4596. Configurations/artifacts stay in this worktree's `.tools/reports`; the temporary browser config is preserved there and not committed. Root verification ports and files remain untouched. Bounded manifests record launched PIDs and owned-tree shutdown. SDK10.0.401/Node24.22.1/Java21; restore uses NuGetAudit=false with unchanged dependency pins.

Final focused browser result: **1/1 passed**, `.tools/reports/s14-review-browser.json`. Inspected [saved Standard after reload](s14-review-standard-360.png) at360×800: no strategic recommendation or suggestion action, all manual options and the preserved revision2 draft remain available. Inspected [authoritative option detail](s14-review-authoritative-option-360.png): the quote's exact full-success compromise/floor is rendered readably, and the specialty is supplied by the shared mapping. Text and controls wrap without horizontal overflow. Assisted/Guided switching and the Guided explanatory steps were exercised in the same real flow. Transitional screenshot artifacts remain ignored; only these two stable reviewed captures are published. The flow also retains its existing public1920 readiness and returning-summary checks.
