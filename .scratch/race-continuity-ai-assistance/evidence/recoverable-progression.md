# Ticket 06 — Recoverable live progression

Implemented against public HTTP/SignalR boundaries with real Auth/Firestore emulators; no cloud project, hosted deployment or family cost measurements were available/authorized.

## Behaviour and durable boundaries

Household outcomes persist an eight-second server-owned presentation deadline before publication. The next window opens through scheduled delivery, without a display connection or playback acknowledgement. Standalone designer sessions retain their existing immediate next-window behaviour.

The host can `POST /api/lobbies/{id}/continue` with `{commandId, phase, windowRevision}` and its Firebase bearer token. Public snapshots expose `windowRevision`, `presentationDeadlineUtc` and `presentationRemainingSeconds`; confirmed team locks still use `commandRevision`. Continue saves command identity before publication; retries cannot open a second window. Pause clears scheduled intent and freezes presentation/decision remaining duration; resume produces a new generation and deadline. Recovery pause differs from an intentional host pause: host resume after interrupted progression opens the next decision window directly.

The application saves wakeup generation, phase, window revision, kind and due instant in durable ModuleState. The same manifest CAS updates an indexed scheduled marker. Enqueue happens only after the save and remains retryable if the queue fails. The persisted intent is the outbox until the transition consumes it; repeated enqueue uses the identical Cloud Tasks name and treats ALREADY_EXISTS as success. New generations supersede old intent; stale, duplicate and paused callbacks are no-ops. A stale callback cannot trigger ordinary recovery of a newer overdue phase before generation validation.

Trusted presentation delivery can restore a cold process and open its intended window. Ordinary presentation recovery pauses for the host. A decision callback on a cold process closes at most its already-expired window and durably pauses; duplicate delivery cannot create an unattended burst. Saved choices and policy versions remain authoritative. Failed next-window writes return 503 and restore the last durable state before any successful publication.

`POST /internal/wakeups` accepts `{sessionId,generation,phase,windowRevision}`. `POST /internal/wakeups/sweep` independently discovers only indexed scheduled sessions, retries enqueue and processes due transitions. A cold sweep performs ordinary recovery and never unpauses a recovered race. The sweep is essential: an in-process timer or startup scan cannot repair a crash between commit and enqueue while Cloud Run remains at zero instances.

## Deployment handoff for ticket 07

Production registers `CloudTasksWakeups` when `Wakeups:Queue` is configured. Development alone has a local delivery worker; `Wakeups:Mode=manual` disables that worker for controlled-clock checks. Local delivery grants no HTTP authentication bypass. Production configuration must reject missing scheduling settings rather than accept races with no delivery adapter.

| Setting (environment uses double underscore) | Meaning |
|---|---|
| `Wakeups:ProjectId`, `Wakeups:Location`, `Wakeups:Queue` | Approved Cloud Tasks queue resource |
| `Wakeups:CallbackUrl` | Direct HTTPS Cloud Run `/internal/wakeups`, without query parameters |
| `Wakeups:Audience` | Explicit shared OIDC audience, normally the canonical Cloud Run service URL |
| `Wakeups:ServiceAccountEmail`, `Wakeups:ServiceAccountSubject` | Exact task caller email and service-account `uniqueId`/JWT `sub` |
| `Wakeups:SweepServiceAccountEmail`, `Wakeups:SweepServiceAccountSubject` | Separate exact Scheduler caller email and `uniqueId` |

Both routes validate Google signature, issuer, audience and expiry, plus exact authorized caller subject/email and verified email. Firebase players cannot call either route. A task identity cannot call the sweep and a sweep identity cannot deliver a task. The signing-key provider is the sole replaced identity environment boundary in deterministic tests; real Google signing key retrieval/service-account issuance remains a hosted smoke requirement.

Use the runtime service identity for ADC, grant queue enqueue rights and actAs on the dedicated task signing account, and retain the documented Cloud Tasks service-agent role. Configure one independent Cloud Scheduler HTTP job, suggested frequency once per minute, `POST` to `/internal/wakeups/sweep`, with its dedicated account and **explicit audience equal to Wakeups:Audience**. Keep Scheduler's service-agent role and required actAs provisioning permission. The app validates these routes even when the browser service is publicly invokable; IAM invoker alone does not protect individual public routes. Bound queue concurrency/rate/retries and Scheduler retry duration in the ticket 07 deployment configuration. [Tasks authentication](https://docs.cloud.google.com/tasks/docs/creating-http-target-tasks), [Scheduler authentication](https://docs.cloud.google.com/scheduler/docs/http-target-auth).

