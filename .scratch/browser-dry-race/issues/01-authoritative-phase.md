# 01: Play one authoritative Race Phase

**What to build:** A local playtester opens separate browser display and controller views, chooses instructions for both cars and sees one complete authoritative Race Phase against ten scripted teams.

**Blocked by:** None (can start immediately using fixtures while segment evidence is reviewed).

**Status:** ready-for-agent

- [ ] A documented local start procedure runs Angular, an ASP.NET Core session server and the existing infrastructure-independent race engine without cloud services.
- [ ] Create one active in-memory session from allowlisted presets, distance and designer seed with 11 teams, 22 cars, fixed rolling order, gaps and fresh tyres.
- [ ] Return a public display link and separately scoped team controller access; a session identifier alone grants no private observations or command permission.
- [ ] Keep player, team and device concepts separate. Public Classification and private legitimate team observations use explicit allowlisted contracts, never raw diagnostic phase results.
- [ ] The controller submits complete choices for both cars atomically; opponents use versioned scripts frozen from legitimate phase-start observations and hidden until resolution.
- [ ] Resolve through the existing engine and show its exact phase outcome, with authoritative awaiting/resolving/result lifecycle states and session/phase/revision identities.
- [ ] API/SignalR checks and a small real-browser smoke flow verify both-car submission and observed phase results, including access restrictions.
