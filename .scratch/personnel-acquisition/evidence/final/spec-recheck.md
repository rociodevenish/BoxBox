# S12 Spec remediation recheck

Reviewed `8458f00dd8b43725395f8746811e371e0a100f72...67d14389c97f20f783c55840e5f56d5985aba367` in the remediation worktree. No actionable spec findings; the original clean Spec result stands.

The shared Hire/Scout helper preserves the exact durable command payload and operation-specific storage key. It captures the original team and private-view generation before awaiting, retains ambiguous failures, clears definitive 4xx failures, and updates personnel only while the captured generation remains current. Hire still refreshes public campaign state after acknowledgement. Hidden responses therefore cannot reopen private personnel, while accepted operations retain their original retry semantics.

Scout allocation extraction is behavior preserving: version, bounds, capped allocation formula, later-half cutoff, and single-team exception match the original expressions. Open and saved-state validation use the same extracted rules; all saved content/domain/allocation versions remain unchanged.

The new local delivery guard checks final `IConfiguration` before creating its timer and only returns for explicit `Wakeups:Mode=manual`. Default local automatic behavior and the existing Cloud Tasks implementation remain unchanged. This is consistent with the spec's restart/duplicate delivery requirements by respecting explicit manual scheduler control; it does not remove automatic acquisition deadlines.

Read remediation evidence, including the diagnosed competing background delivery after an ambiguous committed write and the unchanged weather tests passing after correction. Reported related API, browser, domain and frozen-replay checks support the narrow change; this review did not independently execute tests, services, or visual checks. No source edits were made.
