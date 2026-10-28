# 05: Manage host controls and controller access

> GitHub issue: [#89](https://github.com/rociodevenish/BoxBox/issues/89) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A host pauses and resumes shared play, revokes controller access and lets authorised devices refresh to current state safely.

**Blocked by:** 03 — Share a tablet with private handoffs; 04 — Play in Family and Mixed privacy modes.

**Status:** resolved

- [x] Host pause/resume controls decision-window availability without cancelling committed resolution or inventing choices for absent private teams.
- [x] Resolution automatically opens the next decision window exactly once unless paused, without a separate Continue action; disconnected display playback cannot block progression. An authorised host resume opens a paused window once.
- [x] Revocation invalidates obsolete assignment authority and closes private subscriptions; clients explain lost permission and stop obsolete editing.
- [x] Permission checks remain current on every command/subscription; revoked devices receive no further private updates and cannot submit.
- [x] Refresh/resubscription retrieves membership, assignment, readiness and lifecycle revisions within the server lifetime without reopening accepted locks.
- [x] Preserve the active phase's frozen required set; mid-race takeover and durable server restart remain deferred.
- [x] Verify host/spectator restrictions, automatic progression, pause/resume, revocation, missing-player waiting and refresh through public API/SignalR and focused browser flows.

## Comments
Claimed by ticket05 on codex/shared-05 from integration 5565a9b. Approved seams: HTTP/SignalR and focused browser flows. Human household feedback remains pending under ticket 06.
## Answer

Hosts can pause or resume play from the household lobby and revoke each explicitly listed controller device. Pausing a current decision window prevents editing while retaining accepted locks and the frozen required teams. Pausing committed resolution allows its outcome to complete and holds the next opening; resume opens that window once. Repeated and concurrent resume commands do not advance an additional phase. Controller and public screens explain paused play, with outcome playback remaining independent of server progression.

Revocation removes all primary/additional team grants for the selected device, rotates affected assignment revisions and removes obsolete private SignalR group memberships. It preserves participant ownership, accepted locks and required teams. Commands and subscriptions still check current authenticated membership and explicit grants; shared UID does not implicitly grant another device access. Host-only device metadata identifies the account, device and affected team count. Public lobby revision notifications refresh current authority; obsolete private data, drafts, acknowledgements and edits are cleared. Shared tablets return to neutral, and late private messages cannot restore revoked views. Remaining authorised devices resubscribe at the new revision. Legitimate Open spectator views refresh metadata and Open subscriptions after authority changes; Open projection filtering and its notification signature remain unchanged.

Refresh restores current membership, assignment, readiness and lifecycle within this server lifetime, preserving accepted locks. Joining another device advances lobby metadata revision. No absent-team fallback, takeover or durable restart support was introduced.

## Evidence

- Implementation SHA: `68c4a37cf4a449695a2327a8b22df6bb22f2b2a5`, based on and including integration `5565a9bdc0f06717e4d240332c461b5b57a88a39` (read-only ancestry confirmed). Broad integration verification remains ticket 06's responsibility.
- Vertical HTTP/SignalR TDD: pause tracer failed with 404 before implementation and passed afterwards. Revocation tracer failed with 404 after correcting its import/syntax, then passed. Final `.tools/reports/host-api-final.json` (PID 25280, exit 0) passes all three host cases: two human teams retain the first accepted lock through pause/refresh/resume and wait for the second; revoked primary devices cannot read/submit/resubscribe while the explicitly granted other device retains access and obsolete streams stay quiet; a pause requested after the public resolving notification allows one outcome cursor, eight concurrent resumes yield exactly phase 1, and retry does not reopen or resolve again. Spectators cannot pause, resume, revoke or list controller devices. Paused snapshot republications reuse the same outcome cursor; the initial raw snapshot-count assertion was corrected to count unique outcomes.
- Affected verification `2026-10-10T21-09-29-263Z-27e9e775-d2c1-495e-817e-1ffbf70dab70`: frontend typecheck/build and 110 simulation tests passed; 41 API cases passed and the concurrency tracer's raw snapshot-count assertion failed. The final focused three-case run above passes that corrected assertion; no unnecessary full backend rerun followed a test-only correction.
- Household verification `2026-10-10T21-10-22-564Z-f873a3d1-9490-48a0-8124-134fa816f1d9`: typecheck and six household browser flows passed. Final affected frontend compile/build `2026-10-10T21-12-16-977Z-f8007575-34d5-4dea-a24d-34ddd288ffeb` passed after generation guards. Existing initial-bundle and qrcode CommonJS warnings remain.
- Final focused `.tools/reports/host-browsers-final.json` (PID 37284, exit 0): four affected flows pass. The 360px phone pauses, refreshes paused, resumes, locks two cars, automatically receives the next decision window without any display connected, loses private controls immediately on revocation and remains denied after reload. Active shared-tablet revocation clears both car views and Ready controls, including a late real private frame. Family and Mixed spectators refresh revised Open subscriptions after live revocation, retain accepted legitimate Open instructions and receive subsequent pause/resume state; Private teams remain absent.
- Inspected and published [paused 360px phone](../evidence/host-paused-360.png): clear paused explanation, both cars contained, disabled approval, no horizontal overflow. [resumed 360px phone](../evidence/host-resumed-360.png) shows the automatic next window after outcome playback. Full-page captures preserve both-car controls and existing portraits; compact timing headings retain their existing density.
- Inspected [paused 1920x1080 public display](../evidence/host-phase-1920.png): explicit host-resume wording, public readiness and all 22 timing rows fit. Corrected wide control wrapping after the initial capture cut off the final rows; the focused browser asserts the last row ends within 1080px. No private estimates or commands appear.
- Verification used Node 24.19 and the repository .NET SDK. Command records preserve owned PIDs/identities, elapsed times and exits. UI/API 4471/4571 and auth 19099/19400/19450 have no listening services retained; emulator lease released. Dependency-junction/worktree cleanup stays with the integration owner after merge.
- Actual human household playtest feedback remains pending under ticket 06 by explicit user direction. Browser evidence does not substitute for human findings.
- Automatic approval review rejected an attempted merge of the integration branch into this worker branch, interpreting the instruction not to merge integration as a prohibition. Read-only ancestry proves the current integration tip is already included, so no merge was needed and no task work remains blocked.
