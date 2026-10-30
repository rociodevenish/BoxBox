# Review fixes

Based on integration `db5db9e`; merged integration `0cc4366` before completion. One isolated worktree/branch: `codex/player-home-fixes`.

- Anonymous Quick join participants now see the existing account sign-in/registration form. Campaign setup also requires a permanent account and retains its allowlisted destination through authentication. Root and create destinations both work after guest joining. Existing Firebase account-switch behavior and UID-scoped device keys preserve authority; no guest device grants are transferred to another account.
- Firestore startup walks document-name ordered pages. `Persistence:MembershipMigrationLimit` bounds one page (1-10000), never the deployment total. Legacy recovery metadata still uses version-conditional, metadata-only commits with bounded retry; indexed records need no recovery reads or writes. New application saves carry membership metadata atomically, including concurrent saves. Unavailable storage/repeated write races still fail startup rather than expose incomplete discovery. Restart is safe and reaudits all pages.
- Discovery and return share a narrow private current-campaign-player resolver. Return retains its separate original-membership/current-event error messages and all existing device authority handling.

## Focused verification

Approved seams: real Playwright frontend/API/Auth-emulator journeys; existing real-token API/store integration tests. Node 24 bundle, SDK 10.0.402, Java 21, and installed Chromium. Isolated UI4490/API4590; Auth19709/Firestore19700/hub19710/logging19760/websocket9270. All processes were owned by bounded runners and stopped after checks. Reports are retained under `.tools/reports/player-home-fixes/` in the integration workspace.

- `backend-red.json`: new page-size-one legacy regression failed with the old total-record-limit IOException; existing two listing cases passed.
- `backend-green.json`: 4/4 CampaignDiscoveryTests passed. Covers volatile/durable discovery and privacy, authoritative first/current membership, completed two-event recovery/results, legacy records across multiple size-one pages, indexed-only records above the audit page size, unchanged indexed manifest versions on repeat audit and store restart, and unchanged recovery payloads.
- `browser-red.json`: actual Quick join created an anonymous user; subsequent Sign in form disappeared after Auth initialization and its button detached, preventing account authentication.
- `browser-green.json`: 5/5 passed. Existing registration/subsequent-sign-in create destination plus guest Quick join followed by existing-account sign-in and new-account registration, each returning to root and campaign creation. Angular compilation included both changed templates.
- `git diff --check` passed. No broader checks repeated because the parent owns the integrated regression run.

The startup audit remains proportional to stored manifest count, with bounded response memory. Concurrent legacy writers from an older deployment must be stopped during upgrade; all current deployment writes include membership metadata atomically. There is no newly introduced total-record deployment ceiling.
