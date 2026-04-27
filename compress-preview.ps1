<#
.SYNOPSIS
    Compresses a raw CRUD feature preview recording and places it in BIA.ToolKit/Resources/Previews/.

.DESCRIPTION
    Takes a raw video capture (any format ffmpeg can read), applies the HQ preset used by the
    CRUD Generator preview tiles (960w - CRF 25 - 24 fps - H.264 - no audio - faststart),
    and writes the result as BIA.ToolKit/Resources/Previews/{Feature}.mp4.

    The FeaturePreviewVideo UserControl picks it up automatically on next build.

.PARAMETER Source
    Path to the raw recording. Any container/codec ffmpeg supports.

.PARAMETER Feature
    Target feature name. Must match a <uc:FeaturePreviewVideo FeatureName="..."/> in
    CRUDGeneratorUC.xaml. Known values:
      fixable · fixable-parent · show-history · form-readonly · import · advanced-filter

.EXAMPLE
    .\compress-preview.ps1 -Source "C:\tmp\raw-fixable.mkv" -Feature fixable

.EXAMPLE
    .\compress-preview.ps1 C:\tmp\raw.mp4 advanced-filter
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Source,

    [Parameter(Mandatory = $true, Position = 1)]
    [ValidateSet('fixable', 'fixable-parent', 'show-history', 'form-readonly', 'import', 'advanced-filter')]
    [string]$Feature
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error "ffmpeg is not on PATH. Install it (e.g. 'scoop install ffmpeg') and retry."
    exit 1
}

if (-not (Test-Path -LiteralPath $Source)) {
    Write-Error "Source file not found: $Source"
    exit 1
}

$repoRoot  = Split-Path -Parent $PSCommandPath
$targetDir = Join-Path $repoRoot 'BIA.ToolKit\Resources\Previews'
$target    = Join-Path $targetDir "$Feature.mp4"

if (-not (Test-Path -LiteralPath $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
}

Write-Host "Compressing '$Source' -> '$target'..." -ForegroundColor Cyan

& ffmpeg -y -i $Source `
    -vf 'scale=960:-2,fps=24' `
    -c:v libx264 -crf 25 -preset slow `
    -an `
    -movflags +faststart `
    $target

if ($LASTEXITCODE -ne 0) {
    Write-Error "ffmpeg failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$sizeKb = [Math]::Round((Get-Item -LiteralPath $target).Length / 1KB, 1)
Write-Host "OK - $target ($sizeKb KB)" -ForegroundColor Green
