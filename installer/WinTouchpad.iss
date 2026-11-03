; Win Precision Touchpad — Inno Setup 6 script
; Build: ISCC.exe installer\WinTouchpad.iss
; Expects publish output at artifacts\publish\win-x64 (self-contained .NET + WASDK framework-dependent)

#define MyAppName "Win Precision Touchpad"
#define MyAppNameShort "WinTouchpad"
#define MyAppVersion "0.1.1"
#define MyAppPublisher "wangsan71"
#define MyAppURL "https://github.com/wangsan71/Win-Precision-Touchpad"
#define MyAppExeName "WinTouchpad.App.exe"
#define MyAppRegValue "WinPrecisionTouchpad"
#define PublishDir "..\artifacts\publish\win-x64"

[Setup]
AppId={{A7C3E9F1-4B2D-4E8A-9C1F-6D5B8A0E2F33}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\{#MyAppNameShort}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
InfoBeforeFile=
OutputDir=..\dist
OutputBaseFilename=WinTouchpad-Setup-v{#MyAppVersion}
SetupIconFile=..\src\WinTouchpad.App\Assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesetraditional"; MessagesFile: "Languages\ChineseTraditional.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start with Windows / 開機時啟動 / 开机时启动"; GroupDescription: "Startup / 開機啟動:"; Flags: unchecked
Name: "launch"; Description: "Launch after install / 安裝後啟動 / 安装后启动"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
; Matches AutostartHelper: HKCU\...\Run\WinPrecisionTouchpad
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppRegValue}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent; Tasks: launch

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
