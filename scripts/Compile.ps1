# Shared source discovery, compiler options and embedded data for every entry point.
$cipherCaseRoot = Split-Path -Parent $PSScriptRoot

function Invoke-CipherCaseCompile {
    param(
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [Parameter(Mandatory = $true)][string]$EntryPoint,
        [ValidateSet('exe', 'winexe')][string]$Target = 'exe',
        [string[]]$AdditionalSources = @()
    )

    $compiler = @(
        "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $compiler) { throw 'Windows .NET Framework 4 C# compiler was not found.' }

    $sourceRoot = Join-Path $cipherCaseRoot 'src\ClassicalCipherToolbox'
    $sources = @(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -Recurse |
        Sort-Object FullName | ForEach-Object { $_.FullName })
    $sources += $AdditionalSources
    foreach ($source in $sources) {
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source: $source" }
    }

    $compilerArgs = @(
        '/nologo', "/target:$Target", '/platform:anycpu', '/optimize+', '/debug-',
        '/reference:System.dll', '/reference:System.Drawing.dll', '/reference:System.Numerics.dll',
        '/reference:System.Windows.Forms.dll', "/main:$EntryPoint"
    )
    if ($Target -eq 'winexe') {
        $compilerArgs += '/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')
    }

    $resources = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'Resources.psd1')
    foreach ($name in ($resources.Keys | Sort-Object)) {
        $resourcePath = Join-Path $sourceRoot $resources[$name]
        if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) {
            throw "Missing embedded resource: $resourcePath"
        }
        $compilerArgs += "/resource:$resourcePath,ClassicalCipherToolbox.Analysis.$name"
    }

    $absoluteOutput = [IO.Path]::GetFullPath($OutputPath)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $absoluteOutput) | Out-Null
    & $compiler @compilerArgs "/out:$absoluteOutput" @sources
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed (exit code $LASTEXITCODE)." }
}
