Standards remediation recheck: `8458f00dd8b43725395f8746811e371e0a100f72..67d14389c97f20f783c55840e5f56d5985aba367`.

Both prior P3 judgement findings are resolved:

- `src/frontend/src/app/campaign/campaign-page.ts`: `personnelCommand` owns command persistence, exact-payload retries, response-generation guarding and definitive-error cleanup for Hire and Scout. Hire retains its separate campaign refresh. The two duplicate durable-command implementations are removed.
- `src/backend/Game.Domain/PersonnelAcquisition.cs`: Open and Validate both use `ValidScoutAllocation` and `AllocatedScoutTokens`. Allocation bounds/formula remain equivalent and distinct failure messages are preserved.

No new material documented-standard violations or actionable baseline smells identified in the scoped remediation diff. `src/backend/Game.Api/ScheduledWakeups.cs` reads final infrastructure configuration before starting `LocalWakeupDelivery`, so manual mode suppresses the timer. Configuration stays in the API layer, with no infrastructure dependency introduced into domain/simulation code. The correction's narrow purpose is supported by the remediation README's documented concurrent-delivery reproduction and existing public-test results; this recheck did not independently execute those tests.

Remaining Standards findings: zero. Read-only review only; no tests, services or repository edits.
