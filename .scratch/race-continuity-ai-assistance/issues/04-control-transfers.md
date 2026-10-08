# 04: Transfer control at safe phase boundaries

**What to build:** A host assigns temporary or permanent AI control to an unavailable player's team and an authorised player can return without replacing committed choices.

**Blocked by:** 03 — Continue through deadlines and missed decisions.

**Status:** ready-for-agent

- [ ] Host-requested temporary/permanent takeover takes effect at the next uncommitted phase boundary, preserving player/team identity and existing locks.
- [ ] Authorised human reclaim is boundary-scoped and cannot overwrite accepted human or automatic decisions.
- [ ] Public control status explains who is responsible; membership and controller authority remain separate from AI status.
- [ ] Persist control revisions and transitions so restart retains the same takeover/reclaim outcome and revoked devices cannot act or receive private updates.
- [ ] Verify takeover, timeout fallback, reclaim, concurrent requests and accepted-lock preservation through API/SignalR and rendered controller status.
