param([string]$OutputName = "密码箱.exe")
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $projectRoot "src\ClassicalCipherToolbox"
$outputRoot = Join-Path $projectRoot "outputs"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "找不到 Windows C# 编译器。"
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$sources = Get-ChildItem -LiteralPath $sourceRoot -Filter "*.cs" -Recurse |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }

$outputFile = Join-Path $outputRoot $OutputName
$manifestFile = Join-Path $sourceRoot "app.manifest"
$ngramResource = Join-Path $sourceRoot "Analysis\english-ngrams.bin.gz"
$fiveGramResource = Join-Path $sourceRoot "Analysis\english-5grams.bin.gz"
$keywordResource = Join-Path $sourceRoot "Analysis\english-keywords.txt.gz"
$telegraphResource = Join-Path $sourceRoot "Analysis\chinese-telegraph.txt.gz"
$inputCodeResource = Join-Path $sourceRoot "Analysis\chinese-input-codes.txt.gz"
$chineseTableResource = Join-Path $sourceRoot "Analysis\chinese-code-tables.txt.gz"
$chineseIdsResource = Join-Path $sourceRoot "Analysis\chinese-ids.txt.gz"

& $compiler `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    /debug- `
    /win32manifest:$manifestFile `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "/resource:$ngramResource,ClassicalCipherToolbox.Analysis.EnglishNgrams" `
    "/resource:$fiveGramResource,ClassicalCipherToolbox.Analysis.EnglishFiveGrams" `
    "/resource:$keywordResource,ClassicalCipherToolbox.Analysis.EnglishKeywords" `
    "/resource:$telegraphResource,ClassicalCipherToolbox.Analysis.ChineseTelegraph" `
    "/resource:$inputCodeResource,ClassicalCipherToolbox.Analysis.ChineseInputCodes" `
    "/resource:$chineseTableResource,ClassicalCipherToolbox.Analysis.ChineseCodeTables" `
    "/resource:$chineseIdsResource,ClassicalCipherToolbox.Analysis.ChineseIds" `
    /out:$outputFile `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出代码：$LASTEXITCODE"
}

Get-Item -LiteralPath $outputFile
