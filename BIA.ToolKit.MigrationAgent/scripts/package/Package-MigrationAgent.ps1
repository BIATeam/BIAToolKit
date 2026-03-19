<#
.SYNOPSIS
    Generates a distributable .zip for the BIA Migration Agent.

.DESCRIPTION
    Packages the scripts, prompts, and source data for a specific migration version
    into a zip file ready to distribute to developers.

.PARAMETER MigrationVersion
    Migration identifier (e.g., "v6-to-v7"). Defaults to "v6-to-v7".

.PARAMETER OutputPath
    Where to write the zip file. Defaults to the current directory.

.EXAMPLE
    .\Package-MigrationAgent.ps1
    .\Package-MigrationAgent.ps1 -MigrationVersion "v6-to-v7" -OutputPath "C:\Releases"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$MigrationVersion = "v6-to-v7",

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

# Resolve paths
$agentRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "BIA-MigrationAgent-package-$(Get-Random)"
$packageName = "BIA-MigrationAgent-$MigrationVersion"
$stagingDir = Join-Path $tempDir $packageName

Write-Host "Packaging BIA Migration Agent ($MigrationVersion)..." -ForegroundColor Cyan
Write-Host "Source: $agentRoot" -ForegroundColor DarkGray

# --- Validation ---

$promptFile = Join-Path $agentRoot "prompts\migration-$MigrationVersion.prompt.md"
if (-not (Test-Path $promptFile)) {
    Write-Error "Prompt file not found: $promptFile"
    exit 1
}

$sourceDataDir = Join-Path $agentRoot "sources\$MigrationVersion"
if (-not (Test-Path $sourceDataDir)) {
    Write-Error "Source data not found: $sourceDataDir"
    exit 1
}

# --- Build staging directory ---

Write-Host "Building package structure..." -ForegroundColor DarkGray

# Scripts
$scriptsDir = Join-Path $stagingDir "scripts"
New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null
Copy-Item (Join-Path $agentRoot "scripts\Install-MigrationAgent.ps1") -Destination $scriptsDir
Copy-Item (Join-Path $agentRoot "scripts\Uninstall-MigrationAgent.ps1") -Destination $scriptsDir

# Prompts
$promptsDir = Join-Path $stagingDir "prompts"
New-Item -ItemType Directory -Path $promptsDir -Force | Out-Null
Copy-Item $promptFile -Destination $promptsDir

# Report prompt (standalone fallback)
$reportPrompt = Join-Path $agentRoot "prompts\report-migration-fix.prompt.md"
if (Test-Path $reportPrompt) {
    Copy-Item $reportPrompt -Destination $promptsDir
}

# Source data (templates, config, migration-fixes)
$targetSourceDir = Join-Path $stagingDir "sources\$MigrationVersion"
New-Item -ItemType Directory -Path $targetSourceDir -Force | Out-Null
Copy-Item "$sourceDataDir\*" -Destination $targetSourceDir -Recurse -Force

# Ensure migration-fixes directory exists (even if empty)
$fixesDir = Join-Path $targetSourceDir "migration-fixes"
if (-not (Test-Path $fixesDir)) {
    New-Item -ItemType Directory -Path $fixesDir -Force | Out-Null
}

# README
$readme = Join-Path $agentRoot "README.md"
if (Test-Path $readme) {
    Copy-Item $readme -Destination $stagingDir
}

# --- Create zip ---

$zipFile = Join-Path $OutputPath "$packageName.zip"

if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

Write-Host "Creating zip: $zipFile" -ForegroundColor DarkGray
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipFile -CompressionLevel Optimal

# --- Report ---

$zipSize = [math]::Round((Get-Item $zipFile).Length / 1KB, 1)
$fileCount = (Get-ChildItem $stagingDir -Recurse -File).Count

# --- Cleanup ---

Remove-Item $tempDir -Recurse -Force

Write-Host ""
Write-Host "=================================" -ForegroundColor Green
Write-Host " Package created successfully    " -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""
Write-Host "  File:  $zipFile" -ForegroundColor White
Write-Host "  Size:  ${zipSize} KB ($fileCount files)" -ForegroundColor White
Write-Host ""
Write-Host "Distribution:" -ForegroundColor Yellow
Write-Host "  1. Envoyer le zip au développeur" -ForegroundColor White
Write-Host "  2. Extraire quelque part (ex: C:\Tools\BIA-MigrationAgent\)" -ForegroundColor White
Write-Host "  3. Lancer:" -ForegroundColor White
Write-Host "     .\scripts\Install-MigrationAgent.ps1 -TargetProjectPath ""C:\MonProjet"" -MigrationVersion ""$MigrationVersion""" -ForegroundColor Gray
Write-Host ""
