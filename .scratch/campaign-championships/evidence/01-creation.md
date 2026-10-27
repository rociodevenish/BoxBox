# S11 ticket 01 — durable short campaign and first weekend

Implemented on `codex/s11-01`, based on clean S10 `de0340be6060578699d36884fa5ba724e3ba8dba`; merged integration documentation tip `f9cb3dd` before completion.

## Answer and public seams

The real API host registers CampaignCoordinator and ICampaignStore. Campaign records use an explicit `campaign-record-v1` schema and `campaign-` storage key prefix through the existing ISessionStore CAS adapter; they have no join code or scheduled wakeup. Calendar/event/session identities are reserved in the campaign record before idempotent lobby creation. Membership, ownership, private preparation, practice limits, final-ready, qualifying, AI responsibility and reclaim retain the existing household operations. The campaign never creates a standalone global race or exposes engine/module state.

Creation → event preparation → active weekend is observable through authenticated `/api/campaigns` operations and `/campaigns/new`. Concurrent starts reuse the recorded start identity and reserved lobby. A restart or uncertain write retries the same operation; no second weekend can be created. Direct lobby start is blocked for campaign lobbies; direct preparation cannot replace the frozen circuit or source inputs. Scoring, second-event progression and full recovery UX are left to subsequent tickets.

## Frozen content and source boundaries

`authored-short-monza-monaco-v1` is an authored two-event fixture ordered Monza then Monaco, not a representation of the current real-world calendar. Source event references reviewed: provisional `event-italian-gp-2026` → `track-monza`; `event-monaco-gp-2026` → `track-monaco`. Dates and sporting/calendar assertions in the source are not published as verified facts. Full calendar remains rejected until an approved complete manifest exists.

Every event freezes all eleven WeekendPreset pairs, all twenty-two person identities, neutral car ratings, engineer/source/model versions and evaluated event conditions before opening preparation. The source track/driver schema/data versions are `1.0.0/2026.10.08-initial-balance`; car attribute vocabulary is provisional `1.0.0/2026.10.08-initial-model`. Preset car input is the existing neutral detailed-rating fixture (82 per supported attribute), independent of constructor metadata. Detailed driver/car inputs retain their 1–100 design-point bounds; detailed track demand is 1–100, race characteristics 1–5, track length is converted from km to metres by the existing catalog. No headline stars are converted to physics. Existing WeekendPreparation validation and authored mappings apply.

Supplied driver/track originals were inspected and preserved. Presentation maps stable track IDs through `public/tracks/manifest.json` and stable person IDs through `public/portraits/manifest.json`; existing webp runtime variants and provenance are reused. Readable circuit, constructor and person names survive missing imagery. Images provide presentation only, never geometry.

## Verification

Public API test first failed at the missing campaign endpoint (404), recorded in `.tools/reports/s11-01/red.json`; creation/restart retry then passed 1/1. Expanded final focused suite passed **12/12** against actual Auth/Firestore emulators. It checks frozen creation, gated full-calendar input, authenticated membership/private-payload filtering, legitimate readiness, concurrent starts, direct-start/circuit-switch rejection, four creation reservation/lobby failures and six start reservation/qualifying/publication failures, each before/after durable commit and host restart. No internal storage-layout assertions are used.

Final API command: `dotnet test tests/Game.Api.Tests --filter FullyQualifiedName~CampaignTests --artifacts-path .tools/s11-artifacts -p:NuGetAudit=false --nologo`, owned by bounded Firebase invocation `.tools/reports/s11-01/api-final.json`, launcher PID **21796**, passed.

Dedicated `playwright.campaign.config.ts` and `tests/campaign` are excluded from the default gallery. Final browser flow passed **1/1**: permanent account → campaign/calendar → claim → preparation → practice/final-ready → reload → first qualifying. Images load, the phone has no horizontal overflow, and readiness survives refresh. Final owned report `.tools/reports/s11-01/browser-final-2.json`, launcher PID **36756**, passed. Angular compiler typecheck passed.

Reviewed captures: `s11-01-calendar-360.png` (360×800 full flow), `s11-01-prepared-360.png` (360×800), and `s11-01-preparation-public-1920.png` (actual 1920×1080 public display). Calendar cards, live names, readiness and controls are readable; no horizontal overflow or private setup appears on the public display. Captures were copied with the bounded evidence publisher, not by rewriting historical evidence.

Node 24 bundled runtime and Java 21 were used. Emulator config is `tools/auth-emulator/firebase.campaign.json`; owned UI/API/Auth/Firestore/hub/logging/websocket ports are 4541/4641/19579/19580/19830/19880/9177. Existing unrelated preview ports and PIDs were preserved. Final listener inspection is recorded at completion. No broad suite was rerun here; integration owns the one broad pass.
