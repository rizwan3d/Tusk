$ErrorActionPreference = "Stop"

$installDir = Join-Path $env:LOCALAPPDATA "Ivory\bin"
$binary = Join-Path $installDir "iv.exe"

Write-Host "Removing Ivory binary from $installDir..."
Remove-Item -ErrorAction SilentlyContinue -Force $binary

if (Test-Path $installDir) {
    $entries = Get-ChildItem -Path $installDir
    if ($entries.Count -eq 0) {
        Remove-Item -Force -Recurse $installDir
    }
}

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not $userPath) { $userPath = "" }
$parts = $userPath -split ';' | Where-Object { $_ -and ($_ -ne $installDir) }
$updated = ($parts -join ';')
[Environment]::SetEnvironmentVariable("Path", $updated, "User")

Write-Host "Removed $installDir from user PATH."
Write-Host "Ivory uninstalled. Open a new terminal to ensure PATH updates take effect."
