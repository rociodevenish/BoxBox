# S16 storage and policy replay prerequisite

Scope: an implementation prerequisite for #21/#25, based on integration `ca2529e59789d8d13c30b301a7f294263b30b017`. Verified 2026-10-30 UTC / 2026-10-31 Australia/Hobart. This does not claim an accepted full-calendar release campaign; #25 still owns that proof.

## Measured problem and selected interface

The preceding diagnostic (`C:/Users/sbdev/AppData/Local/Temp/s16-storage-growth.md`) projected 23 repeated-circuit event slots with 242 public development-policy observations. Raw samples were 36,676,509–63,856,675 bytes, beyond the existing Firestore adapter's 4,000,000-byte payload bound and chunk-count bound. They were deliberately diagnostic records, not accepted campaign saves. Brotli+Base64 samples were 148,136–1,015,380 bytes. Arbitrarily many extra advice requests mean these samples are not a finite worst case.

The existing `ICampaignStore` interface remains unchanged. `CampaignStore` now losslessly serializes campaigns >= 256 KiB into a `campaign-brotli-json-v1` envelope containing declared raw length, SHA256 and Brotli Base64 data. Smaller campaigns retain their existing raw JSON representation. Historical raw saves remain readable. Decoded typed records still pass the same complete `CampaignSaveIntegrity` checks; no history, quote, outcome, ledger, roster or model validation was removed.

Bounds are explicit: maximum raw serialization/decompression 128MiB (134,217,728 bytes), maximum final stored UTF-8 payload 4,000,000 bytes. The raw bound gives roughly twice the largest measured projection's headroom without making an unbounded season guarantee. Both raw and compressed serialization buffers are bounded; compressed output is bounded before Base64 allocation, and the final complete envelope is checked again. Decompression uses a fixed declaration-bounded destination, requires exact length and complete compressed consumption, and verifies SHA256. Unsupported format, excessive/false declarations, invalid Base64, truncation, trailing data and digest mismatch reject without overwriting bytes.

The Firestore adapter, its 160,000-character chunks, maximum 32 chunks and atomic manifest CAS remain unchanged. Oversize writes fail before calling the underlying store with `SessionFailure.InvalidInput` and explicit permanent-capacity/preserve-save guidance. `CampaignCoordinator.Write` already rethrows `SessionFault`, so this failure is not converted to a generic transient retry. Corrupt/unsupported stored envelopes use the existing actionable incompatible-save conflict. Downgrading to a deployment that lacks this envelope reader is unsupported; preserve the save and use a compatible deployment.

## Bounded pure-policy memoization

Only `DevelopmentDecisionPolicy.Evaluate` results are cached. SHA256 is computed by streaming the complete serialized observation, including all nested quote contracts, own library versions/contents, demands, ratings, budgets, remaining opportunities and incident activation. The FIFO retains at most 1024 digest/recommendation pairs and no observation graphs; there are no identity-only keys, cached exceptions or persisted cache state.

JSON does not represent custom dictionary key/value comparer semantics. Such observations bypass caching and retain the original evaluation behavior; ordinary default/ordinal dictionaries can be memoized. A regression first reproduced a false cache hit between identical serialized entries with case-insensitive versus ordinal lookup, then passed with the eligibility guard. Invalid nonfinite/unserializable inputs fall back to original policy validation and error behavior. The unchanged evaluator still handles old policy versions and rich/Rush alternatives.

All surrounding record-context validation remains outside the cache. Recovery continues to check receipts, sources, actual own roster/library, calendar, ledger balance, outcomes and effective snapshots. Tests prime valid pure results for changed observations and still reject illegitimate saved balance/library context and forged source/recommendation fields.

## Actual consumer and durable evidence

`CampaignStorageGrowthTests` creates an ordinary supported two-event preset campaign through public application operations, completes the first 22-car weekend, opens development, and submits 120 distinct legitimate public recommendation commands. It retains all observations and receipts. This produces 4,198,666 raw UTF-8 bytes. Before the implementation, writing that valid record through `CampaignStore` to the real Firestore emulator failed with the existing 4 MB IOException (red; about40,232ms advice-workload time).

After the implementation, the same public workload stored 21,574 UTF-8 bytes in the final recorded run (command GUIDs/entropy make compressed size vary slightly), with exact typed serialized roundtrip equality. Advice-workload generation took 17,979 ms; the combined API test took 26 seconds. These are local debug observations, not latency guarantees.

The same actual-emulator test verifies:

