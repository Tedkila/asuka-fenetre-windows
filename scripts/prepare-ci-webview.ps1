$ErrorActionPreference = 'Stop'
$existing = Get-ChildItem "${env:ProgramFiles(x86)}/Microsoft/EdgeWebView/Application/*/msedgewebview2.exe","$env:LOCALAPPDATA/Microsoft/EdgeWebView/Application/*/msedgewebview2.exe" -ErrorAction SilentlyContinue
if ($existing) { Write-Host 'WebView2 Runtime present.'; exit 0 }
New-Item .tools -ItemType Directory -Force | Out-Null
$installer = Join-Path $PWD '.tools/MicrosoftEdgeWebview2Setup.exe'
Invoke-WebRequest 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $installer
$signature = Get-AuthenticodeSignature $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'Microsoft installer signature verification failed.' }
$process = Start-Process $installer -ArgumentList '/silent','/install' -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "WebView2 installation failed: $($process.ExitCode)" }
