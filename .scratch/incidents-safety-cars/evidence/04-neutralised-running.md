# Ticket 04 evidence

Claim: `codex/s09-04`, based on integration `98665e013f3d3591462178bec832691dfe5538a7`. Approved seams: public simulation, session HTTP/SignalR and rendered browser. [Runtime rules](../../../docs/neutralised-running.md) record the bounded coefficients, route mapping, exceptions and version compatibility.

The first public slice required a missing ResolveSafetyCar operation; implementation then passed actual compression and fractional restart. Subsequent focused slices cover real route/service loss, lapped order, queued/ongoing segmented commitments, continuing failures and finish. API checks initially exposed the old staging lifecycle expectations; updated travel/restart assertions now verify real time advance, accepted-command retry, recovered state, membership and typed replay. A dedicated compatibility case verifies v1 designer records retain zero-time staging while v2 cannot silently match that outcome.

Unique focused checks, with no broad regression pass:

- 29 simulation checks: 7 new neutralised cases plus existing interruption, special choice and segmented pit cases. `.tools/reports/s09-04/simulation-command.json`.
- 5 API checks: 4 standalone/designer cases in `api-final-command.json` plus the shared lock/deadline recovery case in `emulator-command.json`. Shared check used owned Auth19379/Firestore19370 with hub19580/log19630/ws9161. The session store in these checks is the conditional in-process store; complete Firestore disruption restart coverage remains ticket 05.
- 2 browser flows: narrow two-car choices → neutralised running → normal choices, refresh and queued stop preservation with actual public/private subscriptions and reduced motion. `browser-command.json`, UI4524/API4624.
- Angular typecheck passed in `typecheck-command.json`.

Inspected captures: [360px special choices](safety-car-360.png), [360px full restart flow](restart-360.png), [1920×1080 public display](neutralised-restart-1920.png). Both car identities, compound/pace controls, ongoing commitment and normal next-span instructions remain readable without horizontal overflow. The desktop table and pit highlights are readable. The current completion summary remains in the existing development Race facts panel; full public incident/status presentation is ticket 05. Captures are full page; the development panel extends below the 1080px display viewport. No human playtest or disruption-comprehension claim is made.

Owned process records: simulation22520, API28552 and final API17712, emulator14592, browser26536, typecheck27084. Emulator run completed with shutdown; Playwright completed and closed its servers. Final listener inspection found none on4524/4624/19379/19370/19580/19630/9161. Custom emulator config and copied rules are preserved in `.tools/reports/s09-04/`. One initial config attempt refused rules outside its project directory; the passing run copies rules inside that directory. No global process kills, cloud credentials, push or PR.

Limitations: live risks remain off; all live tracks need explicit authored routes before activation. Ticket 05 owns opening integration, v1 saved-session staging migration and complete Firestore retry/restart lifecycle evidence. Ticket 06 owns paired-seed strategy/frequency and human timing evidence. Ordinary/S08 null-route paths retain their versions and optional-field omissions.
