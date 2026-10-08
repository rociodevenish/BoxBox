# 01: Play one authoritative Race Phase

**What to build:** A local playtester opens separate browser display and controller views, chooses instructions for both cars and sees one complete authoritative Race Phase against ten scripted teams.

**Blocked by:** None (can start immediately using fixtures while segment evidence is reviewed).

**Status:** resolved

- [x] A documented local start procedure runs Angular, an ASP.NET Core session server and the existing infrastructure-independent race engine without cloud services.
- [x] Create one active in-memory session from allowlisted presets, distance and designer seed with 11 teams, 22 cars, fixed rolling order, gaps and fresh tyres.
- [x] Return a public display link and separately scoped team controller access; a session identifier alone grants no private observations or command permission.
- [x] Keep player, team and device concepts separate. Public Classification and private legitimate team observations use explicit allowlisted contracts, never raw diagnostic phase results.
- [x] The controller submits complete choices for both cars atomically; opponents use versioned scripts frozen from legitimate phase-start observations and hidden until resolution.
- [x] Resolve through the existing engine and show its exact phase outcome, with authoritative awaiting/resolving/result lifecycle states and session/phase/revision identities.
- [x] API/SignalR checks and a small real-browser smoke flow verify both-car submission and observed phase results, including access restrictions.


## Answer

Implemented Game.Application/Game.Api session authority and a real one-phase Angular browser path. Separate capabilities protect team snapshots/commands; complete plans validate atomically; frozen legitimate opponent scripts resolve via the existing engine. Explicit DTOs exclude diagnostic/private leakage. See [run procedure](../../../docs/browser-dry-race.md) and [verification evidence](../evidence/01-authoritative-phase.md).

Verification: 108 simulation tests and 4 API/SignalR tests pass; 34 gallery tests and 1 real-server browser smoke pass; Angular typecheck and production build pass (262.28 kB raw / 68.79 kB estimated transfer). Later tickets cover retry/reconnect, further phases and polished integration. Reviewed capture dimensions and current display row clipping are recorded without claiming full TV fit.
