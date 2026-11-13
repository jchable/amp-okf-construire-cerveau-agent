#requires -Version 7.0
param([Parameter(Mandatory)][guid]$BotId, [Parameter(Mandatory)][uri]$PublicUrl)
$ErrorActionPreference = 'Stop'
if ($BotId -eq [guid]::Empty -or $PublicUrl.Scheme -ne 'https' -or $PublicUrl.UserInfo) {
    throw "Indiquer le client ID du bot et l’URL HTTPS du tunnel."
}
$destination = Join-Path (Split-Path $PSScriptRoot) 'artifacts/teams'
$package = New-Item -ItemType Directory -Force -Path (Join-Path $destination 'package')
$manifest = (Get-Content (Join-Path $PSScriptRoot 'manifest.template.json') -Raw).
    Replace('__BOT_ID__', $BotId.ToString('D')).
    Replace('__BASE_URL__', $PublicUrl.GetLeftPart([UriPartial]::Authority))
Set-Content (Join-Path $package.FullName 'manifest.json') $manifest -Encoding utf8NoBOM
foreach ($icon in 'color.png', 'outline.png') {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $icon) -Destination $package.FullName -Force
}
$files = 'manifest.json', 'color.png', 'outline.png' | ForEach-Object { Join-Path $package.FullName $_ }
$zip = Join-Path $destination 'Acme.Teams.zip'
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Write-Host "Application à installer dans le chat personnel : $zip"
