# Publish a self-contained single-file screensaver + bundle the content next to it.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $PSScriptRoot 'dist'
Remove-Item -Recurse -Force $dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $dist | Out-Null

# 1) ensure the content bundle exists (scripts + assets).
# The content-generation tooling is not part of this (source-only) repo — see README.
# Generate <repo>/content from your own copy of the original game first.
if (-not (Test-Path (Join-Path $root 'content/scripts.json'))) {
    throw "No content bundle found at $root\content. Generate it from your own copy of the original game data before building (see README: 'Content & assets')."
}

# 2) publish the screensaver (self-contained single file)
dotnet publish (Join-Path $root 'src/JohnnyCastaway.ScreenSaver') `
    -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $dist 'publish')

# 3) a .scr is just the exe renamed
Copy-Item (Join-Path $dist 'publish/JohnnyCastaway.ScreenSaver.exe') (Join-Path $dist 'JohnnyCastaway.scr')

# 4) content bundle beside the .scr
Copy-Item -Recurse (Join-Path $root 'content') (Join-Path $dist 'content')

Write-Host "Built $dist\JohnnyCastaway.scr (+ content\). Size:" (Get-Item (Join-Path $dist 'JohnnyCastaway.scr')).Length
