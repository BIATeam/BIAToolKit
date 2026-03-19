<#
.SYNOPSIS
    Installs BIA Migration Agent prompt files into a target BIA Framework project.

.DESCRIPTION
    Copies the Copilot prompt file and migration data into the target project so that
    developers can run the migration from GitHub Copilot (agent mode) in VS Code.

.PARAMETER TargetProjectPath
    Path to the root of the BIA Framework project to migrate.

.PARAMETER MigrationVersion
    Migration identifier (e.g., "v5-to-v6"). Defaults to "v5-to-v6".

.PARAMETER SourcePath
    Path to the BIA.ToolKit.MigrationAgent folder. Defaults to the script's parent directory.

.EXAMPLE
    .\Install-MigrationAgent.ps1 -TargetProjectPath "C:\Projects\MyBIAProject"
    .\Install-MigrationAgent.ps1 -TargetProjectPath "C:\Projects\MyBIAProject" -MigrationVersion "v6-to-v7"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetProjectPath,

    [Parameter(Mandatory = $false)]
    [string]$MigrationVersion = "v5-to-v6",

    [Parameter(Mandatory = $false)]
    [string]$SourcePath
)

$ErrorActionPreference = "Stop"

# Resolve source path (default: script's parent = BIA.ToolKit.MigrationAgent/ or zip root)
if (-not $SourcePath) {
    # Try 1: repo structure (scripts/ is inside BIA.ToolKit.MigrationAgent/)
    $SourcePath = Split-Path -Parent $PSScriptRoot

    # Try 2: BIAToolKit repo root (BIA.ToolKit.MigrationAgent/ is a sibling)
    if (-not (Test-Path (Join-Path $SourcePath "prompts"))) {
        $candidate = Join-Path (Split-Path -Parent $SourcePath) "BIA.ToolKit.MigrationAgent"
        if (Test-Path (Join-Path $candidate "prompts")) {
            $SourcePath = $candidate
        }
    }
}

# --- Validation ---

# Check target project exists
if (-not (Test-Path $TargetProjectPath)) {
    Write-Error "Target project path does not exist: $TargetProjectPath"
    exit 1
}

# Check source prompt file exists
$promptFile = Join-Path $SourcePath "prompts\migration-$MigrationVersion.prompt.md"
if (-not (Test-Path $promptFile)) {
    Write-Error "Prompt file not found: $promptFile. Available migrations:"
    Get-ChildItem (Join-Path $SourcePath "prompts") -Filter "migration-*.prompt.md" | ForEach-Object { Write-Host "  - $($_.BaseName -replace 'migration-','')" }
    exit 1
}

# Check source data exists
$sourceDataPath = Join-Path $SourcePath "sources\$MigrationVersion"
if (-not (Test-Path $sourceDataPath)) {
    Write-Error "Migration data not found: $sourceDataPath"
    exit 1
}

# Detect BIA project: find Constants.cs with FrameworkVersion
$constantsFile = $null
# New structure: DotNet\*.Crosscutting.Common\Constants.cs
$constantsFile = Get-ChildItem -Path $TargetProjectPath -Filter "Constants.cs" -Recurse -ErrorAction SilentlyContinue |
Where-Object { $_.FullName -match '\\DotNet\\[^\\]+\.[^\\]+\.Crosscutting\.Common\\Constants\.cs$' } |
Select-Object -First 1

if (-not $constantsFile) {
    # Old structure: *.Common\Constants.cs
    $constantsFile = Get-ChildItem -Path $TargetProjectPath -Filter "Constants.cs" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\[^\\]+\.[^\\]+\.Common\\Constants\.cs$' } |
    Select-Object -First 1
}

if (-not $constantsFile) {
    Write-Warning "Could not find Constants.cs with FrameworkVersion. This may not be a BIA Framework project."
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y') { exit 0 }
}
else {
    # Extract FrameworkVersion
    $content = Get-Content $constantsFile.FullName -Raw
    if ($content -match 'FrameworkVersion\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"') {
        $detectedVersion = $Matches[1]
        Write-Host "Detected BIA Framework version: $detectedVersion" -ForegroundColor Cyan

        # Load migration config to check source version compatibility
        $configFile = Join-Path $sourceDataPath "migration-config.json"
        if (Test-Path $configFile) {
            $config = Get-Content $configFile -Raw | ConvertFrom-Json
            $expectedMajor = $config.sourceVersion.Split('.')[0]
            $detectedMajor = $detectedVersion.Split('.')[0]
            if ($detectedMajor -ne $expectedMajor) {
                Write-Warning "Project version ($detectedVersion) does not match migration source version ($($config.sourceVersion)). Migration '$MigrationVersion' expects V$expectedMajor."
                $continue = Read-Host "Continue anyway? (y/N)"
                if ($continue -ne 'y') { exit 0 }
            }
        }
    }
    else {
        Write-Warning "Could not extract FrameworkVersion from Constants.cs"
    }

    # Extract CompanyName and ProjectName from namespace
    if ($content -match 'namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.') {
        $companyName = $Matches[1]
        $projectName = $Matches[2]
        Write-Host "Detected Company: $companyName, Project: $projectName" -ForegroundColor Cyan
    }
}

