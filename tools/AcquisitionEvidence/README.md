# Acquisition experiment archive

From the repository root, with the pinned SDK and Node runtime:

```text
dotnet build tools/AcquisitionEvidence/AcquisitionEvidence.csproj
dotnet tools/AcquisitionEvidence/bin/Debug/net10.0/AcquisitionEvidence.dll NEW_EMPTY_DIRECTORY
dotnet tools/AcquisitionEvidence/bin/Debug/net10.0/AcquisitionEvidence.dll --replay NEW_EMPTY_DIRECTORY/acquisition.json.gz
node --test tools/AcquisitionEvidence/evidence.test.mjs
```

`DOTNET_EXE` selects the SDK executable for the Node contract test. Output must be new or empty; existing evidence is never overwritten. This standalone tool does not alter campaign, race or legacy replay formats.

`acquisition-evidence-v1` freezes the complete materialised personnel pool (including driver ratings, preferences, prices and source versions), domain/reserve/policy versions and 48 ordered runs. Each run retains its seed, allocation, initial order, full versioned seven-slot schedule, and every actual offer before a Scout or hire. Trace entries retain command identity/revision, own budget/reserve/tokens, unavailable-Scout explanation, blocked offered choices and post-hire reserve previews. Final per-team records retain assignments, immutable credit ledger, readiness, remaining tokens and category expenditure. Command identities are deterministic invariant-culture hashes of the versioned run identity and revision.

The public `PersonnelAcquisition` boundary governs every action and validates every accepted hire. Policies select only real offered cards with public legal-hire previews. `low-cost` chooses the lowest fixed price; `high-cost-legal` the highest legal fixed price. Both preserve tokens. `mixed-scout` spends at most one available Scout before each hire, with cheapest/highest legal cohorts assigned by stable team identity and rotated by seed modulo three. Ties use ordinal card ID. There is no access to a future offer or an opponent budget in the policy decision.

Replay branches before catalog loading, opens the saved pool, repeats each versioned experiment and compares every schedule, observation, command, assignment, debit and result. It rejects unknown/incomplete archive versions and altered observations/results. This checks deterministic reproducibility; it does not independently prove that the current domain rules are correct. Existing public domain and API tests provide separate worked examples, failure/restart and concurrent-command checks.

`acquisition-summary-v1` is descriptive output derived from those runs: balance distribution, minimum post-hire reserve margin, Scout usage, blocked offered choices (not rejected HTTP commands), balances by initial priority, paid prices by slot/pick position, and paired per-team compensation deltas. The archive is the replay authority; the summary can be regenerated from a fresh run. Prices remain provisional. Credits spent and printed attributes are not measurements of roster strength or market value. Synthetic deterministic policies, eight seeds and heterogeneous cohort confounding cannot establish causal fairness or a final Scout allocation. The no-Scout policies are paired controls: changing token compensation should leave their hires unchanged.
