# 02: Lock multiple teams and resolve together

**What to build:** Several players privately manage both cars and the race resolves exactly once after every required team locks.

**Blocked by:** 01 — Join a household lobby and claim a team.

**Status:** ready-for-agent

- [ ] Authorised private controllers receive their legitimate team observations; spectator displays receive public Classification, lifecycle and per-team readiness only.
- [ ] Validate current authenticated membership and assignment on every command/private subscription; prevent cross-session or wrong-team access.
- [ ] Preserve atomic two-car commands, command identities and expected phase/revision. The first valid team lock is final and visible across authorised devices.
- [ ] Freeze scripted choices from phase-start observations before human locks; resolve only when the complete frozen required set is ready.
- [ ] Concurrent/repeated/stale/conflicting submissions neither replace accepted locks nor resolve twice; rejected input changes no lock or driver state.
- [ ] Missing players leave the phase waiting; membership or revocation cannot silently shrink the required set. Deadlines and fallback remain deferred.
- [ ] Exercise differing lock orders and concurrent locks through API/SignalR, plus a real-browser two-private-controller flow and public/private payload checks.
