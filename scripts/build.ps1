#Requires -Version 5.1
param(
  [string]$Configuration = "Debug",
  [string]$Platform = "x64"
)
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Appx = "C:\BuildTools\MSBuild\Microsoft\VisualStudio\v17.0\AppxPackage\"
Push-Location (Join-Path $Root "src")
try {
  dotnet build WinTouchpad.sln -c $Configuration -p:Platform=$Platform `
    "-p:AppxMSBuildToolsPath=$Appx"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  Write-Host "OK. Run WinUI host:"
  Write-Host "  dotnet run --project WinTouchpad.App -c $Configuration -p:Platform=$Platform `"-p:AppxMSBuildToolsPath=$Appx`""
  Write-Host "Legacy WinForms tray still available: WinTouchpad.Tray"
}
finally { Pop-Location }
