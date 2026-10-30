# 03 — Campaign discovery and resume evidence

Implemented on `codex/player-home-03`, based on integration `f44ae5420fae6dd4c4cd841647be843856f1bf05`; merged the completed join slice and latest tracker integration `b8fc02a` before delivery.

Authenticated home now distinguishes **Campaigns you host** (reopen) from **Campaigns you joined** (reenter), with Create campaign and Join another game retained. The standalone `CampaignResume` component accepts the home's existing `User|null`; it does not initialize authentication. Loading, empty, retry and signed-out states are distinct. Account changes abort requests, clear summaries immediately and invalidate both token and response continuations. Links use the existing `/campaigns/{id}` authenticated Return path, including completed results and boundary-safe control.

`GET /api/campaigns` authenticates the caller and returns exactly `campaignId`, `hostName`, `role`, `lifecycle`, `eventNumber`, `trackName`, with `private, no-store` caching. No participant UIDs, join codes, device grants, private setup, strategy or personnel decisions are returned. Host name is labelled as the household host, rather than inventing a campaign name. A short stable game identifier distinguishes otherwise similar campaigns.

Both volatile and Firestore adapters maintain account membership indexes atomically with the authoritative conditional record write; the volatile adapter uses an inverted UID-to-session index and Firestore uses `ARRAY_CONTAINS`. Discovery never uses scheduled-wakeup enumeration. Application-owned recovery metadata identifies candidate first weekends using the versioned authored calendar identity; campaign integrity and both first/current-event membership are rechecked before a summary is disclosed. An index entry alone never grants access.

Legacy Firestore records are covered by the bounded startup audit documented in `docs/agents/verification-workflow.md`. Metadata-only CAS patches preserve saved payloads/chunks and existing join/schedule metadata. The audit reads document-name ordered pages of 1000 manifests by default (configurable 1 to 10000 per page), without a total-record ceiling. Unavailable storage or repeated concurrent-write races fail startup; they do not silently omit old campaigns. Restart safely audits again, skipping recovery reads and writes for indexed manifests. Volatile storage still loses records when the backend restarts.

## Verification

Agreed seams: real-token HTTP API/store integration and real frontend/API/emulator Playwright journeys. No component-state tests or mocked authentication.

- API red: `.tools/reports/resume-red.json` — both adapters failed at the absent listing route (405 instead of authenticated 403).
- API green: `.tools/reports/resume-final-api.json` and `resume-final-api-command.json` — 3 cases cover volatile/durable listing, safe summary keys, account isolation, joined members, fresh-device return, authoritative membership removal, actual two-event completed campaign, restart/results, legacy manifest metadata removal/backfill, idempotence, paged migration preserving indexed record versions, unchanged recovery payloads, stale index rejection, and current-event membership correction.
- Browser red: `.tools/reports/resume-browser-red.json` — missing hosted section on home.
- Actual-home browser green: `.tools/reports/resume-browser-final.json` — fresh browser sign-in, hosted discovery and Return, retry, delayed loading response, sign-out clearing and second-account empty state. A temporary initial harness was removed before integrating the real home.
- Member browser green: `.tools/reports/resume-member-browser.json` — account joins another host's campaign, separate member group, reentry via existing Return, and Join another game remains visible. A prior combined run used an obsolete join-button locator; only that test locator was corrected and its affected case rerun.
- Named integrated check: `node tools/verify.mjs player-home-resume` (strict owned lifecycle). Integration folder excluded from the ordinary gallery. Parent owns the single integrated broad pass.

UI/API ports: **4485 / 4585**. Owned emulator ports: Auth **19639**, Firestore **19640**, hub **19930**, logging **19980**, websocket **9193**. Node24.22.1, SDK10.0.402, Java21 and the existing pinned Chromium were used. Bounded command manifests record launched PIDs and cleanup; no preview is retained by this slice.

## Visual inspection

Inspected the complete **360×800** phone flow and **1920×1080** desktop viewport (desktop exported full-page). Both show readable household context, a distinct hosted group and large resume/entry actions without horizontal overflow. The phone card stacks its action; desktop lays it beside the context. Captures were reviewed before the parent's broad pass and explicitly published:

- `s20-03-resume-360.png`
- `s20-03-resume-1920.png`
