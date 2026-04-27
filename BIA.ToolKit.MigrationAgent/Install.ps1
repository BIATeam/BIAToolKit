<#
.SYNOPSIS
    Shortcut to install BIA Migration Agent into a target project.

.DESCRIPTION
    Wrapper script at the root of the distribution package.
    Forwards all parameters to scripts\Install-MigrationAgent.ps1.

.EXAMPLE
    .\Install.ps1 -TargetProjectPath "C:\MonProjet"
    .\Install.ps1 -TargetProjectPath "C:\MonProjet" -MigrationVersion "v6-to-v7"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetProjectPath,

    [Parameter(Mandatory = $false)]
    [string]$MigrationVersion,

    [Parameter(Mandatory = $false)]
    [string]$SourcePath
)

$scriptArgs = @{ TargetProjectPath = $TargetProjectPath }
if ($MigrationVersion) { $scriptArgs.MigrationVersion = $MigrationVersion }
if ($SourcePath) { $scriptArgs.SourcePath = $SourcePath }

& "$PSScriptRoot\scripts\Install-MigrationAgent.ps1" @scriptArgs
