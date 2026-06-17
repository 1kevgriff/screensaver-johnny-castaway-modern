$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'JohnnyCastaway'
$cur = (Get-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue).'SCRNSAVE.EXE'
if ($cur -and $cur.StartsWith($target + [IO.Path]::DirectorySeparatorChar)) {
    Remove-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue
    Write-Host "Cleared SCRNSAVE.EXE registration."
}
Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue
Write-Host "Removed $target."
