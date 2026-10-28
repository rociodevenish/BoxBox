# 01: Verification and agent workflow

> GitHub issue: [#109](https://github.com/rociodevenish/BoxBox/issues/109) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

Status: resolved

Implement the workflow requirements in [the spec](../spec.md): bounded process/command tooling, isolated test servers and ignored capture output, explicit evidence publishing, reproducible verification plus CI, concise orchestration and early visual-check guidance. Reuse existing commands. Do not start shared-session ticket 02 or alter race mechanics. Verify timeout behavior, ignored capture destinations and a representative verification invocation.

## Answer

Implemented bounded portable verification, isolated servers and ignored captures, explicit evidence publication, CI and concise workflow guidance. See [verification evidence](../evidence/01-workflow.md).
