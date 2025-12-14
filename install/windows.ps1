$ErrorActionPreference = "Stop"

$baseUrl = "https://github.com/rizwan3d/Ivory/releases/latest/download"
$installDir = Join-Path $env:LOCALAPPDATA "Ivory\bin"

function Get-IvoryArchiveName {
    $archRaw = $env:PROCESSOR_ARCHITEW6432
    if (-not $archRaw) { $archRaw = $env:PROCESSOR_ARCHITECTURE }
    $arch = ($archRaw).ToLowerInvariant()
    switch ($arch) {
        "amd64" { return "iv-win-x64.tar.gz" }
        "arm64" { return "iv-win-arm64.tar.gz" }
        default { throw "Unsupported architecture: $arch. Supported: AMD64, ARM64." }
    }
}

if (-not (Get-Command tar -ErrorAction SilentlyContinue)) {
    throw "tar is required to extract the Ivory archive. Install the Windows tar/bsdtar utility and try again."
}

$archiveName = Get-IvoryArchiveName
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ivory-install-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

$archivePath = Join-Path $tempDir $archiveName
$uri = "$baseUrl/$archiveName"
Write-Host "Downloading $uri..."
Invoke-WebRequest -Uri $uri -OutFile $archivePath

Write-Host "Extracting..."
tar -xzf $archivePath -C $tempDir

$binary = Get-ChildItem -Path $tempDir -Recurse -File | Where-Object { $_.Name -match '^iv.*(\.exe)?$' } | Select-Object -First 1
if (-not $binary) {
    throw "Unable to find Ivory binary in the archive."
}

$dest = Join-Path $installDir "iv.exe"
Copy-Item $binary.FullName $dest -Force

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not $userPath) { $userPath = "" }
$userParts = $userPath -split ';' | Where-Object { $_ }
if ($userParts -notcontains $installDir) {
    $updated = ($userParts + $installDir) -join ';'
    [Environment]::SetEnvironmentVariable("Path", $updated, "User")
    Write-Host "Added $installDir to your user PATH."
}

if (($env:Path -split ';') -notcontains $installDir) {
    $env:Path = "$installDir;$env:Path"
}

Remove-Item -Recurse -Force $tempDir
Write-Host "Ivory installed to $dest"
Write-Host "PATH updated for this session; open a new terminal to ensure it is picked up everywhere."
