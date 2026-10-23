$ErrorActionPreference = 'Stop'
if (Get-Process FenetreAsuka -ErrorAction SilentlyContinue) { throw 'Fermez Fenêtre Asuka avant de la désinstaller.' }
$answer = Read-Host 'Supprimer l''application installée, le code, la signature faciale, le jeton et tous les réglages de ce compte ? Tapez EFFACER'
if ($answer -cne 'EFFACER') { Write-Host 'Annulé.'; exit 0 }
Remove-ItemProperty 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Run' -Name 'FenetreAsuka' -ErrorAction SilentlyContinue
$shortcut = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs/Fenêtre Asuka.lnk'
if (Test-Path $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
foreach ($path in @((Join-Path $env:LOCALAPPDATA 'FenetreAsuka'), (Join-Path $env:LOCALAPPDATA 'Programs/FenetreAsuka'))) {
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Recurse -Force }
}
Write-Host 'Application et données locales supprimées. Vous pouvez aussi supprimer le ZIP et son dossier extrait.'
