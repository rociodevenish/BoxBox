# Independent integration review

Reviewed `git diff ca2529e59789d8d13c30b301a7f294263b30b017...db5db9e01089222ef8174015891a69f7ceecef8b` in independent Standards and Spec agents.

## Standards

No documented-standard violations. One possible Duplicated Code smell: discovery and Return separately implement the same first/current-weekend membership predicate. Resolve through a narrow shared player-resolution helper, retaining Return-specific failure messages.

## Spec

- P2: guest identities appear signed out on home but hide the permanent-account form on the account page. Fix the guest-to-permanent sign-in/registration journey and retain its destination.
- P1: the startup membership audit counts already indexed manifests against a deployment-wide record cap. Replace that cap with safe paged migration that preserves existing recovery records and does not prevent healthy indexed deployments from starting as they grow.

Both behavior findings and the authorization duplication are assigned to one isolated fix implementer. Targeted re-review and affected checks follow its merge.

## Resolution

Fixes integrated at `6c852fa387571f90290afc98584cf75a8967de1d`; see [review-fixes](review-fixes.md) for observed red and passing focused API 4/4 and browser 5/5 checks.

Independent targeted re-review of `db5db9e...6c852fa` confirmed the guest account destinations, paged metadata-only migration and shared authorization resolver. Standards: zero outstanding findings. Spec: zero outstanding findings. Neither reviewer reran tests; execution evidence comes from implementer and integrated checks.

## Full-suite integration follow-up

The first configured full API run passed 242/247 cases and exposed eager storage initialization on public production health/auth startup plus interference from the new manual-clock test's unscoped durable store. [Final integration fixes](final-integration-fixes.md) records the exact production red and the corrected concurrent 34/34 related-class run.

Both reviewers independently checked `4162833...2b4cad0`: migration remains inside the store factory before any campaign data/discovery access, while public health/auth no longer needs storage credentials. The manual-clock test retains real persistence and membership checks through the existing scenario scope. Standards and Spec each report zero outstanding findings. The final full API run is recorded separately in completion evidence.
