<#
.SYNOPSIS
    Removes BIA Migration Agent files and settings from a target project.

.DESCRIPTION
    Cleans up the Copilot prompt files, migration data, and VS Code settings
    that were installed by Install-MigrationAgent.ps1.

.PARAMETER TargetProjectPath
    Path to the root of the BIA Framework project.

.PARAMETER KeepSettings
    If set, VS Code settings added during install will NOT be reverted.

.EXAMPLE
    .\Uninstall-MigrationAgent.ps1 -TargetProjectPath "C:\Projects\MyBIAProject"
    .\Uninstall-MigrationAgent.ps1 -TargetProjectPath "C:\Projects\MyBIAProject" -KeepSettings
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetProjectPath,

    [Parameter(Mandatory = $false)]
    [switch]$KeepSettings
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $TargetProjectPath)) {
    Write-Error "Target project path does not exist: $TargetProjectPath"
    exit 1
}

# Load the install manifest
$manifestPath = Join-Path $TargetProjectPath ".github\copilot\migration-manifest.json"
if (-not (Test-Path $manifestPath)) {
    Write-Warning "No migration manifest found at $manifestPath."
    Write-Warning "This project may not have been installed via Install-MigrationAgent.ps1."

    # Attempt cleanup by scanning for known files
    $copilotDir = Join-Path $TargetProjectPath ".github\copilot"
    if (Test-Path $copilotDir) {
        $promptFiles = Get-ChildItem (Join-Path $copilotDir "prompts") -Filter "migration-*.prompt.md" -ErrorAction SilentlyContinue
        $dataFolders = Get-ChildItem (Join-Path $copilotDir "migration-data") -Directory -ErrorAction SilentlyContinue

        if ($promptFiles -or $dataFolders) {
            Write-Host "Found migration files:" -ForegroundColor Yellow
            $promptFiles | ForEach-Object { Write-Host "  Prompt: $($_.FullName)" -ForegroundColor Gray }
            $dataFolders | ForEach-Object { Write-Host "  Data:   $($_.FullName)" -ForegroundColor Gray }
            $continue = Read-Host "Delete these files? (y/N)"
            if ($continue -eq 'y') {
                $promptFiles | Remove-Item -Force
                $dataFolders | Remove-Item -Recurse -Force
                Write-Host "Migration files removed." -ForegroundColor Green
            }
        }
        else {
            Write-Host "No migration files found." -ForegroundColor DarkGray
        }
    }
    exit 0
}

# Read manifest
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
Write-Host "Found migration manifest (installed: $($manifest.installedDate))" -ForegroundColor Cyan

# Remove prompt file
$promptFilePath = Join-Path $TargetProjectPath $manifest.promptFile
if (Test-Path $promptFilePath) {
    Remove-Item $promptFilePath -Force
    Write-Host "Removed prompt file: $($manifest.promptFile)" -ForegroundColor DarkGray
}

# Remove migration data folder
$dataFolderPath = Join-Path $TargetProjectPath $manifest.dataFolder
if (Test-Path $dataFolderPath) {
    Remove-Item $dataFolderPath -Recurse -Force
    Write-Host "Removed migration data: $($manifest.dataFolder)" -ForegroundColor DarkGray
}

# Remove manifest itself
Remove-Item $manifestPath -Force
Write-Host "Removed manifest file" -ForegroundColor DarkGray

# Clean up empty directories
$promptsDir = Join-Path $TargetProjectPath ".github\copilot\prompts"
$dataDir = Join-Path $TargetProjectPath ".github\copilot\migration-data"
$copilotDir = Join-Path $TargetProjectPath ".github\copilot"

foreach ($dir in @($promptsDir, $dataDir, $copilotDir)) {
    if ((Test-Path $dir) -and ((Get-ChildItem $dir -Force).Count -eq 0)) {
        Remove-Item $dir -Force
        Write-Host "Removed empty directory: $dir" -ForegroundColor DarkGray
    }
}

# Revert VS Code settings
if (-not $KeepSettings -and $manifest.addedSettings -and $manifest.addedSettings.Count -gt 0) {
    $settingsFile = Join-Path $TargetProjectPath ".vscode\settings.json"
    if (Test-Path $settingsFile) {
        $settings = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $removedKeys = @()

        foreach ($key in $manifest.addedSettings) {
            if ($settings.PSObject.Properties.Name -contains $key) {
                $settings.PSObject.Properties.Remove($key)
                $removedKeys += $key
            }
        }

        if ($removedKeys.Count -gt 0) {
            # Check if settings object is now empty
            if ($settings.PSObject.Properties.Count -eq 0) {
                Remove-Item $settingsFile -Force
                Write-Host "Removed empty .vscode/settings.json" -ForegroundColor DarkGray

                # Remove .vscode if empty
                $vscodeDir = Join-Path $TargetProjectPath ".vscode"
                if ((Test-Path $vscodeDir) -and ((Get-ChildItem $vscodeDir -Force).Count -eq 0)) {
                    Remove-Item $vscodeDir -Force
                    Write-Host "Removed empty .vscode directory" -ForegroundColor DarkGray
                }
            }
            else {
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding UTF8
            }
            Write-Host "Reverted VS Code settings: $($removedKeys -join ', ')" -ForegroundColor DarkGray
        }
    }
}
elseif ($KeepSettings) {
    Write-Host "VS Code settings kept as-is (-KeepSettings flag)" -ForegroundColor DarkGray
}

# --- Done ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Migration Agent uninstalled            " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
