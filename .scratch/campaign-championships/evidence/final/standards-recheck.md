# S11 Standards recheck

Scope: a10fcbc0241d2fb637ade87a0bb55ceb7c9c7c8a...79374076bb00b44cb79fbaeb3c19c6c33cc96670 only, including the prior P3 finding and remediation evidence. Read-only inspection; no tests or services launched.

## Resolved

The prior optional P3 Duplicated Code finding is resolved. `src/backend/Game.Application/CampaignScoringInputs.cs:7-8` now owns the pure frozen entrant ordering and classification-to-finish mapping. Both write-time scoring (`CampaignCoordinator.Results.cs:31-34`) and recovered contribution validation (`CampaignResultIntegrity.cs:14,23`) call it; standings projection also uses the canonical entrants. Independent integrity checks remain outside the helper.

## Remaining findings

No documented Standards breaches or material new Fowler heuristic smells identified in this narrow diff.

The return fix separates device registration from binding, preserves already-authorized grants and the existing queued boundary on retries, and documents the changed recovery behavior in `docs/campaign-runtime.md`. The added scenario store wraps the real persistence abstraction and restricts manual sweep enumeration without disabling global test parallelism. The frontend deferrals fit the existing pathname routing and preserve the declared bundle budget. Evidence additions occupy a new remediation folder rather than rewriting historical captures.

Recorded build/browser/API outcomes were inspected as evidence only; they were not independently rerun by this reviewer. Root owns independent verification.
