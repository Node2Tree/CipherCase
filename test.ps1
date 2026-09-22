$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts\Compile.ps1')

$testProgram = Join-Path $PSScriptRoot 'work\tests\CipherTests.exe'
$testSources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'tests') -Filter '*.cs' -Recurse |
    Sort-Object FullName | ForEach-Object { $_.FullName })
Invoke-CipherCaseCompile -OutputPath $testProgram -EntryPoint ClassicalCipherToolbox.Tests.CipherTests -AdditionalSources $testSources

# Fixture paths in the existing tests are relative to the repository root.
Push-Location $PSScriptRoot
try {
    & $testProgram
    if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit code $LASTEXITCODE)." }
}
finally { Pop-Location }
