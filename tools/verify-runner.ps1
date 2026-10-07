param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
if (-not (Get-Command $Dotnet -ErrorAction SilentlyContinue)) {
    $Dotnet = 'C:\Program Files\dotnet\dotnet.exe'
}
$runnerProject = Join-Path $PSScriptRoot 'SimulationRunner/SimulationRunner.csproj'
$smokeOutput = Join-Path $PSScriptRoot '../reports/smoke'
& $Dotnet build $runnerProject --nologo
if ($LASTEXITCODE -ne 0) { throw 'Runner build failed' }
& $Dotnet run --project $runnerProject --no-build -- 7 7 --output $smokeOutput
if ($LASTEXITCODE -ne 0) { throw 'Race command failed' }
$report = Get-Content (Join-Path $smokeOutput 'race.json') -Raw | ConvertFrom-Json
if ($report.run.Phases.Count -ne 2 -or $report.run.Result.Classification.Count -ne 22) {
    throw 'Race report did not contain phases and full field'
}
if (-not (Test-Path (Join-Path $smokeOutput 'race.txt'))) { throw 'Readable race report missing' }
$badScript = Join-Path $smokeOutput 'invalid-script.json'
Set-Content $badScript '[{"driverId":"driver-01","decision":{}}]'
& $Dotnet run --project $runnerProject --no-build -- 7 7 --script $badScript --output $smokeOutput
if ($LASTEXITCODE -eq 0) { throw 'Missing phase was silently accepted' }
Set-Content $badScript '[{"phase":0,"driverId":"driver-01","decision":{"paec":"Push"}}]'
& $Dotnet run --project $runnerProject --no-build -- 7 7 --script $badScript --output $smokeOutput
if ($LASTEXITCODE -eq 0) { throw 'Unknown decision property was silently accepted' }
$validPlan = Join-Path $smokeOutput 'plan.json'
Set-Content $validPlan '{"scheduled":[{"phase":0,"driverId":"driver-01","decision":{"pitCompound":"Hard"}}],"conditional":[]}'
& $Dotnet run --project $runnerProject --no-build -- 7 7 --strategy $validPlan --compare $validPlan --runs 2 --output $smokeOutput
if ($LASTEXITCODE -ne 0) { throw 'Comparison command failed' }
$comparison = Get-Content (Join-Path $smokeOutput 'comparison.json') -Raw | ConvertFrom-Json
if ($comparison.Pairs.Count -ne 2 -or $comparison.EligibleEqualDistanceTimeDifference.Mean -ne 0) {
    throw 'Identical-plan comparison did not preserve paired zero differences'
}
Write-Output 'Runner smoke checks passed.'
