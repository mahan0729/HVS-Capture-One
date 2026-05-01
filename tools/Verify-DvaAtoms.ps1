<#
.SYNOPSIS
    Verifies that a processed MP4 file contains all required DVA uuid atoms.

.DESCRIPTION
    Runs ExifTool against the output file and checks that every atom in the
    DVA Atom Canon v1 is present and non-empty. Reports PASS / FAIL per atom
    and an overall result at the end.

.PARAMETER FilePath
    Full path to the processed output MP4 to verify.

.PARAMETER ExifToolPath
    Full path to exiftool.exe. Defaults to C:\Tools\exiftool\exiftool.exe.

.EXAMPLE
    .\Verify-DvaAtoms.ps1 -FilePath "D:\Output\Smith_John_01.mp4"
#>

param(
    [Parameter(Mandatory)]
    [string] $FilePath,

    [string] $ExifToolPath = "C:\Tools\exiftool\exiftool.exe"
)

# Preflight

if (-not (Test-Path $FilePath)) {
    Write-Error "File not found: $FilePath"
    exit 1
}

if (-not (Test-Path $ExifToolPath)) {
    Write-Error "ExifTool not found at: $ExifToolPath"
    exit 1
}

Write-Host ""
Write-Host "HVS Capture One - DVA Atom Verifier" -ForegroundColor Cyan
Write-Host "File : $FilePath" -ForegroundColor Gray
Write-Host ("-" * 60) -ForegroundColor DarkGray

# Run ExifTool

$raw  = & $ExifToolPath -v3 $FilePath 2>&1
$text = $raw -join "`n"

# Atom extraction
# ExifTool -v3 output for uuid atoms:
#   UUID-Unknown = <16 UUID bytes><4 name bytes><value>

function Get-AtomValue {
    param([string] $AtomName)
    $pattern = "UUID-Unknown\s*=.*?$([regex]::Escape($AtomName))\s*(.+)"
    $m = [regex]::Match($text, $pattern)
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return $null
}

# Required atoms

$required = @(
    @{ Name = "ttl1"; Label = "Main Title (ttl1)"        },
    @{ Name = "dvat"; Label = "Main Title DVA (dvat)"    },
    @{ Name = "ttls"; Label = "Sub Title (ttls)"         },
    @{ Name = "ttld"; Label = "Description (ttld)"       },
    @{ Name = "plen"; Label = "Program Length (plen)"    },
    @{ Name = "date"; Label = "Creation Date (date)"     },
    @{ Name = "unam"; Label = "Client Name (unam)"       },
    @{ Name = "emal"; Label = "Client Email (emal)"      },
    @{ Name = "cpnm"; Label = "Location Number (cpnm)"   },
    @{ Name = "tpnm"; Label = "Project ID (tpnm)"        },
    @{ Name = "numc"; Label = "Chapter Count (numc)"     }
)

$passCount = 0
$failCount = 0

Write-Host ""

foreach ($atom in $required) {
    $value = Get-AtomValue -AtomName $atom.Name
    if ($null -ne $value -and $value.Length -gt 0) {
        Write-Host ("  PASS  {0,-28} {1}" -f $atom.Label, $value) -ForegroundColor Green
        $passCount++
    } else {
        Write-Host ("  FAIL  {0,-28} (not found)" -f $atom.Label) -ForegroundColor Red
        $failCount++
    }
}

# Box structure check

Write-Host ""
Write-Host "Box structure:" -ForegroundColor Gray

$hasFtyp = $text -match "Tag 'ftyp'"
$hasMoov = $text -match "Tag 'moov'"
$hasMdat = $text -match "Tag 'mdat'"
$hasUdta = $text -match "Tag 'udta'"
$hasMeta = $text -match "Tag 'meta'"

foreach ($check in @(
    @{ Label = "ftyp box"; Pass = $hasFtyp },
    @{ Label = "moov box"; Pass = $hasMoov },
    @{ Label = "udta box"; Pass = $hasUdta },
    @{ Label = "meta box"; Pass = $hasMeta },
    @{ Label = "mdat box"; Pass = $hasMdat }
)) {
    if ($check.Pass) {
        Write-Host ("  PASS  {0}" -f $check.Label) -ForegroundColor Green
        $passCount++
    } else {
        Write-Host ("  FAIL  {0}" -f $check.Label) -ForegroundColor Red
        $failCount++
    }
}

# Summary

Write-Host ""
Write-Host ("-" * 60) -ForegroundColor DarkGray

$total = $passCount + $failCount
if ($failCount -eq 0) {
    Write-Host "  RESULT: ALL $total CHECKS PASSED" -ForegroundColor Green
} else {
    Write-Host "  RESULT: $failCount of $total checks FAILED" -ForegroundColor Red
}

Write-Host ""

exit $failCount
