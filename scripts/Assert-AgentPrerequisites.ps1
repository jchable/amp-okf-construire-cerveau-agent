#requires -Version 7.0
# Chargé par les préparations 2 et 3 dans leur portée locale.
. (Join-Path $PSScriptRoot 'Demo1-Paths.ps1')

$required = @('OKF_CHAT_BASE_URL', 'OKF_CHAT_MODEL', 'OKF_CHAT_API_KEY', 'ACME_TENANT_ID', 'ACME_USER_ID')
if ($Teams) { $required += 'ACME_BOT_ID', 'ACME_BOT_SECRET' }
$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($missing.Count) {
    throw ("Renseigner les variables du README dans ce terminal : " + ($missing -join ', '))
}
$ids = @('ACME_TENANT_ID', 'ACME_USER_ID')
if ($Teams) { $ids += 'ACME_BOT_ID' }
foreach ($name in $ids) {
    $id = [guid]::Empty
    if (-not [guid]::TryParse([Environment]::GetEnvironmentVariable($name), [ref]$id) -or $id -eq [guid]::Empty) {
        throw "$name doit contenir un GUID Entra valide."
    }
}
$endpoint = $null
if (-not [uri]::TryCreate($env:OKF_CHAT_BASE_URL, [UriKind]::Absolute, [ref]$endpoint) -or
    ($endpoint.Scheme -ne 'https' -and -not ($endpoint.IsLoopback -and $endpoint.Scheme -eq 'http'))) {
    throw "OKF_CHAT_BASE_URL doit être une URL HTTPS (HTTP accepté sur la boucle locale pour les tests)."
}
$catalogPath = Join-Path $outputs 'acme/catalog.json'
if ($null -ne $env:OKF_CATALOG_PATH) { $catalogPath = $env:OKF_CATALOG_PATH }
$memoryRoot = Join-Path $cache 'memory'
if ($null -ne $env:OKF_MEMORY_ROOT) { $memoryRoot = $env:OKF_MEMORY_ROOT }
if ([string]::IsNullOrWhiteSpace($catalogPath) -or [string]::IsNullOrWhiteSpace($memoryRoot)) {
    throw "Le chemin du catalogue ou de la mémoire est vide."
}
$catalogPath = [IO.Path]::GetFullPath($catalogPath)
$memoryRoot = [IO.Path]::GetFullPath($memoryRoot)
if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
    throw "Catalogue absent : $catalogPath. Exécuter la préparation et la démo 1 du README."
}
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$acme = @($catalog.sources | Where-Object { $_.id -eq 'acme' -and $_.role -eq 'knowledge' })
if ($acme.Count -ne 1 -or [string]::IsNullOrWhiteSpace($acme[0].path)) {
    throw "Le catalogue doit contenir une source acme de rôle knowledge avec son chemin."
}
$bundlePath = [IO.Path]::GetFullPath($acme[0].path, (Split-Path -Parent $catalogPath))
if (-not (Test-Path -LiteralPath (Join-Path $bundlePath 'policies/revenue-recognition.md') -PathType Leaf)) {
    throw "Politique Acme absente de $bundlePath. Exécuter la démo 1 du README."
}
$validator = Join-Path $tools 'okf/okf.exe'
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Outil okf absent. Exécuter scripts/Prepare-Demo1.ps1 avant le passage."
}
& $validator validate $bundlePath --as-of 2026-11-12
if ($LASTEXITCODE) { throw "Le bundle Acme est invalide ($LASTEXITCODE)." }
# Des chemins absolus garantissent le même stockage après un changement de dossier.
$env:OKF_CATALOG_PATH = $catalogPath
$env:OKF_MEMORY_ROOT = $memoryRoot
Write-Host "Catalogue : $catalogPath"
Write-Host "Bundle : $bundlePath"
Write-Host "Mémoire commune console et Teams : $memoryRoot"
