param(
    [Parameter(Mandatory = $true)]
    [string] $OriginalDataDir,

    [Parameter(Mandatory = $true)]
    [string] $XdeltaPath,

    [string] $BsdiffPath,
    [string] $BspatchPath,
    [string] $OutputDir = (Join-Path $PSScriptRoot "delta-results")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot)
$patchedDataDir = Join-Path $repoRoot "payload\Sunless Skies_Data"
$assetListPath = Join-Path $repoRoot "payload\release-static-assets.txt"
$runBsdiff = $BsdiffPath -and $BspatchPath

if (-not (Test-Path -LiteralPath $XdeltaPath -PathType Leaf)) {
    throw "xdelta3 executable not found: $XdeltaPath"
}

if ($runBsdiff -and
    (-not (Test-Path -LiteralPath $BsdiffPath -PathType Leaf) -or
     -not (Test-Path -LiteralPath $BspatchPath -PathType Leaf))) {
    throw "Both bsdiff and bspatch executables are required."
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$rows = @()

foreach ($line in Get-Content -LiteralPath $assetListPath) {
    $relativePath = $line.Trim()
    if (-not $relativePath -or $relativePath.StartsWith("#")) {
        continue
    }

    $sourcePath = Join-Path $OriginalDataDir $relativePath
    $targetPath = Join-Path $patchedDataDir $relativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Original file not found: $relativePath"
    }
    if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
        throw "Patched file not found: $relativePath"
    }

    $safeName = $relativePath.Replace("/", "_").Replace("\", "_")
    $xdeltaFile = Join-Path $OutputDir "$safeName.vcdiff"
    $xdeltaOutput = Join-Path $OutputDir "$safeName.xdelta.out"

    & $XdeltaPath -f -9 -e -s $sourcePath $targetPath $xdeltaFile
    if ($LASTEXITCODE -ne 0) { throw "xdelta encode failed: $relativePath" }
    & $XdeltaPath -f -d -s $sourcePath $xdeltaFile $xdeltaOutput
    if ($LASTEXITCODE -ne 0) { throw "xdelta decode failed: $relativePath" }

    $targetHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $targetPath).Hash
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $xdeltaOutput).Hash -ne $targetHash) {
        throw "xdelta result hash mismatch: $relativePath"
    }
    Remove-Item -LiteralPath $xdeltaOutput -Force

    $bsdiffBytes = $null
    if ($runBsdiff) {
        $bsdiffFile = Join-Path $OutputDir "$safeName.bsdiff"
        $bsdiffOutput = Join-Path $OutputDir "$safeName.bsdiff.out"
        & $BsdiffPath $sourcePath $targetPath $bsdiffFile
        if ($LASTEXITCODE -ne 0) { throw "bsdiff encode failed: $relativePath" }
        & $BspatchPath $sourcePath $bsdiffOutput $bsdiffFile
        if ($LASTEXITCODE -ne 0) { throw "bsdiff decode failed: $relativePath" }
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $bsdiffOutput).Hash -ne $targetHash) {
            throw "bsdiff result hash mismatch: $relativePath"
        }
        Remove-Item -LiteralPath $bsdiffOutput -Force
        $bsdiffBytes = (Get-Item -LiteralPath $bsdiffFile).Length
    }

    $rows += [pscustomobject]@{
        Path = $relativePath
        DeltaPath = [IO.Path]::GetFileName($xdeltaFile)
        PatchedBytes = (Get-Item -LiteralPath $targetPath).Length
        XdeltaBytes = (Get-Item -LiteralPath $xdeltaFile).Length
        BsdiffBytes = $bsdiffBytes
        SourceSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash.ToLowerInvariant()
        TargetSha256 = $targetHash.ToLowerInvariant()
    }
}

$rows | Export-Csv -LiteralPath (Join-Path $OutputDir "results.csv") -NoTypeInformation -Encoding UTF8
$manifest = [ordered]@{
    formatVersion = 1
    tool = [ordered]@{ name = "xdelta3"; version = "3.2.0" }
    files = @($rows | ForEach-Object {
        [ordered]@{
            path = $_.Path.Replace("\", "/")
            deltaPath = $_.DeltaPath
            sourceSha256 = $_.SourceSha256
            targetSha256 = $_.TargetSha256
            targetSize = $_.PatchedBytes
        }
    })
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDir "manifest.json") -Encoding UTF8
$rows | Format-Table Path, PatchedBytes, XdeltaBytes, BsdiffBytes -AutoSize
[pscustomobject]@{
    PatchedBytes = ($rows | Measure-Object PatchedBytes -Sum).Sum
    XdeltaBytes = ($rows | Measure-Object XdeltaBytes -Sum).Sum
    BsdiffBytes = ($rows | Measure-Object BsdiffBytes -Sum).Sum
} | Format-List
