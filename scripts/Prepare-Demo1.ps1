#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OkfRepository
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
. (Join-Path $PSScriptRoot 'Demo1-Paths.ps1')
$source = (Resolve-Path -LiteralPath $OkfRepository).Path

# git archive lit uniquement les fichiers suivis de la révision choisie.
$marker = Join-Path $cache "export-$revision.ready"
if (-not (Test-Path -LiteralPath $marker)) {
    $null = New-Item -ItemType Directory -Path $snapshot -Force
    $archive = Join-Path $cache "export-$revision.tar"
    git -c "safe.directory=$($source.Replace('\','/'))" -C $source archive --format=tar "--output=$archive" $revision
    if ($LASTEXITCODE) { throw "Échec de git archive ($LASTEXITCODE)." }
    tar -xf $archive -C $snapshot
    if ($LASTEXITCODE) { throw "Échec de l’extraction ($LASTEXITCODE)." }
    Remove-Item -LiteralPath $archive
    Set-Content -LiteralPath $marker -Value $revision
}
$projects = [ordered]@{
    okfgen = 'producers/src/OkfProducer.Cli/OkfProducer.Cli.csproj'
    okf = 'src/OKF4net.Cli/OKF4net.Cli.csproj'
    'okf-render' = 'src/OKF4net.Render/OKF4net.Render.csproj'
}
foreach ($entry in $projects.GetEnumerator()) {
    Write-Host "Préparation de $($entry.Key)"
    dotnet publish (Join-Path $snapshot $entry.Value) -c Release -r win-x64 --self-contained false -p:PublishAot=false -p:RuntimeIdentifiers=win-x64 -o (Join-Path $tools $entry.Key)
    if ($LASTEXITCODE) { throw "Compilation de $($entry.Key) interrompue ($LASTEXITCODE)." }
}
$null = New-Item -ItemType Directory -Path $acmeSource -Force
Get-ChildItem -LiteralPath (Join-Path $root 'src/Acme.Orders') -File | Copy-Item -Destination $acmeSource
dotnet build (Join-Path $acmeSource 'Acme.Orders.csproj') -c Release
if ($LASTEXITCODE) { throw "Compilation Acme interrompue ($LASTEXITCODE)." }
& (Join-Path $PSScriptRoot 'Clear-Demo.ps1')
$acmeArtifacts = Join-Path $outputs 'acme'
$bundle = New-Item -ItemType Directory -Path (Join-Path $acmeArtifacts 'bundle') -Force
Get-ChildItem -LiteralPath (Join-Path $snapshot 'bundles/acme_retail') |
    Copy-Item -Destination $bundle.FullName -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'src/Acme.Agent/catalog.json') -Destination $acmeArtifacts
foreach ($file in 'NOTICE', 'LICENSE.Apache-2.0') {
    Copy-Item -LiteralPath (Join-Path $snapshot $file) -Destination $acmeArtifacts -Force
}
# Utiliser les chemins de scène, même dans un terminal réutilisé après une recette.
$env:OKF_MEMORY_ROOT = $null
$env:OKF_CATALOG_PATH = $null
Write-Host "Poste prêt. Dans le README, copier le bloc « préparer le terminal », puis les commandes de scène."
