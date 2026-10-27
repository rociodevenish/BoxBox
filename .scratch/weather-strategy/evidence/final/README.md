# S10 integration completion

Engineering is complete on `codex/weather-strategy`, based on completed S09 `51c18a5e8db556372ca4021ad9dfc75dac5d8fd6`. Verified implementation revision: `e1b25da26ad35d6c080f46a8750ba0f9aec9cf2e`. The final completion commit adds evidence and tracker updates only. Every implementation worktree was independently merged and removed; only the root checkout remains.

## Verification

The single initial broad invocation at `a2d7097` passed tooling, typecheck and build, then stopped before backend tests launched because a pre-existing personal-qualifying preview occupied the default emulator ports. The [initial immutable manifest](initial-verification.json) retains that failure. A sandboxed preflight did not reveal those listeners; escalated inspection identified the recorded preview Firebase PID 25928 and Java PID 30036. Neither was stopped or reused.

The remediation added optional backend/deadline emulator configuration and control websocket overrides, retaining existing defaults. [Isolated configuration](verification-emulators.json) used Auth 19539, Firestore 19540, hub 19760, logging 19810 and websocket 9173; control used its normal ports with websocket 9174. UI/API were 4537/4637. The demo project and copied rules were owned by this run; no cloud login or credentials were used.

The [affected/remaining completion manifest](verification-completion.json), at `e1b25da`, records ten named stages passing: tooling **3**, backend **204 simulation + 171 API tests**, gallery **34**, browser **22**, boundary **2**, shared **14**, assistance **1**, control **1**, deadlines **1**, hosted **1**. Typecheck and build passed initially and were unaffected by the backend/test-runner remediation. Thus every one of the twelve verification stages has a passing result; no second broad invocation was used.

Exact commands, launched PIDs, elapsed times, exit codes and revision identities remain in the immutable manifests and original ignored report directories. [Final listener inspection](listeners.json) confirms all owned verification ports closed, while the five existing preview listeners retain their original PIDs. [Completion metadata](completion.json) records the environment, activation and acceptance limits. Worker reports, configurations, failures and the original full archive were copied with SHA256 verification under `.tools/reports/s10/` before worktree cleanup.

## Standards

[Independent Standards review](standards-review.md) found one P2: reaction identity formatting depended on current culture, violating same-input replay determinism. Invariant formatting and a public cross-culture regression resolve it. The [narrow recheck](standards-recheck.md) is clean; no remaining actionable Standards finding.

## Spec

[Independent Spec review](spec-review.md) found one P2: a threshold crossing deferred by minimum separation could disappear after rain flattened or reversed. The scheduler now checks the remembered band's outward edges at the first eligible time before predicting future crossings. Public flat/reversal, boundary, Safety Car deadline and finish cases cover the correction. The [narrow recheck](spec-recheck.md) is clean; no remaining actionable implementation finding.

Both reviews were read-only and independent. One remediation implementer owned both findings and the required isolation overrides. Its [red/green record](../review-remediation/README.md) contains **24 unique focused checks**, including seven new cases, and exact replay of all 560 published weekends after the fixes. The archives were unchanged. Review totals: one original finding per axis, zero remaining after recheck.

## Visual and experimental evidence

Before broad verification, root inspected the complete [Guided 360px phone flow](../05/open-equal-guided-phone-360.png) and actual [1920×1080 mixed-trigger public viewport](../05/open-mixed-public-1920x1080.png). Observed/predicted text, forecast horizons, complete compound names, both-car choices and uncertainty are readable. The public view fits all 22 rows with one cause banner and concise weather/incident events, without private forecasts. The phone is a long scrolling flow, not a claim that both cards and advice fit one screen. Existing browser assertions verify the viewport. Other preparation, starting-tyre and drying captures remain in [ticket05 evidence](../05-weather-advice.md).

The [560 frozen complete 22-car weekends](../06-weekend-evidence.md) cover two tracks, five profiles, eight paired seeds and seven arms, including Q1–Q3, accepted starting tyres, v3 opening and full 10-lap races. Ten archives retain complete inputs/operations, full outcome hashes and comparison outcomes; all 560 replay exactly without catalog reads, both before and after review remediation. Original bulky histories remain ignored. These are paired observations, not 560 independent seeds.

Results retain uncertainty: quality 0/1 did not change decisions or race outcomes under this observation policy; narrower forecast intervals did not universally improve coverage. The ten-lap drying baseline did not cross the Dry reaction threshold. Mechanism tests separately cover falling crossings and hysteresis. No universal forecast benefit, final balance, real meteorological calibration or sporting-rule fidelity is claimed.

## Activation and acceptance limits

The real host opts in with `Weather__Policy=weather-weekend-presets-v1` and `Weather__Profile=all-dry|steady-wet|rain-arrival|drying|changeable` (choose one profile value). Remove both to retain dry creation defaults. See [host activation](../../../../docs/weather-host-activation.md) for configuration validation, actual-circuit mapping and provenance. Weather, wet tyres and reaction capabilities are frozen together; legacy dry and earlier opening/Safety Car replay semantics remain explicit.

Actual household forecast understanding, added decision/presentation time, simultaneous-trigger clarity and hosted human acceptance remain unmeasured. The parent and final evidence ticket are ready-for-human. Automated runtimes and image review do not replace those measurements. No push, PR, deployment or S11 work was performed.

Build retains bundle-budget/CommonJS warnings. Invocation-only `NuGetAudit=false` accommodates the already recorded SignalR10.0.12 advisory; it is not security remediation or a clean dependency audit.
