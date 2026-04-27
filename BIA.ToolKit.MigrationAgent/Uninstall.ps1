<#
.SYNOPSIS
    Shortcut to uninstall BIA Migration Agent from a target project.

.DESCRIPTION
    Wrapper script at the root of the distribution package.
    Forwards all parameters to scripts\Uninstall-MigrationAgent.ps1.

.EXAMPLE
    .\Uninstall.ps1 -TargetProjectPath "C:\MonProjet"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetProjectPath
)

& "$PSScriptRoot\scripts\Uninstall-MigrationAgent.ps1" -TargetProjectPath $TargetProjectPath
