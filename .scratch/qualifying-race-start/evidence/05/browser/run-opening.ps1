$env:PATH='C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin;C:/Projects/F1Game/.tools/java/jdk-21.0.12.1+1/bin;'+$env:PATH
$env:F1_TEST_PORT='4515'
$env:F1_API_PORT='4615'
$env:F1_SHARED_AUTH_PORT='19349'
$env:F1_SHARED_FIRESTORE_PORT='19340'
$env:F1_SHARED_EMULATOR_CONFIG='../../.tools/reports/s08-05-ui/config/firebase.json'
$env:F1_DOTNET_ARTIFACTS_PATH='C:/Projects/F1Game/.worktrees/s08-05/.tools/reports/s08-05-ui/artifacts'
$env:PLAYWRIGHT_BROWSERS_PATH='C:/Projects/F1Game/.tools/playwright'
$env:F1_S08_EVIDENCE='s08-05'
$env:NuGetAudit='false'
node tools/run-bounded.mjs --timeout-ms 480000 --report .tools/reports/s08-05-ui/browser-opening-final.json -- node src/frontend/node_modules/@playwright/test/cli.js test --config src/frontend/playwright.shared.config.ts opening.spec.ts
exit $LASTEXITCODE