# --- Installation ---

# Create target directories
$targetPromptsDir = Join-Path $TargetProjectPath ".github\copilot\prompts"
$targetDataDir = Join-Path $TargetProjectPath ".github\copilot\migration-data\$MigrationVersion"

Write-Host "Creating directories..." -ForegroundColor DarkGray
New-Item -ItemType Directory -Path $targetPromptsDir -Force | Out-Null
New-Item -ItemType Directory -Path $targetDataDir -Force | Out-Null

# Copy prompt file
Write-Host "Copying prompt file..." -ForegroundColor DarkGray
Copy-Item -Path $promptFile -Destination $targetPromptsDir -Force

# Copy report-migration-fix prompt (shared across all migration versions)
$reportPromptFile = Join-Path $SourcePath "prompts\report-migration-fix.prompt.md"
if (Test-Path $reportPromptFile) {
    Copy-Item -Path $reportPromptFile -Destination $targetPromptsDir -Force
    Write-Host "Copied report-migration-fix.prompt.md" -ForegroundColor DarkGray
}

# Copy migration data
Write-Host "Copying migration data..." -ForegroundColor DarkGray
Copy-Item -Path "$sourceDataPath\*" -Destination $targetDataDir -Recurse -Force

# Configure VS Code settings
$vscodeDir = Join-Path $TargetProjectPath ".vscode"
$settingsFile = Join-Path $vscodeDir "settings.json"
$backupFile = Join-Path $vscodeDir "settings.json.migration-backup"

if (-not (Test-Path $vscodeDir)) {
    New-Item -ItemType Directory -Path $vscodeDir -Force | Out-Null
}

# Backup original settings.json as a sidecar file (restored verbatim on uninstall)
if (Test-Path $settingsFile) {
    Copy-Item -Path $settingsFile -Destination $backupFile -Force
    Write-Host "Backed up .vscode/settings.json → settings.json.migration-backup" -ForegroundColor DarkGray
    $settings = Get-Content $settingsFile -Raw -Encoding UTF8 | ConvertFrom-Json
}
else {
    Write-Host "No existing .vscode/settings.json — will create one" -ForegroundColor DarkGray
    $settings = [PSCustomObject]@{}
}

# Migration-specific settings — always set, overwrite any existing values
# The original values are safe in the .bak file
$migrationSettings = @{
    "chat.promptFiles"                      = $true
    "chat.agent.maxRequests"                = 100
    "chat.tools.autoApprove"                = $true
    "chat.tools.terminal.enableAutoApprove" = $true
    "chat.tools.terminal.autoApprove"       = @(
        "git", "git diff", "git add", "git commit", "git rev-parse",
        "git status", "git checkout", "git apply", "git merge-file",
        "dotnet", "dotnet build", "dotnet restore", "dotnet format",
        "npm", "npm install", "npm run",
        "find", "rm", "cp", "mv", "mkdir", "cat"
    )
}

foreach ($key in $migrationSettings.Keys) {
    if ($settings.PSObject.Properties.Name -contains $key) {
        $settings.PSObject.Properties[$key].Value = $migrationSettings[$key]
    }
    else {
        $settings | Add-Member -NotePropertyName $key -NotePropertyValue $migrationSettings[$key]
    }
}

$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding UTF8
Write-Host "Configured VS Code settings: $($migrationSettings.Keys -join ', ')" -ForegroundColor DarkGray

# Save a manifest of what was installed (for uninstall)
$manifest = @{
    migrationVersion = $MigrationVersion
    installedDate    = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    promptFile       = ".github\copilot\prompts\migration-$MigrationVersion.prompt.md"
    reportPromptFile = ".github\copilot\prompts\report-migration-fix.prompt.md"
    dataFolder       = ".github\copilot\migration-data\$MigrationVersion"
    settingsBackup   = if (Test-Path $backupFile) { ".vscode\settings.json.migration-backup" } else { $null }
}
$manifestPath = Join-Path $TargetProjectPath ".github\copilot\migration-manifest.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content $manifestPath -Encoding UTF8

# --- Done ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Migration Agent installed successfully " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installed files:" -ForegroundColor White
Write-Host "  Prompt:  $targetPromptsDir\migration-$MigrationVersion.prompt.md" -ForegroundColor Gray
Write-Host "  Report:  $targetPromptsDir\report-migration-fix.prompt.md" -ForegroundColor Gray
Write-Host "  Data:    $targetDataDir\" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open your project in VS Code" -ForegroundColor White
Write-Host "  2. Open Copilot Chat (Ctrl+Shift+I)" -ForegroundColor White
Write-Host "  3. Switch to Agent mode" -ForegroundColor White
Write-Host "  4. Attach the prompt: #file:.github/copilot/prompts/migration-$MigrationVersion.prompt.md" -ForegroundColor White
Write-Host "  5. Type: 'Run this migration' or 'Migre mon projet'" -ForegroundColor White
Write-Host ""
Write-Host "After migration, to document manual fixes:" -ForegroundColor Yellow
Write-Host "  Attach: #file:.github/copilot/prompts/report-migration-fix.prompt.md" -ForegroundColor White
Write-Host "  Type: 'Documente mon correctif'" -ForegroundColor White
Write-Host ""
