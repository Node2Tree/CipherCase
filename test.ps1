$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $projectRoot "src\ClassicalCipherToolbox"
$testRoot = Join-Path $projectRoot "tests"
$workRoot = Join-Path $projectRoot "work\tests"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
$coreSources = Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Core") -Filter "*.cs" |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$cipherSources = Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Ciphers") -Filter "*.cs" |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$analysisSources = Get-ChildItem -LiteralPath (Join-Path $sourceRoot "Analysis") -Filter "*.cs" |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }
$documentationSource = Join-Path $sourceRoot "ToolDocumentation.cs"
$uiSources = @(
    (Join-Path $sourceRoot "CipherForm.cs"),
    (Join-Path $sourceRoot "TextRulesForm.cs"),
    (Join-Path $sourceRoot "HelpForm.cs"),
    (Join-Path $sourceRoot "NativeMethods.cs")
)
$testSource = Join-Path $testRoot "CipherTests.cs"
$testProgram = Join-Path $workRoot "CipherTests.exe"
$ngramResource = Join-Path $sourceRoot "Analysis\english-ngrams.bin.gz"
$fiveGramResource = Join-Path $sourceRoot "Analysis\english-5grams.bin.gz"
$keywordResource = Join-Path $sourceRoot "Analysis\english-keywords.txt.gz"
$telegraphResource = Join-Path $sourceRoot "Analysis\chinese-telegraph.txt.gz"

& $compiler `
    /nologo `
    /target:exe `
    /platform:anycpu `
    /optimize+ `
    /debug- `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "/resource:$ngramResource,ClassicalCipherToolbox.Analysis.EnglishNgrams" `
    "/resource:$fiveGramResource,ClassicalCipherToolbox.Analysis.EnglishFiveGrams" `
    "/resource:$keywordResource,ClassicalCipherToolbox.Analysis.EnglishKeywords" `
    "/resource:$telegraphResource,ClassicalCipherToolbox.Analysis.ChineseTelegraph" `
    /out:$testProgram `
    $coreSources `
    $cipherSources `
    $analysisSources `
    $documentationSource `
    $uiSources `
    $testSource

if ($LASTEXITCODE -ne 0) {
    throw "测试编译失败，退出代码：$LASTEXITCODE"
}

& $testProgram
if ($LASTEXITCODE -ne 0) {
    throw "测试失败，退出代码：$LASTEXITCODE"
}
