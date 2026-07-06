param(
    [string]$GameDir = ""
)

$ErrorActionPreference = "Stop"

function Resolve-GameDir {
    param([string]$Candidate)

    if ($Candidate -and (Test-Path -LiteralPath (Join-Path $Candidate "Sunless Skies.exe"))) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $cwd = (Get-Location).Path
    if (Test-Path -LiteralPath (Join-Path $cwd "Sunless Skies.exe")) {
        return $cwd
    }

    $default = "C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies"
    if (Test-Path -LiteralPath (Join-Path $default "Sunless Skies.exe")) {
        return $default
    }

    $inputPath = Read-Host "Enter your Sunless Skies game folder"
    if ($inputPath -and (Test-Path -LiteralPath (Join-Path $inputPath "Sunless Skies.exe"))) {
        return (Resolve-Path -LiteralPath $inputPath).Path
    }

    throw "Sunless Skies.exe was not found. Install cancelled."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path $scriptDir "payload"
if (-not (Test-Path -LiteralPath $payload)) {
    throw "Missing payload folder: $payload"
}

$target = Resolve-GameDir $GameDir
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = Join-Path $target "_backup_korean_patch_$stamp"
New-Item -ItemType Directory -Path $backup | Out-Null

$backupItems = @(
    "doorstop_config.ini",
    "winhttp.dll",
    "changelog.txt",
    "BepInEx\config\AutoTranslatorConfig.ini",
    "BepInEx\Translation\ko\Text"
)

foreach ($item in $backupItems) {
    $src = Join-Path $target $item
    if (Test-Path -LiteralPath $src) {
        $dst = Join-Path $backup $item
        $dstParent = Split-Path -Parent $dst
        if (-not (Test-Path -LiteralPath $dstParent)) {
            New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
        }
        Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force
    }
}

Copy-Item -LiteralPath (Join-Path $payload "*") -Destination $target -Recurse -Force

Write-Host ""
Write-Host "Sunless Skies Korean patch installed."
Write-Host "Game folder: $target"
Write-Host "Backup folder: $backup"
Write-Host ""
Write-Host "Launch the game once. The first launch may take longer while BepInEx prepares files."
