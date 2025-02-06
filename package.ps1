$biaToolKitProjectPath = ".\BIA.ToolKit"
$updaterProjectPath = ".\BIA.ToolKit.Updater"
$installScriptName = "install.ps1"

$packageJsonPath = "./package.json"

if (-not (Test-Path $packageJsonPath)) {
    Write-Host "Error: package.json not found." -ForegroundColor Red
    exit 1
}

$packageJson = Get-Content $packageJsonPath | ConvertFrom-Json

if (-not $packageJson.distributionServer -or -not $packageJson.packageArchiveName -or -not $packageJson.packageVersionFileName) {
    Write-Host "Error: Missing required values in package.json." -ForegroundColor Red
    exit 1
}

$distributionServer = $packageJson.distributionServer
$updateArchiveName = $packageJson.packageArchiveName
$versionFile = $packageJson.packageVersionFileName

$csprojPath = Get-ChildItem -Path $biaToolKitProjectPath -Filter "*.csproj" | Select-Object -First 1 | ForEach-Object { $_.FullName }

if (-not $csprojPath) {
    Write-Host "Unable to find .csproj in $biaToolKitProjectPath." -ForegroundColor Red
    exit 1
}

[xml]$csprojContent = Get-Content $csprojPath
$biaToolKitVersion = $csprojContent.Project.PropertyGroup.Version

if (-not $biaToolKitVersion) {
    Write-Host "Unable to retrieve application version." -ForegroundColor Red
    exit 1
}

Write-Host "Publishing BIAToolKit (version $biaToolKitVersion)" -ForegroundColor Green

Write-Host "Publishing BIAToolKit..." -ForegroundColor Yellow
$publishResult = dotnet publish $biaToolKitProjectPath -c Release -r win-x64 --self-contained false -o "$biaToolKitProjectPath\publish" 2>&1
$errors = $publishResult | Where-Object { $_ -match "error" }
if ($LASTEXITCODE -ne 0) {
    Write-Host "BIAToolKit publish failed." -ForegroundColor Red
    if ($errors) { Write-Host $errors -ForegroundColor Red }
    exit 1
}

Write-Host "Creating update archive..." -ForegroundColor Yellow
Compress-Archive -Path "$biaToolKitProjectPath\publish\*" -DestinationPath "$biaToolKitProjectPath\$updateArchiveName" -Force

Write-Host "Copying update archive to server..." -ForegroundColor Yellow
Copy-Item "$biaToolKitProjectPath\$updateArchiveName" "$distributionServer\$updateArchiveName" -Force
Remove-Item "$biaToolKitProjectPath\$updateArchiveName" -Force

Write-Host "Publishing updater..." -ForegroundColor Yellow
$publishResult = dotnet publish $updaterProjectPath -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o "$updaterProjectPath\publish" 2>&1
$errors = $publishResult | Where-Object { $_ -match "error" }
if ($LASTEXITCODE -ne 0) {
    Write-Host "Updater publish failed." -ForegroundColor Red
    if ($errors) { Write-Host $errors -ForegroundColor Red }
    exit 1
}

Write-Host "Copying updater to server..." -ForegroundColor Yellow
$updaterExe = Get-ChildItem "$updaterProjectPath\publish" -Filter "*.exe" | Select-Object -First 1
if ($updaterExe) {
    Copy-Item "$updaterProjectPath\publish\$($updaterExe.Name)" "$distributionServer\$($updaterExe.Name)" -Force
} else {
    Write-Host "Unable to find .exe in $updaterProjectPath\publish." -ForegroundColor Red
    exit 1
}

Write-Host "Copying installer.ps1..." -ForegroundColor Yellow
Copy-Item "$installScriptName" "$distributionServer\$installScriptName" -Force

Write-Host "Updating version.txt..." -ForegroundColor Yellow
Set-Content -Path "$distributionServer\$versionFile" -Value $biaToolKitVersion

Write-Host "Published successfully!" -ForegroundColor Green
