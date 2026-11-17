#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$Teams = $false
. (Join-Path $PSScriptRoot 'Assert-AgentPrerequisites.ps1')
dotnet build (Join-Path $root 'src/Acme.Console') -c Release
if ($LASTEXITCODE) { throw "Compilation de la console interrompue ($LASTEXITCODE)." }
Write-Host "Démo 2 prête. Lancer la console avec la commande du README."
