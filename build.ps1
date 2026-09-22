param(
    [string]$OutputName = "密码箱.exe",
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'outputs')
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts\Compile.ps1')

$outputFile = Join-Path $OutputDirectory $OutputName
Invoke-CipherCaseCompile -OutputPath $outputFile -Target winexe -EntryPoint ClassicalCipherToolbox.Program
Get-Item -LiteralPath $outputFile
