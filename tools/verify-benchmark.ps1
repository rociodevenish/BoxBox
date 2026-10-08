param([string]$Dotnet = 'dotnet', [int]$InitialSeed = 7, [int]$Runs = 20, [string]$BaselineRoot)
$ErrorActionPreference = 'Stop'
if ($Runs -lt 20) { throw 'Benchmark publication requires at least 20 seeds' }
if (-not (Get-Command $Dotnet -ErrorAction SilentlyContinue)) { $Dotnet = 'C:/Program Files/dotnet/dotnet.exe' }
$taskRoot = Split-Path $PSScriptRoot -Parent
$revision = 'd22ceedc9548d3cfce6672812a31faf0f6a1bcf6'
if (-not $BaselineRoot) {
    $commonGit = & git -C $taskRoot rev-parse --path-format=absolute --git-common-dir
    if ($LASTEXITCODE -ne 0) { throw 'Repository context required' }
    $BaselineRoot = Join-Path (Split-Path $commonGit -Parent) '.worktrees/segment-benchmark-baseline'
}
if (-not (Test-Path $BaselineRoot)) {
    & git -C $taskRoot worktree add --detach $BaselineRoot $revision
    if ($LASTEXITCODE -ne 0) { throw 'Pinned baseline checkout failed' }
}
$actual = & git -C $BaselineRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $actual -ne $revision) { throw 'Baseline must stay at the pinned revision; no checkout reset is performed' }
if (& git -C $BaselineRoot status --porcelain) { throw 'Baseline tracked checkout must be unchanged' }
$output = Join-Path $taskRoot 'reports/segment-benchmark'
$previousOutput = Join-Path $output 'previous'
& $Dotnet run -c Release --project (Join-Path $PSScriptRoot 'SimulationBenchmarkHarness') "-p:SimulationRoot=$BaselineRoot" -p:PreviousModel=true -- $previousOutput $InitialSeed $Runs
if ($LASTEXITCODE -ne 0) { throw 'Previous executable benchmark failed' }
& $Dotnet run -c Release --project (Join-Path $PSScriptRoot 'SimulationRunner') -- 40 $InitialSeed --suite benchmark --runs $Runs --baseline-benchmark (Join-Path $previousOutput 'previous-benchmark.json') --output $output
if ($LASTEXITCODE -ne 0) { throw 'Current benchmark failed' }
$summary = Get-Content (Join-Path $output 'benchmark-summary.json') -Raw | ConvertFrom-Json
if ($summary.Results.Count -ne 5) { throw 'Expected previous/current aggregate plus three segment counts' }
foreach ($result in $summary.Results) {
    if ($result.Seeds.Count -ne $Runs -or $result.PhaseSamples.Count -ne $Runs * 8 -or $result.MemoryReportSamples.Count -ne 3) { throw 'Benchmark samples missing' }
    if ($result.SegmentCount -gt 1) {
        $zones = @($result.Configuration.Track.Segments | Where-Object PassingAllowed | Select-Object -ExpandProperty PassingZoneId -Unique)
        if ($zones.Count -ne 2) { throw 'Subdivision changed configured passing opportunity identity' }
    }
}
if (& git -C $BaselineRoot status --porcelain) { throw 'Previous source checkout was modified' }
Write-Output 'Actual previous/current benchmark checks passed. These are measurements, not latency threshold assertions.'
