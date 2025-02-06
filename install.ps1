if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Start-Process powershell.exe -ArgumentList "-File `"$PSCommandPath`"" -Verb RunAs
    exit
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$zipPath = Join-Path -Path $scriptPath -ChildPath "BIAToolKit.zip"
$installPath = "C:\Program Files\BIAToolKit"
$exeName = "BIA.ToolKit.exe"
$shortcutPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\BIA.ToolKit.lnk"
$iconPath = "$installPath\Images\Tools.ico"  

if (-Not (Test-Path $installPath)) {
    New-Item -ItemType Directory -Path $installPath | Out-Null
}

Expand-Archive -Path $zipPath -DestinationPath $installPath -Force

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = "$installPath\$exeName"
$Shortcut.WorkingDirectory = $installPath
$Shortcut.IconLocation = $iconPath
$Shortcut.Save()
