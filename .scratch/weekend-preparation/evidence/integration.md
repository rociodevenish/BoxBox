# S07 integration record

Implementation starts on `codex/weekend-preparation`, based on `9a54a39db0fa5caa8fada77508da446ce21ad795`. This includes the locally verified S06 implementation and the user's committed stable roadmap/spec codes. The root working tree was clean at S07 start.

## Scope and acceptance limits

The user explicitly requested moving to S07 after committing the remaining edits. S06 ticket 07's hosted deployment, household playtest and measured cost evidence remain pending; they are not represented as completed prerequisites. S07 proceeds with authorized local implementation and verification. Its own player comprehension, preparation duration and hosted acceptance must be recorded separately from deterministic correctness.

S07 uses authored presets and tuning hypotheses. Supplied 2026 driver/track/car data are provisional versioned inputs, not official ratings or calibrated physics. Detailed values and compatibility traits are mapped explicitly; presentation stars do not determine engine coefficients. Qualifying resolution, standing starts, incidents, weather and acquisition/development remain later specs. S07 supplies a preparation freeze seam for the future qualifying-start transition.

Read-only content inventory found 28 supplied driver PNGs and 23 track PNGs matching their JSON slugs. Source content IDs differ from image slugs; runtime mapping must preserve both explicitly. Existing frontend portrait variants cover 22 race drivers and no track variants. Engineer presets, numeric preference compatibility, practice confidence and segmented lap timing are not supplied and require versioned authoring. Track demand and pit-loss ratings are dimensionless, not seconds. Source JSON schema versions are `1.0.0`; driver/track data `2026.10.08-initial-balance`, car specification `2026.10.08-initial-model`.

## Workflow and environment

Each ticket is implemented in an isolated worktree, merged by an independent merger into this integration branch, and verified through the parent-approved public simulation, API/SignalR and small rendered browser boundaries. The dependency chain is 01 → 02 → 03 → 04 → 05 → 06. Independent Standards and Spec reviews follow integration; one implementer owns remediation.

Use pinned Node 24.19.0 at `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe`, .NET SDK 10.0.401 and Java 21.0.12.1 at `.tools/java/jdk-21.0.12.1+1`. Existing locked dependency copies are available in `.tools/s06-dependencies/`; existing Playwright Chromium is in `.tools/playwright`. Preserve unrelated preview/emulator listeners and use isolated .NET artifacts. Commands use `tools/run-bounded.mjs` and immutable records; publish only named reviewed evidence.

Before one broad integration pass, review a complete preparation flow at a 360px phone viewport and a 1920×1080 shared display. Following failures, rerun only affected/remaining named steps. Final records will pin the integrated code SHA, distinct checks, capture assessments, ports and owned process identities.

## Ticket 01

Merged at `75a3239b9cdb520bf7fddbe757e322b7ae548fb1`. [Effective preset contract](../../../docs/effective-weekend-presets.md) records units, active/inactive consumers, authored representative circuit geometry, numerical hypotheses and explicit legacy compatibility. [Evidence](01/effective-weekend-v1.json) retains eight paired comparisons. The focused 26-test set includes five new public simulation checks: effective snapshot replay, version rejection, contrasting circuit effects and subdivision invariance. No frontend or durable transition is claimed by this simulation slice. Narrow escalation was needed for VSTest loopback IPC; NuGet vulnerability audit was unavailable, so focused builds used cached locked packages with `NuGetAudit=false`. No test listeners were started.

## Ticket 02

Merged at `ccb8f3210b7136986b564037597061e2f2a15316`. [Setup contract](../../../docs/weekend-setup.md) records five discrete choices per axis, real bounded compromises, the authored 35-trait compatibility mapping, Adaptability moderation and a distinct warm-up consumer. Base source inputs remain immutable. Opt-in pre-race preparation saves all eleven two-car contexts, setup revisions and accepted command receipts under lobby persistence; it creates no authoritative race session. Private/Open/shared-device access retains authenticated current assignment semantics.

[Focused evidence and reviewed captures](ticket02/README.md): 15 simulation/replay checks, three real Auth/Firestore API checks and two browser paths passed. These overlap older focused sets and are not added as a unique integration count. The phone flow (360×800), tablet handoff (768×1024) and public display (1920×1080) cover retry, refresh, input, imagery/fallbacks and privacy. Dense initial summaries were compacted before final inspection. Owned UI/API4502/4602 and Auth/Firestore19349/19340 services were shut down; immutable records retain command identities/PIDs. Legacy StartLobby accepted retries and default tyre warm-up remain unchanged. Practice, final confirmation, freeze and AI preparation are subsequent tickets.

## Ticket 03

Merged at `7d24b27573fb540624ae2f0bce0f9db420a22b4d`. [Practice evidence](ticket03/README.md) records three public simulation checks, four API checks and one rendered phone flow. Assessment input/output and versions persist before acknowledgement, retries recover the same feedback, and later final-candidate edits preserve the original assessment. Player projections omit seeds and designer diagnostics. The current phone and public display captures were reviewed; no hosted or human-duration claim follows from those checks. Embedded driver/track JSON inputs are now explicitly copied by the Docker build stage; an actual container build remains unverified.

## Ticket 04

Merged at `c44351643e90161caf6344d62a2323b9a1520310`. [Confirmation evidence](ticket04/README.md) records twelve API checks and two rendered flows. Final-ready is recoverable and can be explicitly reopened without new practice. The authoritative application freeze validates all eleven teams before one persisted snapshot of setup, source, base/package and effective inputs. It creates no race or qualifying resolution. Failed writes, ambiguous acknowledgements, restart, private SignalR and post-lock rejection are covered. Phone confirmation was reviewed; the final current public display review belongs to ticket 06.

## Ticket 05

Merged at `2368ab154bbe78692ce8efb6cdf9ed4a7e7a816e`. [AI and assistance evidence](ticket05/README.md) records fourteen affected API checks (including the preceding twelve), two public policy checks and one Guided browser flow. Automatic unclaimed teams record provisional/final observation-driven choices and a single assessment, then become final-ready. Claimed human choices remain intact. Assisted/Guided advice uses a seed-free observation boundary, explains both-driver compromises and uncertainty, and requires explicit application while preserving drafts across help changes. The merge commit's subject incorrectly repeats an earlier setup description; the parent and merged commit above identify the actual AI-assisted change.

## Dependency audit finding

Ticket 04 restore reported NU1903 for the existing `Microsoft.AspNetCore.SignalR.Common` 10.0.12 dependency. [Microsoft's advisory](https://github.com/advisories/GHSA-334f-vvp4-7g6p), published 13 October 2026, lists 10.0.13 as patched and describes denial of service involving stateful reconnect. Local behavioral checks use command-scoped `NuGetAudit=false`; this is an explicit verification limitation, not remediation or a clean dependency audit. No dependency/runtime update or deployed remediation is claimed by S07.
