param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies",
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$repoDir = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $GameDir "Sunless Skies_Data"
$translationDir = Join-Path $repoDir "payload\BepInEx\Translation\ko\Text"
$payloadDataDir = Join-Path $repoDir "payload\Sunless Skies_Data"
$workDir = Join-Path $repoDir "_tmp_static_patch\batch"
$reportDir = Join-Path $repoDir "docs"
$patcherProject = Join-Path $repoDir "tools\UiAssetPatcher\UiAssetPatcher.csproj"
$installerProject = Join-Path $repoDir "tools\SunlessSkiesKoreanInstaller\SunlessSkiesKoreanInstaller.csproj"
$assets = @("resources.assets", "level4", "level5", "level16", "level24")

if (-not (Test-Path (Join-Path $GameDir "Sunless Skies.exe"))) {
    throw "Sunless Skies game folder not found: $GameDir"
}

New-Item -ItemType Directory -Force -Path $workDir, $reportDir, $payloadDataDir | Out-Null

Write-Host "[1/5] Building patch tools..."
dotnet build $patcherProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "UiAssetPatcher build failed." }
dotnet build $installerProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }

Write-Host "[2/5] Patching and scanning serialized assets..."
$reports = @()
foreach ($asset in $assets) {
    $inputPath = Join-Path $dataDir $asset
    if (-not (Test-Path $inputPath)) {
        Write-Warning "Skipping missing asset: $inputPath"
        continue
    }

    $outputPath = Join-Path $workDir $asset
    $safeName = $asset.Replace('.', '-')
    $reportPath = Join-Path $workDir "$safeName-untranslated.tsv"

    dotnet run --project $patcherProject -c Release --no-build -- `
        $inputPath $outputPath $translationDir $reportPath
    if ($LASTEXITCODE -ne 0) { throw "Asset patch failed: $asset" }

    Copy-Item -LiteralPath $outputPath -Destination (Join-Path $payloadDataDir $asset) -Force
    $reports += [PSCustomObject]@{ Asset = $asset; Path = $reportPath }
}

Write-Host "[3/5] Building review report..."
$developerPattern = '^(?:A label name:?|a+|Body\. It''s the body text|Dr Random|Header|Hello World|Here is where the AvailableAt|Label|label|New Text|One way to do it|TEST|Testing testing|TestTest|Text text|The smoggy, clankingx|This is (?:headerline|placeholder|standard|the body)|Tip description|title|VERSION VERSION|Warning body)'
$ignoredPattern = '^(?:Hallidges prides itslef(?: |\\s)|Naufragiste)$'
$reviewRows = foreach ($report in $reports) {
    if (-not (Test-Path $report.Path)) { continue }
    foreach ($line in Get-Content $report.Path -Encoding UTF8) {
        $parts = $line -split "`t", 2
        if ($parts.Count -ne 2) { continue }
        $status = if ($parts[1] -match $developerPattern) {
            "DEVELOPMENT"
        } elseif ($parts[1] -match $ignoredPattern) {
            "IGNORED"
        } else {
            "REVIEW"
        }
        "$($report.Asset)`t$status`t$($parts[0])`t$($parts[1])"
    }
}

$reviewPath = Join-Path $reportDir "untranslated-ui-review.tsv"
@("asset`tstatus`tpathIds`ttext") + $reviewRows | Set-Content $reviewPath -Encoding UTF8
$reviewCount = @($reviewRows | Where-Object { $_ -match "`tREVIEW`t" }).Count
$developmentCount = @($reviewRows | Where-Object { $_ -match "`tDEVELOPMENT`t" }).Count
$ignoredCount = @($reviewRows | Where-Object { $_ -match "`tIGNORED`t" }).Count

Write-Host "[4/5] Translation dictionary statistics..."
$translationFiles = @(Get-ChildItem $translationDir -Filter *.txt)
$translationLines = ($translationFiles | Get-Content -Encoding UTF8 | Measure-Object -Line).Lines
Write-Host "  Files: $($translationFiles.Count)"
Write-Host "  Lines: $translationLines"
Write-Host "  Review candidates: $reviewCount"
Write-Host "  Development strings: $developmentCount"
Write-Host "  Ignored names or malformed strings: $ignoredCount"
Write-Host "  Report: $reviewPath"

Write-Host "[5/5] Installer verification..."
$installerArgs = @(
    "run", "--project", $installerProject, "-c", "Release", "--no-build", "--",
    "--game-dir", $GameDir, "--translation-dir", $translationDir
)
if (-not $Apply) { $installerArgs += "--dry-run" }
& dotnet @installerArgs
if ($LASTEXITCODE -ne 0) { throw "Installer verification failed." }

if ($Apply) {
    Write-Host "Patch applied. Run this command again without -Apply to verify zero remaining replacements."
} else {
    Write-Host "Audit complete. Use -Apply to install the generated payload after reviewing the report."
}