The Firestore manifest schedule index is committed atomically with recovery chunks. Replacement persistence adapters must implement scheduled-session enumeration and the schedule-aware Write overload. The household adapter bounds an individual sweep at 1,000 scheduled sessions and reports excess as an operational error rather than silently omit work. Finished/paused sessions leave the index. An upgrade of older saved races without a schedule marker requires a host pause/resume to create a fresh intent; preserve their histories and use compatible model/schema versions.

## Focused verification and visual assessment

- TDD baseline: disconnected-display test failed because the previous server immediately opened the next window. Callback-access baseline returned 404; sweep-gap baseline retained phase zero. Each then passed after its corresponding implementation.
- Progression API suite: **17 passed**, including actual Firestore process replacement, trusted presentation coldstart, ordinary restart pause, one-window decision recovery, stale/new-window delivery, pause/resume generations, failed enqueue plus sweep, failed durable window save/retry, and exact OIDC principal/claim rejection.
- Directly affected existing tests: **7 DeadlineTests, 3 MultiTeamTests, 3 HostControlsTests passed**. Household tests use explicit host continue after results; controllable deadline factories disable local automatic delivery. Legacy standalone AutomaticWindowTests passed after excluding only the changing clock sample from its whole-state comparison. No broad verification run was duplicated.
- Focused browser: **1 passed** at frontend 4496/API 4596, Auth 19329/Firestore 19320, hub 19530/logging 19580. It accepts a human lock, advances the missing-player deadline, continues presentation and checks the current household/public surfaces.
- Inspected [360px controller](progression-phone-360.png) and [1920×1080 display](progression-display-1920.png). No horizontal overflow; the public display fits all 22 rows. Phone content scrolls vertically and retains the existing compact classification/narrow image fallback; this ticket did not change visual layout.

Reports remain ignored under `.tools/reports`: `progression-red.json`, `progression-auth-red.json`, `progression-sweep-red.json`, `progression-affected.json`, `progression-legacy.json`, `progression-final.json` and `progression-browser.json`. Final progression TRX is `.tools/reports/progression-final-tests/progression.trx`. Browser PID 21692 and test PID 45252 completed normally. Emulator launch PID 27164 is recorded in `progression-emulators.json`; owned shutdown is recorded separately. Ports/checks refer to this worktree only; root will pin the final integrated SHA and broad verification once.

## Scheduling costs and limitations

Cloud Scheduler currently grants three jobs per billing account per month; later jobs cost USD $0.10 per 31 days, and paused jobs still count. Scheduler executions are not separately charged by Scheduler, but every sweep invokes Cloud Run and can read Firestore, so a frequent empty sweep can dominate cold-start runtime and affect the free allowance. Budget actual runtime, not just phase-resolution CPU. [Scheduler pricing](https://cloud.google.com/scheduler/pricing).

For two races with 10 phases each, two wakeups/phase would mean roughly 40 task creates and 40 initial deliveries before retries; this is arithmetic, not measured usage. Repeated outbox enqueue attempts and duplicate deliveries add operations. Cloud Tasks has a first-million-operations monthly allowance but does not guarantee exact execution time/order or prevent every duplicate; tasks cannot be scheduled beyond 30 days and expire after 31 days. Pause removes active intent and resume schedules a new near-term task. [Tasks pricing](https://cloud.google.com/tasks/pricing), [limits](https://docs.cloud.google.com/tasks/docs/quotas), [delivery limitations](https://docs.cloud.google.com/tasks/docs/common-pitfalls).

See [hosting research](../hosting-research.md) for websocket billing, Firestore limits, regional/account allowance caveats, rollback compatibility and required hosted evidence. Cloud Tasks/Scheduler IAM, scale-to-zero timing, actual retries/latencies and family cost remain unverified until ticket 07 obtains an approved project and executes its hosted smoke/playtest.
