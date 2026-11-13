# Emplacement local commun aux deux lanceurs. Les outils OKF ignorent les points
# de réanalyse, y compris certains fichiers synchronisés par Dropbox.
$revision = '521ea0cecf543ac73b5dce4f12c84b945e06a5a8'
$root = Split-Path $PSScriptRoot
$cache = Join-Path ([IO.Path]::GetTempPath()) 'amp-lyon-2026-demo1'
$snapshot = Join-Path $cache "source-$($revision.Substring(0,8))"
$tools = Join-Path $cache 'tools'
$acmeSource = Join-Path $cache 'src/Acme.Orders'
$outputs = Join-Path $cache 'artifacts'
