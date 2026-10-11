# S06 integration record

Implementation started 11 October 2026 on `codex/race-continuity-ai-assistance`, from `26447566c568a0214991660cc7e89c35aeef7d60`.

Existing uncommitted roadmap/spec edits belong to the user and are excluded from implementation commits. The S06 title in the working copy identifies this effort as S06.

## Verification environment

- Node: bundled runtime `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe`, v24.19.0. Default shell Node v20.18.0 is unsuitable for the repository checks.
- .NET: SDK 10.0.401, matching `global.json`.
- Java: Microsoft OpenJDK 21.0.12.1 ARM64, unpacked in ignored `.tools/java/jdk-21.0.12.1+1`; process-scoped PATH only. Source: [Microsoft OpenJDK downloads](https://learn.microsoft.com/en-us/java/openjdk/download).
- Firebase CLI: existing locked 15.33.0. Firestore emulator v1.22.0 downloaded through its setup command; bounded download report `.tools/reports/s06-firestore-download.json`.
- Default emulator ports were occupied before S06 verification. Ticket 01 uses isolated owned ports rather than stopping existing services.
- Locked dependency installation uses ignored `.tools/s06-dependencies/` copies of the repository package manifests. An initial root frontend `npm ci` hit an existing preview's locked Lightning CSS native module after removing some packages; missing packages were restored from the isolated locked tree while retaining files in use. Bounded install records stay under `.tools/reports/s06/`.

## Evidence limits

The shared-session prerequisite's automated verification is complete; its actual household playtest remains pending. S06 implementation does not turn automated checks into human findings.

No deployment project has been selected. Hosted join/reconnect, scale-to-zero wakeup, deployment restart, actual family playtest and measured cloud costs remain pending. [Hosting research](../hosting-research.md) records documentation-based constraints and estimates separately from measured acceptance evidence.

Integrated SHA, unique check results, inspected phone/display captures, preview ports and owned process identities will be recorded after integration verification.

## Ticket 01

Merged at `e5a49fc62d8932e944bb83786a9b2919f1789c14`. [Durable recovery evidence](durable-recovery.md) records focused real-emulator checks and the separate snapshot/replay latency sample. Raw bounded command records are preserved in ignored `.tools/reports/s06/ticket01/` before worktree cleanup. Overlapping filtered checks are not added together as unique test counts.

## Ticket 02

Merged at `1102b3f7c7fff1c20ab3790593d39f851bd5dc9a`. [Observation-driven AI evidence](observation-ai.md) records 480 reproducible races, legality, uncertainty and policy hypotheses. Raw focused records are preserved in ignored `.tools/reports/s06/ticket02/`. Reported orphan testhost PID 11428 was no longer running when checked after completion.
