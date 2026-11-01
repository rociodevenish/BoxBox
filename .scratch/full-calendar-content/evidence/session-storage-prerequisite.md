# Bounded weekend recovery storage prerequisite

Prepared 2026-11-01 against integration b58c55f2a6e15565c62206872bfa8251147d70bb. This fixes an actual persistence blocker before final #25 evidence. It is not a completed release-season acceptance archive.

## Diagnosis

One bounded diagnostic used public CampaignCoordinator/SessionCoordinator operations with current `release-2026-full-circuits-v1`, seed123, six laps, eleven teams / 22 cars, ordinary income/budgets and recorded automatic development. Preparation requires one approved assignment; team 01 was claimed and prepared through the absent-player operation. Ten unclaimed teams made automatic choices and the host explicitly closed the claimed team's missing choice as Save.

The unchanged600-second guard stopped the run after21 of 23 committed weekends (600,403 ms, exit 124). Last checkpoint:210 recorded automatic choices plus21 claimed Save commitments,29 race operations,177 lifecycle actions,30,020,927raw UTF-8 campaign bytes;24,958,680bytes in DevelopmentChoices. This was actual runtime, not a projected calendar. No successful full-season claim.

The first checkpoint showing a durable-bound violation was event 20:4,120,242UTF8bytes for a session write; event 21 maximum4,270,795. CampaignStore already bounds forwarded encoded writes at4,000,000bytes, so the observed larger write necessarily came from SessionCoordinator's raw RecoveryRecord path. FirestoreSessionStore retains its unchanged4 MB limit/chunk/CAS contract. Late in-process campaign reads took2–3.2seconds versus70 ms at event 1; this diagnostic does not isolate host load, cold restore, policy cache effects or cloud cost. The volatile run did not export a final typed record before its deadline and was not repeated.

## Change

`BoundedRecoveryJson` now owns the common bounded serialization, Brotli envelope, SHA256 integrity, base64 and exact decompression checks. CampaignStore retains `campaign-brotli-json-v1`; weekend Capture/Restore and committed campaign result reads use `session-brotli-json-v1`. Small raw legacy records remain readable. Limits remain128 MiB raw,4,000,000 encoded bytes and256 KiB compression threshold. History, policies, package versions, replay operations, receipt data, CAS versions and scheduled wakeups are retained. Oversized writes fail before replacing saved data; malformed/unsupported session envelopes fail with an actionable Conflict.

The existing package-selection corruption adapter was updated at its transport boundary to accept encoded recovery payloads before returning intentionally damaged legacy JSON; its public rejection assertions remain the same. No private parser or codec unit-test seam was introduced.

## Public-boundary proof

A focused late-library fixture uses public PermanentDevelopment Open/Edit/Commit/Close operations for22 paid projects per team. Each team receives100 initial credits and22 ordinary six-credit grants; one 18-credit creation and21 ten-credit immutable refinements leave4 credits. It authors a representative frozen library through public inputs; it is not a real completed campaign. Refinements branch from a still-eligible owned version rather than violating attribute caps.

Public SessionCoordinator preparation, qualifying, standing start and race produce a5,415,499 byte raw RecoveryRecord. The genuine red test failed at the actual Firestore adapter's payload limit. After the codec, the same workload fits a roughly56 KB envelope (56,180 bytes in the final focused run). Fresh coordinator recovery preserves the exact frozen preparation serialization and typed replay, and BrowserReplay verification matches. Large-record before-write/lost-ack retries, duplicate pause operations and stale-CAS rejection preserve the appropriate saved state. Seven malformed/unsupported envelope cases preserve bytes/version on rejected reads; small raw legacy recovery remains readable without rewriting it.

Focused real Auth/Firestore emulator checks:

- Initial affected set:21 passed / 3 failed in20.37 s. The failures exposed a direct raw-deserializer in committed campaign results and an existing corruption fixture that skipped encoded payloads. Both corrected.
- Affected rerun:4 passed in25.16 s (large session, campaign storage growth and two package-selection integrity cases). Campaign growth included120 legitimate advice observations,4,198,925raw campaign bytes / 21,720 encoded bytes plus existing raw/encoded bound, corruption and uncertain acknowledgement assertions.
- The21 earlier greens include durable restart/receipt/CAS/device isolation, manual/automatic deadline and saved assistance provenance cases. The session test overlaps between runs;24 unique focused cases passed after corrections.
- Final normal NuGet-audited build:0 warnings / 0 errors. Initial restricted-network NU1900 preserved; network-enabled restore/build resolved it without a final audit suppression or dependency change.

Bounded manifests/logs are retained under ignored `.tools/reports/s16-session-storage-*`; each emulator invocation owned Auth 22109, Firestore 22100, hub 22410, logging 22460 and websocket 9451 and stopped them afterward. The existing full-calendar creation/restart test passed (1 test, 5.56 s), bringing the focused total to 25 unique passing cases. No broad unrelated suites, GitHub mutations, source calendar changes or repeated season run.

## Remaining acceptance

Final #25 must pin the clean #24 combined pack plus this prerequisite, run all 23 actual weekends durably, export per-event private checkpoints, split campaign/weekend payload maxima from the start and perform independent frozen replay. This fixture does not establish final display assets, authentication behavior, cloud billing or human workload.
