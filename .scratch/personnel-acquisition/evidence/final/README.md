# S12 integration completion record

Integration branch: `codex/personnel-acquisition`. Baseline: completed S11 `54c36ddb126f166a4b03aab544d93d023c33c10b`. Integrated implementation including remediation: `07db190b4dba13144b4a935d097682c564b843e6`.

All five engineering tickets are integrated. Their Answers and [map](../../map.md) link the public-boundary evidence. Acquisition is an explicit campaign creation option; existing preset campaigns retain their route.

## Implemented behavior

Campaigns freeze a versioned unique personnel pool, printed prices, saved rotating schedule, offers, Scout draws and token allocation. Each accepted hire commits ownership, one mandatory roster place, its immutable integer-credit debit and command receipt together. Authenticated team projections keep offers, budgets, advice and ledger private. Refresh and ambiguous acknowledgements reuse saved commands and offers.

The conservative global-demand reserve protects remaining mandatory personnel and the minimum initial car option. Its explicit provisional mapping is 18 construction budget units to 18 credits. No car construction or development effects are delivered here. Additional distinct fictional base-price staff provide sufficient affordable supply without changing frozen prices, duplicating people or granting credits.

All seven acquired people are retained per team. Driver identities and supported detailed attributes replace presets across every frozen event and the existing first lobby while constructor and entry identities remain stable. The recoverable handoff keeps preparation blocked until lobby persistence succeeds. Each driver's Race Engineer Setup affects legitimate Practice Assessment confidence. Other unsupported engineer attributes and future Manager/Technical Engineer effects are visibly inactive.

Offer-only advice and automatic choices use actual legal offers and own budget/reserve, with recorded policy versions and observations. After explicit host activation, persisted deadlines give assigned teams 120 seconds and unclaimed teams one second. Callback/sweep retries preserve accepted purchases. The local delivery worker now honors final effective manual configuration before starting, resolving a demonstrated race between automatic delivery and explicit recovery sweeps.

## Independent reviews

### Standards

[Original review](standards-review.md): no material documented-standard breaches; two P3 judgement findings for duplicate durable-command handling and Scout allocation logic. Both were extracted by one remediation implementer. [Narrow recheck](standards-recheck.md): both resolved, no new material findings.

### Spec

[Original review](spec-review.md): no actionable discrepancies. [Narrow recheck](spec-recheck.md): no actionable discrepancies in privacy/retry handling, frozen allocation behavior or manual delivery configuration.

Final findings: Standards zero unresolved; Spec zero unresolved.

## Verification

Visual review preceded the one full verifier invocation. The complete 360px comparison/reserve/ready flow and actual 1920×1080 public top were inspected independently; final remediation captures were also reviewed. See [browser evidence](../05-browser.md) and [remediation evidence](../remediation/README.md). Tall phone captures are full pages from a 360×800 viewport, not claims that every card fits above the fold.

[Initial manifest](initial-verification.json) records tooling, typecheck and build passes on `8458f00`. Backend passed 227 simulation tests, exposed the weather committed-write wakeup race, then reached its 600-second bound without a complete API result. It is retained as a failed run. Only affected and previously unrun named stages were rerun after remediation; [the successful manifest](verification-completion.json) pins implementation `07db190`.

All twelve stages have passing records: tooling 3; typecheck; production build; backend 227 simulation and 216 API tests; gallery 34; browser 22; boundary 2; shared 14; assistance 1; control 1; deadlines 1; hosted 1. The production initial bundle is 429.38 kB within the unchanged 500 kB error budget. Existing warning-budget/CommonJS warnings remain. Focused acquisition checks and replay contracts are additional evidence, not added again to the integrated test totals. No second whole verifier invocation was used.

Focused remediation already passes 14 personnel domain cases, exact replay of the frozen 48-run archive, the unchanged weather recovery cases, six related API cases, typecheck and both acquisition browser flows. Corrected temporary emulator paths, cold-start waits and artifact isolation are documented separately from product failures. No test assertions or production budgets were weakened.

Runtime: Node 24.22.1, .NET SDK 10.0.401 selected by global.json, Java 21.0.12.1, locked dependencies and cached Chromium. Verification uses owned isolated services and the retained [emulator configuration](verification-emulators.json) and [rules](firestore.rules). Command-scoped `NuGetAudit=false` addresses existing unavailable audit access/advisory conditions; dependency pins are unchanged and this is not a clean security-audit claim.

## Evidence limits and cleanup

[Experiments](../05-acquisition-evidence.md) retain 48 replayable runs, 528 completed team rosters and 3,696 unique hires with at least 18 credits remaining. The compressed archive SHA256 is `DAD8BDFE5385D6A514A329118CFEBEC84E916D13CA185319A651C43DDF0FB564`. Eight seeds, three legal policies and paired compensation settings demonstrate completion, not balanced personnel value. Scout compensation, prices, ratings and priority fairness remain tuning hypotheses; campaign/economy performance and real household understanding were not measured.

All worker reports, configuration and required ignored evidence were preserved with verified SHA256 manifests under `.tools/reports/s12/`. Merged worker checkouts were removed after unlinking dependency junctions without recursing into shared caches. The final [listener inspection](listeners.json) found no task-owned listeners on the checked ports. Only the root integration worktree remains. [Completion manifest](completion.json) collects source revisions, unique results and capture hashes. No push, PR, deployment or S13 work is part of this milestone.
