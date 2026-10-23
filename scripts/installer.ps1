$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$destination = Join-Path $env:LOCALAPPDATA 'Programs/FenetreAsuka'
if (-not (Test-Path "$source/FenetreAsuka.exe")) { throw 'Extraire le ZIP complet avant de lancer ce script.' }
if (Get-Process FenetreAsuka -ErrorAction SilentlyContinue) { throw 'Fermez Fenêtre Asuka avant de l''installer ou de la mettre à jour.' }
if ([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($destination)) {
    New-Item $destination -ItemType Directory -Force | Out-Null
    Get-ChildItem $source | Copy-Item -Destination $destination -Recurse -Force
}
$menu = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs/Fenêtre Asuka.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($menu)
$shortcut.TargetPath = Join-Path $destination 'FenetreAsuka.exe'
$shortcut.WorkingDirectory = $destination
$shortcut.Save()
Write-Host 'Installation terminée pour votre compte Windows, sans administrateur.'
Start-Process "$destination/FenetreAsuka.exe"
