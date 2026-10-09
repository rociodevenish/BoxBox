# Ticket 01 verification workflow

Implemented in `711d748` plus the follow-up evidence/discovery commit. Public seams: bounded command CLI, explicit capture-publication CLI, and existing browser/typecheck/build commands. No UI behavior assertions or race mechanics changed.

- Public runner timeout and child failure tests: initial red (missing command), then both passed. The timeout test runs a real waiting Node process, records its launched PID and requires exit 124 within seven seconds. Child exit 7 remains 7.
- Evidence-publication CLI: passed real filesystem copy and rejection outside `.scratch`; ordinary gallery screenshots now live under ignored `.tools/reports/gallery`.
- `node tools/verify.mjs tooling typecheck build gallery`: tooling, typecheck and build passed. Isolated gallery passed 33 of 34; one old touch test bypassed config with hardcoded port 4400. Changed only that navigation setup to configured baseURL; the affected touch test then passed using the isolated server. Integrated broad verification is recorded once by the parent after both independent tickets merge.
- Build retains pre-existing initial-budget and qrcode CommonJS warnings; no dependency upgrades made.
- `git status --porcelain` after checks showed only intended source additions/edits, with no tracked historical capture changes. All captures/artifacts are ignored. A new explicit publisher copies only selected reviewed files.
- Playwright now refuses existing gallery, API and auth servers, uses configurable isolated UI/API ports, and has portable SDK commands. Fixture-host port follows the same environment setting. Existing preview servers and ports were untouched.
- Verification validates Node engines, places its chosen Node first on child PATH, starts actual demo Auth for backend checks and separately for shared checks, records SHA/PID/timing/status JSON, and stops on failures. Per-command deadline is 15 minutes; timeout cleanup has a five-second bound targeting only the launched tree.
- GitHub CI is configured with locked npm installs, existing global.json SDK, Chromium installation, the same verifier, a 40-minute job deadline and retained ignored report artifacts. CI is configured; remote execution is not claimed by this local Windows check.

See `docs/agents/verification-workflow.md` for concise dispatch, milestone/blocker messages, early representative phone/TV inspection, single broad pass and focused reruns. No protected skill instructions or AGENTS.md were expanded.

Follow-up: fixed Playwright CommonJS config root resolution (`f4169b8`) and repeated only the affected phone case successfully. Wired the workflow reference into implement-spec step 1 and root/frontend READMEs, without expanding AGENTS.md.

Startup diagnostics follow-up: API executable now passes directly to Node spawn instead of JSON encoding a shell command; both server streams are piped into verification output so compiler/startup errors are visible. Real compact-controller browser case passed on isolated 4491/4591 with native backslash DOTNET_EXE. Ordinary API logging is reduced to warnings to keep useful diagnostics concise. The earlier integrated compiler failure is not claimed reproduced by this successful local pass.
