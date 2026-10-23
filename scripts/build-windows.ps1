param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot)
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
function Checked { param([scriptblock]$Command) & $Command; if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE)" } }
Checked { python scripts/fetch-models.py }
Checked { dotnet restore tests/Asuka.Tests --locked-mode }
Checked { dotnet restore tests/Asuka.Models.Tests --locked-mode }
Checked { dotnet restore tests/Asuka.Windows.Tests --locked-mode }
if (-not $SkipTests) {
    Checked { dotnet test tests/Asuka.Tests -c Release --no-restore }
    Checked { dotnet test tests/Asuka.Models.Tests -c Release --no-restore }
    Checked { dotnet test tests/Asuka.Windows.Tests -c Release --no-restore }
}
Checked { dotnet restore src/Asuka.Windows -r win-x64 --locked-mode }
$out = Join-Path $PWD 'artifacts/FenetreAsuka-win-x64'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
Checked { dotnet publish src/Asuka.Windows -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=false -o $out }
# App-local CRT from Visual Studio's redistributable directory; no administrator setup.
$vswhere = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
if (-not (Test-Path $vswhere)) { throw 'Visual Studio C++ Build Tools are required to package the redistributable CRT.' }
$vs = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$crt = Get-Item "$vs/VC/Redist/MSVC/*/x64/Microsoft.VC143.CRT" | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $crt) { throw 'Microsoft.VC143.CRT redistribution files were not found.' }
Get-ChildItem "$($crt.FullName)/*.dll" | Copy-Item -Destination $out
# DSHOW camera needs no FFmpeg codecs. Do not distribute the unused FFmpeg plug-in.
Get-ChildItem $out -Recurse -Filter '*ffmpeg*.dll' | Remove-Item -Force
Copy-Item README.md "$out/LISEZ-MOI.md"
Copy-Item scripts/installer.ps1,scripts/desinstaller.ps1,scripts/installer.cmd,scripts/desinstaller.cmd -Destination $out
foreach ($file in @('FenetreAsuka.exe','OpenCvSharpExtern.dll','WebView2Loader.dll','vcruntime140.dll','models/face_detection_yunet_2023mar.onnx','models/face_recognition_sface_2021dec.onnx')) {
    if (-not (Test-Path "$out/$file")) { throw "Missing package file: $file" }
}
$zip = Join-Path $PWD 'artifacts/FenetreAsuka-win-x64.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$out/*" -DestinationPath $zip
(Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() + '  FenetreAsuka-win-x64.zip' | Set-Content "$zip.sha256" -Encoding ascii
Write-Host "ZIP ready: $zip"
