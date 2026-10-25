# S11 integration scope

Completed automated implementation, verification and review: [final record](final/README.md). The sequencing below is the original integration plan; actual household and hosted observations remain pending.

Implement the preset campaign on `codex/campaign-championships`, based on completed S10 `de0340be6060578699d36884fa5ba724e3ba8dba`. Earlier engineering, exact replay and independent reviews are complete; pending household understanding/pacing and hosted acceptance are not represented as measured prerequisites.

The five-ticket graph is sequential: durable short campaign → scoring/standings → reports/next-event preparation → recovery/season completion → evidence. Use isolated implementation worktrees, independent merges, public-boundary TDD, and one integration branch. The parent explicitly approves public campaign rules, authenticated API/SignalR, real persistence/restart and focused browser testing; no new seam approval is needed.

Stable campaign, person, constructor and event identities must remain distinct from devices and live weekend IDs. Freeze versioned rosters/cars and the ordered fixture calendar. Use the existing authoritative weekend engine, preparation policy, membership/privacy and control boundaries. Constructor identity is presentation/competition identity, not performance. Full-calendar mode remains disabled until its complete content manifest is validated; the short authored fixture makes no current real-world calendar assertion.

Campaign/event revisions must guard single active-event creation and exactly-once result contributions, including uncertain writes, concurrent requests, retries and recovery. Store replayable weekend inputs/results and model/content versions, not engine internals. Keep raw Classification, status, distance and eligibility beside scoring. Points and tie-breaks are explicitly versioned game rules; no current sporting-regulation claim or points promotion is inferred.

Between events preserve private drafts, one Practice Assessment and final locks. Future readiness cannot replace current accepted locks. Missing-team fallback is disclosed and recorded; authorised reclaim and host AI takeover reuse existing boundaries. Public calendar/readiness/reports/standings stay separate from private setup, recommendations and diagnostics.

Inspect current supplied assets and source references, preserve originals/provenance, and use stable identities with live text/fallbacks. If domain terms change, use one root glossary/shared ADRs. No acquisition, development economy, transfers or expanded race regulations are included.

Before the single broad verification, review a complete360px phone flow and actual1920×1080 public display. New integration folders need default-gallery exclusion and dedicated owned configs. After failures, rerun only affected/remaining named stages. Independent Standards and Spec reviews use baseline `de0340be6060578699d36884fa5ba724e3ba8dba`; one remediation owner handles all findings.

Preserve immutable command/PID/port records, content/model/scoring versions, frozen histories and hashes before worktree removal. Use pinned Node24.19.0, global.json SDK, Java21, Chromium cache and explicit local demo emulator hosts. Escalated named-port inspection is required because sandboxed inspection can hide listeners. Preserve the existing personal-qualifying preview (Firebase25928/Java30036 on default recovery ports); use owned configurations and the existing backend/shared/assistance/deadline/control overrides. Invocation-only NuGetAudit=false leaves the recorded SignalR10.0.12 advisory unresolved.

Actual multi-day household joining/readiness/reclaim clarity, progression findings and hosted usage/cost require real observation. Automated storage-operation/size measurements and elapsed test time are labelled separately; no household or cloud cost result is invented. No push, PR, deployment or S12 work is requested.
