$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $projectRoot "src\ClassicalCipherToolbox"
$workRoot = Join-Path $projectRoot "work\benchmarks"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
$sources = @()
$sources += Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Core") -Filter "*.cs" | Sort-Object FullName | ForEach-Object { $_.FullName }
$sources += Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Ciphers") -Filter "*.cs" | Sort-Object FullName | ForEach-Object { $_.FullName }
$sources += Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Analysis") -Filter "*.cs" | Sort-Object FullName | ForEach-Object { $_.FullName }
$program = Join-Path $workRoot "CrackerBenchmark.exe"
$ngramResource = Join-Path $sourceRoot "Analysis\english-ngrams.bin.gz"
$keywordResource = Join-Path $sourceRoot "Analysis\english-keywords.txt.gz"
& $compiler /nologo /target:exe /platform:anycpu /optimize+ /debug- /reference:System.dll "/resource:$ngramResource,ClassicalCipherToolbox.Analysis.EnglishNgrams" "/resource:$keywordResource,ClassicalCipherToolbox.Analysis.EnglishKeywords" /out:$program $sources (Join-Path $workRoot "CrackerBenchmark.cs")
if ($LASTEXITCODE -ne 0) { throw "破解基准编译失败" }
& $program $args
if ($LASTEXITCODE -ne 0) { throw "破解基准运行失败" }
