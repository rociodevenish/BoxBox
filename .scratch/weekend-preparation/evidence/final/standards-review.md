Narrow recheck — target 8e1826a0b07a6f28325f1bd5fb276b0199030d09

Both prior duplication heuristics are resolved. PreparationNotifications centralizes controller/team publication, including freeze reuse. Its callers still await the persisted coordinator mutation before publication; only Forbidden suppresses the optional Open projection inside the helper. Outer notification failures retain warning-and-snapshot-recovery behavior.

ValidatePreparationCommand and PreparationReceipt centralize canonical GUID validation, assignment revision checks and receipt matching. Every affected operation still authorizes the current controller before either helper. Setup, practice, confirm and reopen retain their distinct fingerprints, payload validation and lifecycle guards; accepted retries return saved receipts before new-transition checks.

Inspected the pinned fix diff b618faabf583c6aef681a3a000db4323c1b52d8a...8e1826a0b07a6f28325f1bd5fb276b0199030d09. No blocking issue found within this narrow recheck. No tests or services launched.

Original review (historical findings, now resolved above) — 9a54a39db0fa5caa8fada77508da446ce21ad795...b618faabf583c6aef681a3a000db4323c1b52d8a

Documented-standard breaches: none identified in the reviewed preparation implementation. Sources: AGENTS.md; docs/agents/{domain,issue-tracker,triage-labels,verification-workflow}.md; GLOSSARY.md; .docs/f1_strategy_game_{concept,technical_architecture}.md; docs/{effective-weekend-presets,weekend-setup,practice-assessment}.md. No tests or services were launched.

Judgment calls (Fowler heuristic baseline; not hard violations):

- Possible Duplicated Code — src/backend/Game.Api/PreparationEndpoints.cs:18–23, 63–68, 80–85 repeat the same publication sequence: `var team=sessions.HouseholdPreparation(...)`, household group send, optional Open projection/send, and public snapshot send. A privacy/group-selection change must be repeated in three handlers. Extract a preparation notification helper that preserves persistence-before-publication and the explicit Forbidden-only handling for Open projections. Freeze can reuse its per-team portion.
- Possible Duplicated Code — src/backend/Game.Application/SessionCoordinator.Preparation.cs:80–94, 113–126, 156–168 repeat command GUID/revision validation, assignment-revision comparison, fingerprint construction and accepted-receipt recovery: `if(team.Commands.TryGetValue(key,out var accepted))`. Extract only the shared receipt/validation shape, retaining authority checks before receipt lookup and operation-specific fingerprints/lifecycle checks. This reduces the risk of retry rules drifting between setup, practice and readiness.

Diff captured with `git diff 9a54a39db0fa5caa8fada77508da446ce21ad795...b618faabf583c6aef681a3a000db4323c1b52d8a`; nonempty: 140 files, 11,337 insertions, 43 deletions. Commit list captured independently with `git log 9a54a39db0fa5caa8fada77508da446ce21ad795..b618faabf583c6aef681a3a000db4323c1b52d8a --oneline`: b618faa, cdba4e4, 62280a4, d7d4cad, 2368ab1, 6eb9df4, c443516, 102267f, 7d24b27, 9b91106, 9954d6c, 610798e, ccb8f32, f25bbac, 1a6f31f, 0f9b1b8, 5eaf3a9, 75a3239, 21fca09, 947e03f, dd60c4d.
