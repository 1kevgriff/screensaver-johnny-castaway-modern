$ErrorActionPreference = 'Stop'
$dist = Join-Path $PSScriptRoot 'dist'
$scr = Join-Path $dist 'JohnnyCastaway.scr'
if (-not (Test-Path $scr)) { throw "Run build.ps1 first ($scr not found)." }

$target = Join-Path $env:LOCALAPPDATA 'JohnnyCastaway'
New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item $scr (Join-Path $target 'JohnnyCastaway.scr') -Force
Copy-Item -Recurse -Force (Join-Path $dist 'content') (Join-Path $target 'content')

$installed = Join-Path $target 'JohnnyCastaway.scr'
Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -Value $installed
Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value '1'
Write-Host "Installed to $installed and set as the active screen saver."
Write-Host "Open Settings > Lock screen > Screen saver to preview, or run: `"$installed`" /s"
