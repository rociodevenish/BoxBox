param([string]$Report='browser-red-semantic',[string]$Filter='confirm.spec.ts')
$node='C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
$env:PATH=(Split-Path $node)+';C:/Projects/F1Game/.tools/java/jdk-21.0.12.1+1/bin;'+$env:PATH
$env:DOTNET_EXE='C:/Program Files/dotnet/dotnet.exe'
$env:PLAYWRIGHT_BROWSERS_PATH='C:/Projects/F1Game/.tools/playwright'
$env:XDG_CONFIG_HOME='C:/Projects/F1Game/.tools/firebase-config'
$env:F1_TEST_PORT='4701';$env:F1_API_PORT='4801';$env:F1_CONSTRUCTION_AUTH_PORT='22909';$env:F1_CONSTRUCTION_FIRESTORE_PORT='22900';$env:F1_CONSTRUCTION_EMULATOR_CONFIG='../../tools/auth-emulator/firebase.remediation.local.json'
$env:F1_DOTNET_ARTIFACTS_PATH='C:/Projects/F1Game/.tools/worktrees/s13-review-fixes/.tools/reports/remediation/dotnet-browser'
Set-Location C:/Projects/F1Game/.tools/worktrees/s13-review-fixes/src/frontend
& $node ../../tools/run-bounded.mjs --timeout-ms 540000 --report ("../../.tools/reports/remediation/"+$Report+'.json') -- $node node_modules/@playwright/test/cli.js test --config playwright.construction.config.ts $Filter
