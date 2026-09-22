param(
    [ValidateSet('fast', 'medium', 'deep', 'diag', 'tune', 'period', 'homo', 'quick')]
    [string]$Group = 'fast'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts\Compile.ps1')

$benchmarkProgram = Join-Path $PSScriptRoot 'work\benchmarks\CrackerBenchmark.exe'
$benchmarkSource = Join-Path $PSScriptRoot 'benchmarks\CrackerBenchmark.cs'
Invoke-CipherCaseCompile -OutputPath $benchmarkProgram -EntryPoint ClassicalCipherToolbox.Benchmarks.CrackerBenchmark -AdditionalSources @($benchmarkSource)

& $benchmarkProgram $Group
if ($LASTEXITCODE -ne 0) { throw "Benchmark failed (exit code $LASTEXITCODE)." }
