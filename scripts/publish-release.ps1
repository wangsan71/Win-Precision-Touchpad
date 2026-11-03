#Requires -Version 5.1
<#
.SYNOPSIS
  Publish WinTouchpad.App (win-x64) with the crash-safe layout:
  SelfContained=.NET runtime + WindowsAppSDKSelfContained=false (Bootstrap) + WinUI .xbf.
#>
param(
  [string]$Configuration = "Release",
  [string]$Output = ""
)
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not $Output) {
  $Output = Join-Path $Root "artifacts\publish\win-x64"
}
$Appx = "C:\BuildTools\MSBuild\Microsoft\VisualStudio\v17.0\AppxPackage\"
$proj = Join-Path $Root "src\WinTouchpad.App\WinTouchpad.App.csproj"

Write-Host "Publishing to $Output ..."
if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Output | Out-Null

$args = @(
  "publish", $proj,
  "-c", $Configuration,
  "-p:Platform=x64",
  "-p:RuntimeIdentifier=win-x64",
  "-p:WindowsPackageType=None",
  "-p:WindowsAppSDKSelfContained=false",
  "-p:SelfContained=true",
  "-p:PublishReadyToRun=false",
  "-o", $Output
)
if (Test-Path $Appx) {
  $args += "-p:AppxMSBuildToolsPath=$Appx"
}

& dotnet @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Sanity: no conflicting CoreMessagingXP beside Bootstrap-using build
$bad = Get-ChildItem $Output -Filter "CoreMessagingXP.dll" -Recurse -ErrorAction SilentlyContinue
if ($bad) {
  Write-Warning "Found CoreMessagingXP.dll (may conflict with Bootstrap): $($bad.FullName -join ', ')"
}
$xbf = Get-ChildItem $Output -Filter "*.xbf"
if (-not $xbf) { throw "No .xbf in publish output — WinUI will fail at runtime." }
$exe = Join-Path $Output "WinTouchpad.App.exe"
if (-not (Test-Path $exe)) { throw "Missing WinTouchpad.App.exe" }

Write-Host "Publish OK. xbf=$($xbf.Count) exe=$exe"
