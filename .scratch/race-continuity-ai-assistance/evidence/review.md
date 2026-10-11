# S06 independent review

Base `26447566c568a0214991660cc7e89c35aeef7d60`; reviewed target `5c4d4045d87e8eb5c119d48384cfa405a707201f`. Independent read-only Standards and Spec reviewers inspected the same pinned diff. User roadmap/spec edits were excluded from implementation review.

## Standards

No concrete documented-standard violation was found. Two maintainability judgments:

- **P3, duplicated code:** recovery and live resolution repeat authoritative completion bookkeeping: outcome completion, acknowledgment insertion, clearing pending/deadline fields, progression hook and persistence. Share that synchronous sequence while keeping recovery pause and live publication separate.
- **P3, primitive obsession/repeated switches:** control intents/state use strings for Human/TemporaryAi/PermanentAi and repeat responsibility-source mappings. Use an internal typed mode with compatible saved strings and a single mapping.

These are heuristics, not demonstrated correctness defects. Repo standards override the smell baseline.

## Spec

- **P2, historical assistance provenance:** the spec requires “AI and assistance versions recorded with decisions.” Current advice drops its policy version unless timeout auto-accept is enabled, then gets overwritten at the next window. Human-assisted historical decisions lack the assistance policy/version record. The opponent policy can also be legacy v1 while assistance evaluates v2. Persist phase/team provenance and concrete advice in designer records alongside accepted decisions, outside player payloads.

No additional actionable scope creep or wrong behavior was established. Actual hosting, household feedback and cost measurements remain explicitly pending; documented cold ordinary recovery and trusted scheduled callback behavior differ intentionally.

Summary: Standards 2 heuristic findings (worst P3); Spec 1 functional finding (worst P2). A single implementer owns all fixes and related integration regression adjustments; final remediation evidence follows in `review-fixes.md`.
