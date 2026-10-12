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
