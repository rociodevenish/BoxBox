# S11 integration completion

Automated engineering is complete on `codex/campaign-championships`, based on completed S10 `de0340be6060578699d36884fa5ba724e3ba8dba`. Verified implementation revision: `79374076bb00b44cb79fbaeb3c19c6c33cc96670`. The final completion commit adds evidence and tracker updates only. All implementation worktrees were independently merged and removed; only the root checkout remains.

The supported campaign is the frozen authored Monza → Monaco fixture with eleven constructors and twenty-two stable driver identities. It includes durable creation, exactly-once scoring, reports and standings, private between-event preparation, returning-device control, explicit absent-team fallback, and final champions. Full-calendar mode remains gated pending a complete validated content manifest.

## Verification

The single initial broad invocation at `a10fcbc` passed tooling and typecheck, then failed production build: initial bundle 510.02 kB exceeded the unchanged 500 kB error budget. The [initial immutable manifest](initial-verification.json) retains that failure.

One remediation owner addressed both review findings and the build failure. Campaign, preparation and qualifying views now use deferred loading within the existing manual pathname routing. Production initial size is **427.34 kB**, with no budget increase. Scenario-owned enumeration also isolates manual-clock campaign tests without disabling xUnit parallelism or replacing real Firestore.

The [affected/remaining completion manifest](verification-completion.json), at `7937407`, records eleven named stages passing: typecheck, build, backend **209 simulation + 203 API tests**, gallery **34**, browser **22**, boundary **2**, shared **14**, assistance **1**, control **1**, deadlines **1**, hosted **1**. Tooling's **3** checks passed initially and were unaffected. Every one of the twelve verification stages therefore has a passing result; no second broad invocation was used.

Commands, launched PIDs, exit codes, durations and revisions remain in the immutable manifests and original ignored records. Owned UI/API ports were 4547/4647; [emulator configuration](verification-emulators.json) and [rules](firestore.rules) record the isolated local demo environment. [Final listener inspection](listeners.json) found no owned listeners. No unrelated preview was stopped or reused. Worker reports, failed attempts, configurations and captures were preserved with SHA256 verification under `.tools/reports/s11/` before worktree removal. [Completion metadata](completion.json) links the environment, checks and acceptance limits.

## Standards

[Independent Standards review](standards-review.md) found no documented breach and one optional P3 duplication heuristic: write and recovery paths repeated entrant ordering and result-to-scoring conversion. `CampaignScoringInputs` now provides their shared pure mapping. The [narrow recheck](standards-recheck.md) is clean, with no remaining actionable Standards finding.

## Spec

[Independent Spec review](spec-review.md) found one P2: a device registered during qualifying could remain cached as public-only and then miss a later race-boundary reclaim. Registration and explicit return/binding are now separate, including next preparation. Current or pending same-device returns remain idempotent, accepted locks survive, old grants are revoked, and no new private grant appears mid-phase. The [narrow recheck](spec-recheck.md) is clean.

One implementation owner handled all remediation. The [red/green record](../remediation/README.md) includes **20 affected campaign API cases together under normal parallelism**, **5 literal scoring checks**, unchanged published archive replay, production build and one complete browser campaign. Initial totals: Standards one optional P3, Spec one P2. Both resolved; zero remaining findings on either axis.

## Visual and replay evidence

Before broad verification, root inspected the complete campaign flow evidence, actual [360×800 finish viewport](../s11-05-top-viewport-360.png), actual [1920×1080 finish viewport](../s11-05-top-viewport-1920.png), and missing-image circuit/report fallbacks. Season completion and primary Constructors'/secondary Drivers' champions are visible above the fold. Names, points and report navigation remain readable when images fail. Standings and historical reports intentionally scroll; full-page captures are not presented as viewport proof. The focused browser passes after deferred loading preserve the same clear finish views.

The [published campaign archive](../short-campaign.json.gz) is one synthetic, fixed-seed, Competitive two-event history with six laps per event. It freezes calendar, people, cars, conditions and model/scoring versions. Exact replay reexecutes both actual Q1/Q2/Q3 histories, reconstructs each grid and opening, checks every race operation, recreates raw results, rescores awards and rebuilds both championships without mutable catalog reads. The contract rejects altered awards, result hashes, qualifying results and unvalidated calendar expansion, and preserves nonempty output directories. No credentials, cookies or live grants are published. The archive remained unchanged through remediation.

Broader privacy modes, concurrency, retirement/eligibility, tie-break, absence, source compatibility and recovery cases are covered separately by tickets 01–04. This single history is reproducibility evidence, not campaign-balance or household-comprehension evidence.

## Persistence and acceptance limits

[Measured application usage](../05-application-usage.json), around `VolatileSessionStore`, is **60 read attempts**, **37 write attempts**, **14 scheduled-list attempts**, **30,559,713 cumulative attempted serialized write bytes**, **44,521,469 returned read bytes**, and **1,672,659 peak serialized write bytes**. These are whole application payloads. Firestore chunk/RPC counts, billable cloud units and actual hosting costs are unobserved. Real Auth/Firestore durability tests establish correctness separately; no deployment or cost claim is inferred.

Actual separate-day household joining, readiness, reclaim clarity, progression failures and return time remain unmeasured. The parent and final evidence ticket are ready-for-human. Full-calendar content validation also remains gated; acquisition/development is outside this milestone. No push, PR, deployment or S12 work was performed.

Build retains warning-level bundle/CommonJS messages. Invocation-only `NuGetAudit=false` accommodates the already recorded SignalR 10.0.12 advisory; it is not security remediation or a clean dependency audit.
