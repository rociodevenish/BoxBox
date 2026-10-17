# S08 final local verification

Local implementation is complete on `codex/qualifying-race-start`, based on committed S07 `cd7c61f248cdc1e32f38beda5bd4b31dc738cc1a`. Integrated source and remediation are pinned at `2df420f5ba4e486395f3fe88b77c9ed968079dcc`; the completion commit adds tracker and verification records only. All five engineering tickets are resolved. Actual household and hosted acceptance remain pending.

## Result

Preparation locks atomically into durable Q1, followed by Q2/Q3 and a unique 22-car grid. Session-start observation-only AI/timeout choices and per-player assistance retain private human locks. The automatic opening advances the existing engine to the leader's lap-one crossing with trailing spatial progress, tyre state and recorded timing; the first normal decision window starts after presentation, and its phase ends at leader lap six. Reconnect, duplicate callbacks, pause, uncertain acknowledgements and restart preserve saved outcomes. No player launch decision, incident, weather or acquisition system was added.

The game format retains sixteen in Q1 and ten in Q2. These are versioned game rules. Presets, representative circuit geometry and numerical coefficients remain provisional authored hypotheses.

## Verification records

- [Initial broad run](initial-verification.json), code `dc2d9d2`: tooling, typecheck, production build, backend and gallery passed. Backend results were 137 simulation tests and 129 API tests; gallery had 34 passing tests. The browser stage exposed legacy controller labels changing from `Driver 1` to `Driver 01`, causing failures and an aggregate timeout (exit 124). That failed record and its artifacts were preserved.
- [Post-remediation completion](verification-completion.json), code `2df420f5`: all nine selected affected/remaining steps passed: typecheck, build, browser, boundary, shared, assistance, control, deadlines and hosted. Results include 22 browser, two boundary, fourteen shared-device, one assistance, one control, one deadline and one direct-origin test. The hosted-named step checks local cross-origin configuration/REST/SignalR, not a cloud deployment.
- [Focused remediation](../review-remediation/README.md): 62 public simulation checks, thirteen actual Auth/Firestore API checks and two affected browser flows passed, including actual pace/time/wear effects, v1 durable recovery and replay. These overlap prior checks and are not added as independent totals. The ordinary rolling constructor remains unchanged.

All twelve named verification stages ultimately passed across the retained broad and targeted completion records. There was one broad invocation; fixes were followed by affected/remaining checks. [Completion manifest](completion.json) records the baseline, code SHA, run IDs, overlapping counts, versions and limits. Original ignored run manifests remain unchanged under `.tools/reports/verification/runs/`; committed copies retain their original command/PID/path/timestamp data.

Pinned Node 24.19.0, .NET 10.0.401, Java 21 and cached locked dependencies/Chromium were used with isolated artifacts. Existing initial-bundle budget and qrcode/dijkstrajs CommonJS warnings remain. `NuGetAudit=false` was scoped to behavioral checks because the existing SignalR 10.0.12 dependency reports [Microsoft's advisory](https://github.com/advisories/GHSA-334f-vvp4-7g6p). This is a documented limitation, not dependency remediation or a clean security audit.

## Standards

The [independent Standards report](standards-review.md) found zero documented-standard breaches and one P3 Possible Duplicated Code heuristic: repeated expiry fallback and settlement. One implementer extracted the shared settlement operation while retaining caller revision/pause guards, one resolution and persistence before publication. The independent recheck at `2df420f5` confirmed it resolved with no blocking issue. Counts: one heuristic resolved, zero outstanding.

## Spec

The [independent Spec report](spec-review.md) found one P2: automatic opening inherited enum-default Conserve instead of intended Standard. New `automatic-standing-start-v2` initializes Standard/Neutral before derived trajectory, tyre penalty and duration calculations. Explicit `automatic-standing-start-v1` preserves its original Conserve semantics. Recovered public projections report the saved version. One implementer also restored frontend legacy controller-name formatting while retaining supplied names for opening sessions. The independent recheck confirmed the P2 resolved with no silent version reinterpretation or new blocking issue. Counts: one P2 resolved, zero outstanding. Review axes remain separate.

## Replay and measured evidence

The [current v2 evidence](../review-remediation/README.md) contains 400 complete frozen 22-car weekends and saved-file replay results; all 400 historical v1 records also replay exactly. Cases are 384 risk comparisons (32 shared published seeds, three approaches, two circuits, equal/mixed fields) plus sixteen setup controls using the first four seeds. These are correlated paired cases, not 400 independent seeds. Inputs retain source/model/randomness/rules/setup/field/grid/plan versions, all attempts, opening and first race-phase outcomes without reopening mutable catalog content.

Qualifying/grids/risk summaries remain unchanged by the opening correction. All-Out has higher sampled no-time exposure and loses some jointly valid pairs to Safe. Neutral circuit contrast is inconclusive; nonneutral Aero controls differ between circuits without a sign reversal. These limitations and conditional time distributions are retained rather than claiming a universal optimum or calibrated risk. Opening movement occurs in 282/400 current cases, with maximum seven positions gained. Co-located failed-pass queues can produce zero recorded intervals in the current traffic model; unavailable measurements remain labelled unavailable. Image/API agreement alone is not independent timing proof.

## Reviewed presentation and cleanup

Before the broad pass, root inspected the complete 360px qualifying choices and first-window phone flow plus the current 1920×1080 public first window and opening. Root inspected the [post-remediation phone](../review-remediation/phone-first-window.png), [public opening](../review-remediation/public-opening.png) and [1920×1080 public first window](../review-remediation/public-first-window.png) again: readable controls and supplied identities/fallbacks, legal opening movement, both cars' independent choices and all 22 public rows within the display. Tablet handoff, eliminated teammate, accepted-lock refresh, Guided draft preservation, reduced motion/skip, portraits and track fallbacks are recorded in the focused browser evidence. Vertical phone scrolling is expected; no horizontal document overflow or overlap was observed. Historical screenshots/data remain unchanged and explicitly labelled v1.

Every implementation/remediation worktree was merged and removed by the independent merger after ignored reports/configuration were copied and SHA256-verified. Preservation manifests remain under `.tools/reports/s08/ticket01` through `ticket05` and `review-remediation`. Shared dependency targets remain intact. Final listener inspection found no services on owned root/fix UI/API4517/4617/4518/4618, recovery/control Auth/Firestore19109/19100/19309/19300, worker Auth/Firestore19349/19340/19359/19350/19369/19360, related hubs/logging or websockets9150/9158/9159/9160. Unrelated previews were preserved.

## External acceptance

The spec is ready for human acceptance. Still unobserved: household qualifying's four-to-five-minute target, separate decision/presentation/handoff duration, two-car comprehension, start readability, full-weekend pacing and actual hosted deployment/restart/recovery/cost. Automated durations and agent image inspection do not establish those results. The existing dependency advisory remains unremediated. No later spec was started.
