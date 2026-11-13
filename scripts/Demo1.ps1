#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('Acme', 'OKF4net')]
    [string] $Corpus,
    [switch] $Open,
    [switch] $UsePreparedSite
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Demo1-Paths.ps1')
$out = Join-Path $outputs $Corpus.ToLowerInvariant()
$bundle = Join-Path $out 'bundle'
$site = Join-Path $out 'site'
$entryPage = Join-Path $site 'index.html'
$isAcme = $Corpus -eq 'Acme'
if (-not $isAcme) { $entryPage = Join-Path $site 'graph.html' }

if (-not $UsePreparedSite) {
    $generator = Join-Path $tools 'okfgen/OkfProducer.Cli.exe'
    $validator = Join-Path $tools 'okf/okf.exe'
    $renderer = Join-Path $tools 'okf-render/okf-render.exe'
    foreach ($exe in $generator, $validator, $renderer) {
        if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
            throw "Outil manquant ($exe). Lancer scripts/Prepare-Demo1.ps1 avant la séance."
        }
    }
    $null = New-Item -ItemType Directory -Path $out -Force
    if ($isAcme) {
        dotnet (Join-Path $acmeSource 'bin/Release/net10.0/Acme.Orders.dll')
        if ($LASTEXITCODE) { throw 'Échec du scénario Acme.' }
        $null = New-Item -ItemType Directory -Path $bundle -Force
        Get-ChildItem -LiteralPath (Join-Path $snapshot 'bundles/acme_retail') |
            Copy-Item -Destination $bundle -Recurse -Force
        # Les notices accompagnent la copie distribuable, hors du corpus indexé.
        foreach ($file in 'NOTICE', 'LICENSE.Apache-2.0') {
            Copy-Item -LiteralPath (Join-Path $snapshot $file) -Destination $out -Force
        }
        $arguments = @('generate', '--repo', $acmeSource, '--out', $bundle, '--update')
    } else {
        # Le grand graphe illustre la structure publique, avec résolution des noms
        # par Tree-sitter. Cela évite une évaluation MSBuild de chaque projet.
        $arguments = @('generate', '--repo', $snapshot, '--out', $bundle, '--no-msbuild',
            '--repo-url', 'https://github.com/jchable/okf4net', '--rev', $revision)
        if (Test-Path -LiteralPath $bundle) { $arguments += '--update' }
    }

    Write-Host ("okfgen " + ($arguments -join ' '))
    & $generator @arguments
    if ($LASTEXITCODE) { throw "Génération interrompue ($LASTEXITCODE)." }

    # La date commune aux deux exemples stabilise les diagnostics de péremption.
    Write-Host "okf validate $bundle --as-of 2026-11-12"
    $report = Join-Path $out 'validation.txt'
    & $validator validate $bundle --as-of 2026-11-12 > $report
    $validationExit = $LASTEXITCODE
    Get-Content -LiteralPath $report -Tail 2
    if ($validationExit) { throw "Validation en échec. Voir $report" }

    Write-Host "okf-render $bundle --out $site"
    & $renderer $bundle --out $site
    if ($LASTEXITCODE) { throw "Rendu interrompu ($LASTEXITCODE)." }
}

if (-not (Test-Path -LiteralPath $entryPage -PathType Leaf)) {
    throw "Page absente. Lancer ./scripts/Demo1.ps1 $Corpus pour préparer le rendu."
}
Write-Host "Page à projeter : $entryPage"
Write-Host "Bundle : $bundle"
if ($Open) { Invoke-Item -LiteralPath $entryPage }
