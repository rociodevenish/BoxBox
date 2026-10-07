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
$segmentOutput = Join-Path $smokeOutput 'segmented'
$segmentConfig = Join-Path $smokeOutput 'segment-config.json'
Set-Content $segmentConfig '{"laps":7,"track":{"name":"Segments","baseLapTime":85,"passingChance":1,"segments":[{"id":"straight","length":1000,"baselineTime":34,"speedDemand":1,"corneringDemand":0,"passingDifficulty":0},{"id":"corner","length":1500,"baselineTime":51,"speedDemand":0,"corneringDemand":1,"passingDifficulty":0}]},"tyres":{"degradationPerLap":0,"warmupPenalty":0,"pushGain":20},"lapVariation":0.2,"raceFormVariation":0.2}'
$segmentPlan = Join-Path $smokeOutput 'segment-plan.json'
Set-Content $segmentPlan '{"scheduled":[{"phase":0,"driverId":"driver-01","decision":{"pitCompound":"Hard"}},{"phase":0,"driverId":"driver-02","decision":{"pace":"Push"}}],"conditional":[]}'
& $Dotnet run --project $runnerProject --no-build -- --config $segmentConfig --strategy $segmentPlan --output $segmentOutput
if ($LASTEXITCODE -ne 0) { throw 'Segment race command failed' }
$segment = Get-Content (Join-Path $segmentOutput 'race.json') -Raw | ConvertFrom-Json
if ($segment.replay.ModelVersion -ne 'segment-race-v1' -or $segment.metadata.Channels.Count -ne 5 -or $segment.run.Diagnostics.Count -eq 0) {
    throw 'Versioned race metadata or channel diagnostics missing'
}
if (@($segment.run.Result.Events | Where-Object Kind -eq 'SegmentExited').Count -eq 0 -or @($segment.run.Result.Events | Where-Object Kind -eq 'Overtake').Count -eq 0) {
    throw 'Spatial or encounter history missing'
}
$readable = Get-Content (Join-Path $segmentOutput 'race.txt') -Raw
if ($readable -notmatch 'segment straight' -or $readable -notmatch 'encounter driver-' -or $readable -notmatch 'scope driver/race') {
    throw 'Readable spatial, encounter or channel scope diagnostics missing'
}
$replayOutput = Join-Path $segmentOutput 'replayed'
& $Dotnet run --project $runnerProject --no-build -- --replay (Join-Path $segmentOutput 'race-replay.json') --output $replayOutput
if ($LASTEXITCODE -ne 0) { throw 'Race replay command failed' }
if ((Get-FileHash (Join-Path $segmentOutput 'race.json')).Hash -ne (Get-FileHash (Join-Path $replayOutput 'race.json')).Hash) {
    throw 'Race replay changed machine outputs'
}
$comparisonOutput = Join-Path $segmentOutput 'paired'
& $Dotnet run --project $runnerProject --no-build -- --config $segmentConfig --strategy $validPlan --compare $validPlan --runs 2 --output $comparisonOutput
if ($LASTEXITCODE -ne 0) { throw 'Segment comparison command failed' }
$pairedReplayOutput = Join-Path $comparisonOutput 'replayed'
& $Dotnet run --project $runnerProject --no-build -- --replay (Join-Path $comparisonOutput 'comparison-replay.json') --output $pairedReplayOutput
if ($LASTEXITCODE -ne 0) { throw 'Comparison replay command failed' }
if ((Get-FileHash (Join-Path $comparisonOutput 'comparison.json')).Hash -ne (Get-FileHash (Join-Path $pairedReplayOutput 'comparison.json')).Hash) {
    throw 'Paired replay changed machine outputs'
}
foreach ($badKind in @('model', 'randomness', 'seed', 'layout')) {
    $badReplay = Get-Content (Join-Path $segmentOutput 'race-replay.json') -Raw | ConvertFrom-Json
    switch ($badKind) {
        'model' { $badReplay.ModelVersion = 'future-model' }
        'randomness' { $badReplay.RandomnessVersion = 'future-randomness' }
        'seed' { $badReplay.PSObject.Properties.Remove('Seed') }
        'layout' { $badReplay.Configuration.Track.Segments[0].Length = 0 }
    }
    $badFile = Join-Path $smokeOutput "bad-replay-$badKind.json"
    $badReplay | ConvertTo-Json -Depth 100 | Set-Content $badFile
    $rejectedOutput = Join-Path $smokeOutput ([Guid]::NewGuid().ToString())
    & $Dotnet run --project $runnerProject --no-build -- --replay $badFile --output $rejectedOutput
    if ($LASTEXITCODE -eq 0 -or (Test-Path $rejectedOutput)) { throw "Invalid $badKind replay was accepted or wrote reports" }
}
Write-Output 'Segment replay smoke checks passed.'
foreach ($suiteArgs in @(@('--suite', 'track-passing', '--runs', '1'), @('--suite', 'track-passing', '--config', $segmentConfig))) {
    $rejectedOutput = Join-Path $smokeOutput ([Guid]::NewGuid().ToString())
    & $Dotnet run --project $runnerProject --no-build -- @suiteArgs --output $rejectedOutput
    if ($LASTEXITCODE -eq 0 -or (Test-Path $rejectedOutput)) { throw 'Invalid suite request was accepted or wrote reports' }
}
Write-Output 'Segment suite validation smoke checks passed.'
$rejectedOutput = Join-Path $smokeOutput ([Guid]::NewGuid().ToString())
$tyreSuiteError = & $Dotnet run --project $runnerProject --no-build -- --suite tyre-pit --runs 1 --output $rejectedOutput 2>&1
if ($LASTEXITCODE -eq 0 -or (Test-Path $rejectedOutput) -or "$tyreSuiteError" -notmatch 'at least 20 paired seeds') {
    throw 'Tyre suite must enforce principal sample size before writing reports'
}
Write-Output 'Tyre suite validation smoke check passed.'
exit 0
