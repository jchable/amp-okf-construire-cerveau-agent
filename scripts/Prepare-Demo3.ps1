#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri] $PublicUrl
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
if (-not $PublicUrl.IsAbsoluteUri -or $PublicUrl.Scheme -ne 'https' -or $PublicUrl.UserInfo) {
    throw "PublicUrl doit être l’URL HTTPS du tunnel VS Code."
}
$Teams = $true
. (Join-Path $PSScriptRoot 'Assert-AgentPrerequisites.ps1')
dotnet build (Join-Path $root 'src/Acme.Teams') -c Release
if ($LASTEXITCODE) { throw "Compilation de Teams interrompue ($LASTEXITCODE)." }
& (Join-Path $root 'teams/New-TeamsPackage.ps1') -BotId $env:ACME_BOT_ID -PublicUrl $PublicUrl
Write-Host "Endpoint à configurer dans Azure Bot : $($PublicUrl.GetLeftPart([UriPartial]::Authority))/api/messages"
Write-Host "Démo 3 prête. Lancer le service avec la commande du README."
