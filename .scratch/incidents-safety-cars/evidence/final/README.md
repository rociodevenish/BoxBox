# S09 integration completion

Engineering is complete on `codex/incidents-safety-cars`, based on S08 `285277d715c938b656b36c5adb0a99a8970b86d9`. Verified implementation revision: `29206468d7754b4915c9fdc04e29d320830f8b43`; the final commit adds this evidence and tracker links only. All implementer worktrees were merged independently and removed. Only the root checkout remains; final inspection found no listeners on the verification ports.

## Verification

The single initial broad pass at `4af4f43` passed tooling, typecheck and build, then stopped on ten API failures. Simulation passed 171 tests. The [initial immutable manifest](initial-verification.json) retains the failure rather than replacing it with a later success.

One remediation owner fixed both independent Spec findings and all backend failures. Private own-car condition/risk bands and frozen narrow Safety Car assistance now have HTTP/SignalR and rendered coverage. Recovery tests explicitly resolve the local emulator host; lifecycle conflicts precede driver membership validation; ordinary storage-error reads preserve paused state. See [red/green checks, privacy and process evidence](../review-remediation/README.md).

The [affected-check manifest](affected-backend-verification.json), at `82d8a67`, records passing typecheck, build, **171 simulation tests and 146 API tests**. Gallery passed its 34 intended tests but incorrectly discovered four new integration flows without their API fixtures. The same remediation owner added the two missing integration-folder exclusions. No backend or build code changed in that final correction.

The [completion manifest](verification-completion.json), at `2920646`, records all eight affected/remaining stages passing: gallery **34**, browser **22**, boundary **2**, shared **14**, assistance **1**, control **1**, deadlines **1**, hosted **1**. Tooling's **3** checks already passed initially. No second broad invocation was used. Exact launched PIDs, commands, exit codes and elapsed times remain in the manifests and original ignored report directories. [Completion metadata](completion.json) links the revisions, environment and visual review.

## Independent reviews

- [Standards](standards-review.md): no actionable findings. [Remediation recheck](standards-recheck.md): clean.
- [Spec](spec-review.md): two P2 gaps, owned condition estimates and disruption assistance. [Remediation recheck](spec-recheck.md): both resolved, no actionable remaining defect.

Both reviews were read-only and independent. All implementation changes from review and integration failures used one remediation owner.

## Visual and saved-input evidence

Root inspected the complete 360px choices/restart flow, committed-stop and retired states, and the updated [Assisted controller](../review-remediation/s09-review-advice-360.png). Active cars retain usable controls; committed stops cannot be replaced; retired cars and empty approval are disabled; own estimates and advice are readable. Full-page phone images reflect scrolling, not a claim that both cards fit one screen.

Root also inspected the actual [1920×1080 public viewport](../06-public-fit-1920.png): all 22 rows fit, with incident summary, status and banner readable. A browser assertion checks the last row's bottom is at most1080. Earlier full-page desktop captures remain historical evidence; they are not the final viewport-fit claim.

The [904 frozen risk/strategy/mechanism runs](../06-disruption-strategy.md) replay exactly. Ticket01 also replayed all800 saved S08 v1/v2 runs exactly; ticket02 checked representative old records after preserving historical integer-boundary timestamps. Paired arms, two-car fields, distance/eligibility groups, authored route units and uncertain results are disclosed. These are not real-world calibration or human-comprehension measurements.

## Activation and acceptance limits

`Disruptions__Policy=disruption-live-opt-in-v1` explicitly enables the authored Monza/Monaco routes. Unset keeps existing incident-free defaults; other circuits remain off pending authored routes. New inputs snapshot policy, source profiles, route and tuning. Historical opening and staging replay versions retain their original semantics.

Actual household timing, comprehension, shared-device pacing and hosted human acceptance remain pending; the parent and evidence ticket are ready-for-human. No push, PR, deployment or S10 work was performed. Build retains bundle-budget/CommonJS warnings. Command-scoped `NuGetAudit=false` allowed behavioral checks with the already recorded SignalR10.0.12 advisory; this is not security remediation or a clean dependency audit.
