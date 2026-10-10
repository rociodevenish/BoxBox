# 05: Manage host controls and controller access

**What to build:** A host pauses and resumes shared play, revokes controller access and lets authorised devices refresh to current state safely.

**Blocked by:** 03 — Share a tablet with private handoffs; 04 — Play in Family and Mixed privacy modes.

**Status:** ready-for-agent

- [ ] Host pause/resume controls decision-window availability without cancelling committed resolution or inventing choices for absent private teams.
- [ ] Resolution automatically opens the next decision window exactly once unless paused, without a separate Continue action; disconnected display playback cannot block progression. An authorised host resume opens a paused window once.
- [ ] Revocation invalidates obsolete assignment authority and closes private subscriptions; clients explain lost permission and stop obsolete editing.
- [ ] Permission checks remain current on every command/subscription; revoked devices receive no further private updates and cannot submit.
- [ ] Refresh/resubscription retrieves membership, assignment, readiness and lifecycle revisions within the server lifetime without reopening accepted locks.
- [ ] Preserve the active phase's frozen required set; mid-race takeover and durable server restart remain deferred.
- [ ] Verify host/spectator restrictions, automatic progression, pause/resume, revocation, missing-player waiting and refresh through public API/SignalR and focused browser flows.

## Comments
Claimed by ticket05 on codex/shared-05 from integration 5565a9b. Approved seams: HTTP/SignalR and focused browser flows. Human household feedback remains pending under ticket06.
