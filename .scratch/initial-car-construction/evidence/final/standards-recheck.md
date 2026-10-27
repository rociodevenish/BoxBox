# S13 Standards recheck

Scope: read-only `git diff 818ad34e362314c1cd015e6bd1af507bc6f39ff6..d97eea712f4ae04f8f5dbd483df5d8cb212a618c`.

The prior P3 possible Duplicated Code finding is resolved: ConstructionPreview now calls `CarConstruction.MechanicalAxis(catalogue,choices)` and removes the redundant selection, handling sum, clamp and rounding implementation. The domain calculation remains the shared source used by previews, confirmation and policy.

No new material documented-standard breaches or baseline smell findings were found in this delta. The personnel copy reuses existing `baseCarReady` state and distinguishes a construction debit from personnel hires. The added browser assertions check observable ledger, balance and paid-car wording. The recovery-fixture helper confines manual wakeups to test hosts while retaining explicit failure-injection and recovery assertions; it does not alter the production scheduler. The remediation README documents diagnostic probes, final checks, owned ports and capture assessments in the verification-workflow format.

Summary: prior finding resolved; 0 remaining Standards findings. No code changes, tests or services were performed by this reviewer.
