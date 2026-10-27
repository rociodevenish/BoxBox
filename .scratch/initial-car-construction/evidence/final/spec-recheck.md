# S13 Spec recheck

Read-only review of `818ad34e362314c1cd015e6bd1af507bc6f39ff6..d97eea712f4ae04f8f5dbd483df5d8cb212a618c` in the remediation worktree. No tests or services run.

**Prior P3 resolved.** `src/frontend/src/app/campaign/campaign-page.html:60` now shows “Your Base Car is already paid” for a confirmed team's completed roster; line 62 labels `base-car-construction` as “Car construction.” The new public browser assertions verify one 18-credit car debit, seven personnel debits, zero remaining credits, and absence of the protected-reserve sentence after purchase (`src/frontend/tests/construction/confirm.spec.ts:46–54`). This addresses both parts of the prior finding and the spec's cost/future-flexibility requirement.

No new semantic spec defects found in this narrow delta. Preview now calls the same public handling-axis operation used at confirmation; its selection, projection, clamp and rounding match the removed duplicate. Construction mapping and versions remain unchanged. The recovery fixture selects manual wakeups and retains explicit failure-observation, restart and ledger assertions; no production scheduler behavior changed. Published evidence distinguishes the original intermittent failure, diagnostic probe, manual-wakeup probe and final focused passes, without claiming the failed broad run passed.

Human acceptance remains correctly outstanding: ticket 04 is still **ready-for-human**, with actual choice comprehension **UNMEASURED**. The remediation README continues that distinction. Engineering browser assertions/captures and unchanged 1,300-race replay evidence do not claim to complete a human comprehension study or prove balance.

New findings: 0. Remaining prior actionable findings: 0.
