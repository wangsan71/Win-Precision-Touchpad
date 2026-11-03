#Requires -Version 5.1
<#
.SYNOPSIS
  Publish + build Inno Setup Setup.exe (and optional zip) for WinTouchpad.
#>
param(
  [string]$Version = "0.1.1",
  [switch]$SkipPublish,
  [switch]$MakeZip
)
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $Iscc)) { throw "Inno Setup 6 ISCC.exe not found. Install from https://jrsoftware.org/isinfo.php" }

if (-not $SkipPublish) {
  & (Join-Path $PSScriptRoot "publish-release.ps1")
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$iss = Join-Path $Root "installer\WinTouchpad.iss"
$publish = Join-Path $Root "artifacts\publish\win-x64"
if (-not (Test-Path (Join-Path $publish "WinTouchpad.App.exe"))) {
  throw "Publish output missing: $publish"
}

New-Item -ItemType Directory -Force -Path (Join-Path $Root "dist") | Out-Null
Write-Host "Compiling Inno Setup..."
& $Iscc $iss
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Join-Path $Root "dist\WinTouchpad-Setup-v$Version.exe"
if (-not (Test-Path $setup)) {
  # ISCC uses version from script; allow mismatch discovery
  $setup = Get-ChildItem (Join-Path $Root "dist") -Filter "WinTouchpad-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
Write-Host "Setup: $setup  ($([math]::Round((Get-Item $setup).Length/1MB,1)) MB)"

if ($MakeZip) {
  $zipPath = Join-Path $Root "dist\WinTouchpad-win-x64-v$Version.zip"
  if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
  Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $zipPath -CompressionLevel Optimal
  Write-Host "Zip: $zipPath  ($([math]::Round((Get-Item $zip).Length/1MB,1)) MB)"
}

Write-Host "Done."
