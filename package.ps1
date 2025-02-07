function Get-AppVersion {
    param (
        [string]$projectPath
    )

    $csprojPath = Get-ChildItem -Path $projectPath -Filter "*.csproj" | Select-Object -First 1 | ForEach-Object { $_.FullName }

    if (-not $csprojPath) {
        Write-Host "Unable to find .csproj in $projectPath." -ForegroundColor Red
        exit 1
    }

    [xml]$csprojContent = Get-Content $csprojPath
    return $csprojContent.Project.PropertyGroup.Version
}

# Set immutable variables
$biaToolKitProjectPath = ".\BIA.ToolKit"
$updaterProjectPath = ".\BIA.ToolKit.Updater"
$packageJsonPath = "./package.json"

# Getting package.json data
if (-not (Test-Path $packageJsonPath)) {
    Write-Host "Error: package.json not found." -ForegroundColor Red
    exit 1
}
$packageJson = Get-Content $packageJsonPath | ConvertFrom-Json
if (-not $packageJson.distributionServer -or -not $packageJson.packageArchiveName -or -not $packageJson.packageVersionFileName) {
    Write-Host "Error: Missing required values in package.json." -ForegroundColor Red
    exit 1
}

# Set working variables
$distributionServer = $packageJson.distributionServer
$updateArchiveName = $packageJson.packageArchiveName
$versionFile = $packageJson.packageVersionFileName
$distributionServerBackupFolder = "$distributionServer\Backups"
$distributionServerBackupBiaToolKitFolder = "$distributionServerBackupFolder\BiaToolKit"
$distributionServerBackupUpdaterFolder = "$distributionServerBackupFolder\Updater"

# Create distribution server path if not exist
if (-not (Test-Path $distributionServer)) {
    New-Item -ItemType Directory -Path $distributionServer | Out-Null
}

# Create distribution server backup for biatoolkit path if not exist
if (-not (Test-Path $distributionServerBackupBiaToolKitFolder)) {
    New-Item -ItemType Directory -Path $distributionServerBackupBiaToolKitFolder | Out-Null
}

# Create distribution server backup for updater path if not exist
if (-not (Test-Path $distributionServerBackupUpdaterFolder)) {
    New-Item -ItemType Directory -Path $distributionServerBackupUpdaterFolder | Out-Null
}

# Get biatoolkit version
$biaToolKitVersion = Get-AppVersion $biaToolKitProjectPath
if (-not $biaToolKitVersion) {
    Write-Host "Unable to retrieve BIAToolKit application version." -ForegroundColor Red
    exit 1
}

# Get updater version
$updaterVersion = Get-AppVersion $updaterProjectPath
if (-not $updaterVersion) {
    Write-Host "Unable to retrieve updater version." -ForegroundColor Red
    exit 1
}

# Start packaging
Write-Host "Packaging [BIAToolKit@$biaToolKitVersion;Updater@$updaterVersion]" -ForegroundColor Green

# Publish biatoolkit
Write-Host "Publishing BIAToolKit..." -ForegroundColor Yellow
$publishResult = dotnet publish $biaToolKitProjectPath -c Release -r win-x64 --self-contained false -o "$biaToolKitProjectPath\publish" 2>&1
$errors = $publishResult | Where-Object { $_ -match "error" }
if ($LASTEXITCODE -ne 0) {
    Write-Host "BIAToolKit publish failed." -ForegroundColor Red
    if ($errors) { Write-Host $errors -ForegroundColor Red }
    exit 1
}

# Publish updater
Write-Host "Publishing updater..." -ForegroundColor Yellow
$publishResult = dotnet publish $updaterProjectPath -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o "$updaterProjectPath\publish" 2>&1
$errors = $publishResult | Where-Object { $_ -match "error" }
if ($LASTEXITCODE -ne 0) {
    Write-Host "Updater publish failed." -ForegroundColor Red
    if ($errors) { Write-Host $errors -ForegroundColor Red }
    exit 1
}

# Creating biatoolkit update archive
Write-Host "Creating update archive..." -ForegroundColor Yellow
Compress-Archive -Path "$biaToolKitProjectPath\publish\*" -DestinationPath "$biaToolKitProjectPath\$updateArchiveName" -Force

# Copy biatoolkit update archive to distribion server and backup
Write-Host "Copying update archive to server..." -ForegroundColor Yellow
Copy-Item "$biaToolKitProjectPath\$updateArchiveName" "$distributionServer\$updateArchiveName" -Force
Copy-Item "$biaToolKitProjectPath\$updateArchiveName" "$distributionServerBackupBiaToolKitFolder\$($biaToolKitVersion)_$updateArchiveName" -Force
Remove-Item "$biaToolKitProjectPath\$updateArchiveName" -Force

# Copy updater to distribion server and backup
Write-Host "Copying updater to server..." -ForegroundColor Yellow
$updaterExe = Get-ChildItem "$updaterProjectPath\publish" -Filter "*.exe" | Select-Object -First 1
if (-not $updaterExe) {
    Write-Host "Unable to find .exe in $updaterProjectPath\publish." -ForegroundColor Red
    exit 1
}
Copy-Item "$updaterProjectPath\publish\$($updaterExe.Name)" "$distributionServer\$($updaterExe.Name)" -Force
Copy-Item "$updaterProjectPath\publish\$($updaterExe.Name)" "$distributionServerBackupUpdaterFolder\$($updaterVersion)_$($updaterExe.Name)" -Force

# Update version file with biatoolkit version
Write-Host "Updating version.txt..." -ForegroundColor Yellow
Set-Content -Path "$distributionServer\$versionFile" -Value $biaToolKitVersion

# End packaging
Write-Host "Packaged successfully!" -ForegroundColor Green