- stale CAS cannot replace the latest manifest;
- reconstructed API hosts restore all history, and before/after-commit acknowledgement failures retry the same command without duplicate receipts, spending or lost advice (122 choices after two new successful commands);
- warmed-cache forged recommendation/source and illegitimate balance/library observations are rejected without mutating stored payload/version;
- unknown envelope version, declared length 1, declared length above 128 MiB, wrong digest, bad Base64, truncated data and trailing compressed data are rejected through ordinary campaign API reads, retaining exact stored bytes;
- rejected hostile oversized typed persistence requests hit raw and encoded limits before publishing any replacement manifest. These rejection probes are not fabricated accepted campaigns.

Existing `CampaignRecoveryTests` and `DevelopmentGuidanceApiTests` passed alongside it, including old raw source-version recovery, damaged-record preservation, authenticated return/rebind and lost-ack unclaimed-development behavior. No private codec/parser test interface was introduced.

## Policy cost evidence

The public-domain workload uses actual public quotes/package previews and 23 known circuit demands. Before caching, ten identical recovered evaluations allocated 100,032,800 cumulative bytes. After caching, one cold calculation allocated 10,056,272 bytes / 87.44 ms; ten evaluations of an independently deserialized and warmed observation allocated 318,160 bytes total / 5.55 ms. Allocation is cumulative GC allocation, not peak live memory. The test asserts an allocation reduction without asserting wall-clock timing or internal hit counters.

A separate ignored harness replayed the preexisting diagnostic observations through the public policy operation twice, comparing every recommendation with its original saved result. All 242 matched in every pass/scenario:

| Diagnostic observations | Cold replay ms | Warm replay ms | Cold cumulative allocated bytes | Warm cumulative allocated bytes |
| --- | ---: | ---: | ---: | ---: |
| Policy | 7351.01 | 68.54 | 5,218,277,768 | 18,230,048 |
| New-package library stress | 14650.51 | 141.42 | 12,311,154,944 | 31,297,752 |
| One-owned-package refinement stress | 15451.13 | 119.98 | 13,155,622,160 | 33,685,920 |

Projection timings were captured before the final custom-comparer eligibility guard; the final public-domain allocation/semantic checks above include that guard. These projected records were never asserted to pass full campaign integrity or stored as accepted full-calendar saves. Cold startup still performs the full original policy calculation; memoization reduces repeated recovery work, not the first evaluation of novel content. Full-calendar runtime, cold-host latency and deployed memory/cost acceptance remain #25/release evidence.

## Commands and owned processes

All commands used the workspace SDK 10.0.402/runtime 10.0.13 with normal NuGet audit enabled, no suppressions. Explicit worktree: `C:/Projects/F1Game/.tools/worktrees/s16-storage`.

- `dotnet build F1Game.slnx --nologo -m:1 -nr:false`: passed, zero warnings/errors.
- `dotnet test tests/Game.Simulation.Tests/Game.Simulation.Tests.csproj --filter "FullyQualifiedName~DevelopmentPolicyRecoveryCostTests|FullyQualifiedName~DevelopmentGuidanceTests|FullyQualifiedName~RichDevelopmentTests|FullyQualifiedName~RushPolicyTests" --nologo -m:1 -nr:false`: 24 passed before the extra comparer regression. Focused final `FullyQualifiedName~DevelopmentPolicyRecoveryCostTests`: 3 passed, including that regression. No unrelated broad rerun.
- Firebase `emulators:exec --only auth,firestore --project demo-f1game --non-interactive`, with strictly prechecked owned Auth 21109/Firestore 21100/hub 21410/log 21460/websocket 9351. Test filter `FullyQualifiedName~CampaignStorageGrowthTests|FullyQualifiedName~CampaignRecoveryTests|FullyQualifiedName~DevelopmentGuidanceApiTests`: 10 passed in 27.16 seconds. Emulators shut down normally; no existing listener was reused.
- Ignored local runner: `C:/Projects/F1Game/.tools/reports/s16-storage-run.mjs`; owned emulator configuration `.../s16-storage/firebase.json`; bounded process records `.../s16-storage/emulator-command.json` and `test-command.json`. Final recorded PIDs 40600 (Firebase) and 27784 (test command) exited 0. Timeout bounds 660s / 600s and owned-tree cleanup were configured.
- Detailed local logs: `.../s16-storage/api-green-final.log`, `domain-green.log`, `domain-comparer-green.log`, `build.log`, `projection-policy.log`. The diagnostic replay harness is ignored at `.tools/diagnosis/s16-cache` and reads the explicitly labeled outside-repository projection files.
- `git diff --check`: clean. No frontend/browser changes, servers left running, deployment or GitHub mutation.

Integration tip `b327199` (the completed #21 foundation) was merged before this prerequisite commit. The merged solution then built with normal restore/audit, zero warnings/errors; its log is `.tools/reports/s16-storage/build-integrated.log`. The foundation initial pilot is 126,062 bytes, below the raw-format threshold. Owned ports were checked clear after the final emulator run.
