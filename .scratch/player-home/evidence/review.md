# Independent integration review

Reviewed `git diff ca2529e59789d8d13c30b301a7f294263b30b017...db5db9e01089222ef8174015891a69f7ceecef8b` in independent Standards and Spec agents.

## Standards

No documented-standard violations. One possible Duplicated Code smell: discovery and Return separately implement the same first/current-weekend membership predicate. Resolve through a narrow shared player-resolution helper, retaining Return-specific failure messages.

## Spec

- P2: guest identities appear signed out on home but hide the permanent-account form on the account page. Fix the guest-to-permanent sign-in/registration journey and retain its destination.
- P1: the startup membership audit counts already indexed manifests against a deployment-wide record cap. Replace that cap with safe paged migration that preserves existing recovery records and does not prevent healthy indexed deployments from starting as they grow.

Both behavior findings and the authorization duplication are assigned to one isolated fix implementer. Targeted re-review and affected checks follow its merge.
