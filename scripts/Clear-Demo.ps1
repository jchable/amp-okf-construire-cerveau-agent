#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Demo1-Paths.ps1')

# Seuls ces deux dossiers appartiennent au passage Acme en cours.
$base = [IO.Path]::GetFullPath($cache)
$targets = @((Join-Path $outputs 'acme'), (Join-Path $cache 'memory'))
foreach ($target in $targets) {
    $full = [IO.Path]::GetFullPath($target)
    $relative = [IO.Path]::GetRelativePath($base, $full)
    if ($relative -notin 'artifacts\acme', 'memory') {
        throw "Chemin de nettoyage inattendu : $full"
    }
    # Un lien de répertoire pourrait rediriger la suppression hors du cache.
    $path = $full
    while ($true) {
        $item = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        if ($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Nettoyage interrompu sur le lien : $path"
        }
        if ($path -eq $base) { break }
        $path = Split-Path -Parent $path
    }
}
foreach ($target in $targets) {
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}
Write-Host "Passage Acme effacé (bundle, site, catalogue et mémoire)."
