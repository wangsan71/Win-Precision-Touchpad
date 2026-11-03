# Installer (Inno Setup)

## Prerequisites
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`ISCC.exe` under `C:\Program Files (x86)\Inno Setup 6\`)
- .NET 8 SDK

## Build Setup.exe

From repo root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -MakeZip
```

- Publishes crash-safe layout: **SelfContained=.NET** + **WindowsAppSDKSelfContained=false** (Bootstrap) + WinUI `.xbf`
- Compiles `installer\WinTouchpad.iss`
- Outputs:
  - `dist\WinTouchpad-Setup-v0.1.1.exe`
  - `dist\WinTouchpad-win-x64-v0.1.1.zip` (with `-MakeZip`)

Skip republish: `-SkipPublish`

## Installer features
- Default dir: `%LocalAppData%\Programs\WinTouchpad` (no admin)
- Start Menu shortcut; optional Desktop shortcut
- Optional **Start with Windows** → `HKCU\...\Run\WinPrecisionTouchpad` (same as `AutostartHelper`)
- Optional launch after install
- Wizard languages: English / 繁體中文 / 简体中文
- Uninstaller

## Silent install / uninstall (smoke)

```powershell
dist\WinTouchpad-Setup-v0.1.1.exe /VERYSILENT /NORESTART /SUPPRESSMSGBOXES /DIR="%TEMP%\WinTouchpad-smoke"
# uninstall:
& "$env:LOCALAPPDATA\Programs\WinTouchpad\unins000.exe" /VERYSILENT  # if default dir
# or:
Get-ChildItem "$env:TEMP\WinTouchpad-smoke" -Filter unins*.exe | % { & $_.FullName /VERYSILENT }
```
