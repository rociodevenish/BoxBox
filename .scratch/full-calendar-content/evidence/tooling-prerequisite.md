# S16 verification prerequisite: patched ASP.NET tooling

Verified 2026-10-30, starting from integration `13ee8c3`, isolated branch `codex/s16-tooling-prerequisite`.

The first normal audited API-test restore failed with NU1903 (warnings are errors): `Microsoft.AspNetCore.SignalR.Common` 10.0.12 has a high-severity vulnerability. This blocked S16 verification before game tests could run.

Microsoft's [CVE-2026-70159 advisory](https://github.com/dotnet/announcements/issues/452), published 2026-10-13 and revised 2026-10-15, identifies SignalR.Common <=10.0.12 as affected, 10.0.13 as patched, and SDK 10.0.402/runtime 10.0.13 as the .NET 10 update.

## Minimal patch

- `global.json`: SDK 10.0.401 -> 10.0.402; existing latestPatch policy retained.
- `Dockerfile`: exact build SDK tag 10.0.401 -> 10.0.402 so its build satisfies global.json. Existing floating ASP.NET runtime tag unchanged.
- API tests: Mvc.Testing and SignalR.Client 10.0.12 -> 10.0.13.
- BrowserFixtureHost: Mvc.Testing 10.0.12 -> 10.0.13.
- CI already installs from global.json; no separate CI pin needed. No framework, game-code, extra-package or broad dependency changes.

## Local official SDK installation

Downloaded `https://dot.net/v1/dotnet-install.ps1`; installer SHA256:
`e8b873e18a81e5c4cd8ab69d84dac8fead291d50b3c44633cd7fddad709a13d6`.

Ran:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File C:/Projects/F1Game/.tools/dotnet-install-s16.ps1 -Version 10.0.402 -InstallDir C:/Projects/F1Game/.tools/dotnet -NoPath -SkipNonVersionedFiles -Verbose -DownloadTimeout 120
```

The official installer downloaded the win-arm64 ZIP from `https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.402/dotnet-sdk-10.0.402-win-arm64.zip`. Its SHA512 matched the SDK402 ZIP hash in [Microsoft release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json):
`d736fefa5c089ed5e1e854e1c07e5686e1e22638d627f6797633b92b60b1b5daddb607650be07c9f178cdbed1a944646c1a5cf3f12fb6aeef7c8cf149c9ba328`.

`dotnet --info` in the patched worktree selected SDK10.0.402, MSBuild18.9.15, host10.0.13, win-arm64. SDK401 and all existing runtime12 directories remain alongside runtime13. No existing versions were deleted; nonversioned host files were preserved by the installer option. An initial direct script invocation gave no output and was interrupted; the explicit pwsh invocation above completed successfully. Sandbox DNS was unavailable; authorized elevated network execution succeeded.

## Red -> green at the actual restore/build seam

Commands run from `.tools/worktrees/s16-tooling`, executable `C:/Projects/F1Game/.tools/dotnet/dotnet.exe`:

1. Before edits: `restore tests/Game.Api.Tests/Game.Api.Tests.csproj --force --nologo` -> exit1, NU1903 SignalR.Common10.0.12. Local log `.tools/reports/s16-tooling-red.log`.
2. After patch/install: `restore F1Game.slnx --force --nologo` -> exit0. Local log `.tools/reports/s16-tooling-green-restore.log`.
3. `restore tests/Game.BrowserFixtureHost/Game.BrowserFixtureHost.csproj --force --nologo` -> exit0 (fixture host is outside solution).
4. `build F1Game.slnx --no-restore --nologo` -> exit0, zero warnings/errors.
5. `build tests/Game.BrowserFixtureHost/Game.BrowserFixtureHost.csproj --no-restore --nologo` -> exit0, zero warnings/errors.
6. `git diff --check` -> clean.

No `NuGetAudit=false`, advisory suppressions or warning downgrades were used. The restored API-test assets resolve SignalR.Common10.0.13 and record enableAudit=true, auditLevel=low, auditMode=all. No NuGet metadata errors occurred in the successful audited runs. No tautological version-string test was added. Full regression, Docker image build and hosted deployment are outside this prerequisite's focused verification; integration owns them.
