param(
    [Parameter(Mandatory = $true)]
    [string] $DataDir,

    [Parameter(Mandatory = $true)]
    [string] $TranslationDir,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath,

    [string[]] $Files = @("areas.bytes", "prospects.bytes", "bargains.bytes", "exchanges.bytes")
)

$ErrorActionPreference = "Stop"
$utf8Strict = [Text.UTF8Encoding]::new($false, $true)
$translations = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

function Find-UnescapedEquals([string] $Line) {
    for ($i = 0; $i -lt $Line.Length; $i++) {
        if ($Line[$i] -eq '=' -and ($i -eq 0 -or $Line[$i - 1] -ne '\')) {
            return $i
        }
    }
    return -1
}

function Unescape-TranslationValue([string] $Value) {
    return $Value.Replace("\=", "=").Replace("\/", "/").Replace("\s", " ").Replace("\r", "`r").Replace("\n", "`n").Replace("\t", "`t")
}

function Escape-ReportValue([string] $Value) {
    $trailingSpaces = $Value.Length - $Value.TrimEnd(' ').Length
    $body = if ($trailingSpaces -eq 0) { $Value } else { $Value.Substring(0, $Value.Length - $trailingSpaces) }
    return $body.Replace("\", "\\").Replace('"', '\"').Replace("`r", "\r").Replace("`n", "\n").Replace("`t", "\t") + ("\s" * $trailingSpaces)
}

function Read-SerializedStrings([string] $Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $results = [Collections.Generic.List[object]]::new()

    for ($offset = 0; $offset -lt $bytes.Length; $offset++) {
        if ($offset -gt 0 -and $bytes[$offset - 1] -gt 0x1f) {
            continue
        }

        $length = 0
        $shift = 0
        $contentOffset = $offset
        $validLength = $false
        for ($part = 0; $part -lt 3 -and $contentOffset -lt $bytes.Length; $part++) {
            $value = $bytes[$contentOffset++]
            $length = $length -bor (($value -band 0x7f) -shl $shift)
            if (($value -band 0x80) -eq 0) {
                $validLength = $true
                break
            }
            $shift += 7
        }

        if (-not $validLength -or $length -lt 2 -or $length -gt 8192 -or $contentOffset + $length -gt $bytes.Length) {
            continue
        }
        if ($contentOffset + $length -lt $bytes.Length -and $bytes[$contentOffset + $length] -gt 0x1f) {
            continue
        }

        try {
            $text = $utf8Strict.GetString($bytes, $contentOffset, $length)
        } catch {
            continue
        }

        if ($text -notmatch '[A-Za-z]' -or $text -match '[\uAC00-\uD7A3]') {
            continue
        }
        if ($text -notmatch '\s' -and $text -cnotmatch "^[A-Z][A-Za-z'\u2019\-]{3,}$") {
            continue
        }

        $hasUnsupportedControl = $false
        foreach ($character in $text.ToCharArray()) {
            if ([char]::IsControl($character) -and $character -notin "`r", "`n", "`t") {
                $hasUnsupportedControl = $true
                break
            }
        }
        if ($hasUnsupportedControl) {
            continue
        }

        [void] $results.Add([pscustomobject]@{ Offset = $contentOffset; Text = $text })
    }

    return $results
}

foreach ($path in Get-ChildItem -LiteralPath $TranslationDir -Filter "*.txt" | Sort-Object FullName) {
    if ($path.Name.StartsWith('_')) {
        continue
    }
    foreach ($rawLine in [IO.File]::ReadLines($path.FullName, [Text.Encoding]::UTF8)) {
        $line = $rawLine.TrimStart([char]0xfeff)
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("//")) {
            continue
        }
        $equals = Find-UnescapedEquals $line
        if ($equals -lt 1) {
            continue
        }
        [void] $translations.Add((Unescape-TranslationValue $line.Substring(0, $equals)))
    }
}

$rows = [Collections.Generic.List[object]]::new()
foreach ($fileName in $Files) {
    $path = Join-Path $DataDir $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Data file not found: $path"
    }

    $strings = @(Read-SerializedStrings $path | Where-Object { -not $translations.Contains($_.Text) })
    foreach ($group in $strings | Group-Object Text) {
        $text = $group.Name
        $status = if ($text -match '(?i)(?:\btest(?:ing)?\b|\breuse\b|\bplaceholder\b|\bblah\b|^new (?:prospect|bargain)\b|^Test|another bargain|run of the mill bargareeno|combat arena)') {
            "TEST"
        } elseif ($text -match '^(?:FailbetterJames|Chris Gardiner|Prim Cash|Barry Hemans|ClockworkSun|HouseOfRodsAndChains|NatureReserve|TraitorsWood)$') {
            "INTERNAL"
        } elseif ($text -notmatch '\s') {
            "SINGLE"
        } else {
            "REVIEW"
        }
        $offsets = ($group.Group.Offset | Sort-Object -Unique) -join ','
        [void] $rows.Add([pscustomobject]@{
            File = $fileName
            Status = $status
            Count = $group.Count
            ByteOffsets = $offsets
            Text = Escape-ReportValue $text
        })
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

@("file`tstatus`tcount`tbyteOffsets`ttext") + @($rows |
    Sort-Object File, @{ Expression = { if ($_.Status -eq 'REVIEW') { 0 } else { 1 } } }, Text |
    ForEach-Object { "$($_.File)`t$($_.Status)`t$($_.Count)`t$($_.ByteOffsets)`t$($_.Text)" }) |
    Set-Content -LiteralPath $OutputPath -Encoding UTF8

$rows | Group-Object File, Status | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{ Group = $_.Name; UniqueCandidates = $_.Count; Occurrences = ($_.Group | Measure-Object Count -Sum).Sum }
} | Format-Table -AutoSize
