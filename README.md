# Win Precision Touchpad

Windows Precision Touchpad **三指拖曳**（macOS Three Finger Drag 風格）原型。

目前進度：**M4 完成** + **WinUI 3 設定窗** + **多語系 UI**。

## 下載（Release zip）

預先建置的 unpackaged、**self-contained**（win-x64）套件：

- 本機產物：`dist/WinTouchpad-win-x64-v0.1.0.zip`
- GitHub Releases（push 後）：見倉庫 Releases 頁

### 執行 zip

1. 解壓到任意資料夾（例如 `C:\Apps\WinTouchpad\`）
2. 先到 Windows「設定 → 藍牙與裝置 → 觸控板 → 三指手勢」設為 **無**（本 App **不會**自動改）
3. 執行 `WinTouchpad.App.exe`
4. 系統匣圖示 → 「設定…」／右鍵選單

> Self-contained 已內嵌 .NET 8 + Windows App SDK 執行階段，一般 **唔使**再裝 Visual Studio／單獨 Runtime。若改用 framework-dependent 建置，需先安裝 [.NET 8 Desktop](https://dotnet.microsoft.com/download/dotnet/8.0) 同 [Windows App Runtime 1.7](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)。

## 語言／Languages

UI 會跟 **Windows 顯示語言**（`CurrentUICulture`）：

| Culture | UI |
|---------|----|
| `zh-TW` / `zh-HK` / `zh-Hant`（預設後備） | 繁體中文 |
| `zh-CN` / `zh-Hans` | 简体中文 |
| `en-US` / `en-GB` / 其他 `en*` | English |

資源檔：`src/WinTouchpad.App/Localization/Strings*.resx`（設定窗、日誌窗標題、系統匣選單）。

## 快速開始（開發）

1. **建置**（需要 .NET 8 Windows Desktop）

```powershell
cd C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad
dotnet build src\WinTouchpad.sln -c Debug
dotnet test src\WinTouchpad.sln -c Debug
```

2. **系統三指設為「無」**（必做）  
   見 [docs/setup-touchpad.md](docs/setup-touchpad.md)。

3. **執行（WinUI 3）**

```powershell
dotnet run --project src\WinTouchpad.App -c Debug -p:Platform=x64
```

或：`src\WinTouchpad.App\bin\Debug\net8.0-windows10.0.19041.0\WinTouchpad.App.exe`

> **舊版 WinForms Tray（legacy）**：`dotnet run --project src\WinTouchpad.Tray -c Debug`

4. **系統匣**
   - 「設定…」／雙擊 → 開關拖曳、敏感度（預設 **0.6**）、開始距離、開機自啟
   - 「啟用三指拖曳」、日誌、結束
   - 設定窗「關閉」會真正關窗（唔使結束 Tray）

5. **設定檔**  
   `%LocalAppData%\WinPrecisionTouchpad\settings.json`  
   自啟：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\WinPrecisionTouchpad`

### Publish（維護者）

```powershell
dotnet publish src\WinTouchpad.App -c Release -p:Platform=x64 `
  -p:RuntimeIdentifier=win-x64 -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=true -p:SelfContained=true `
  -o artifacts\publish\win-x64
```

## 已知限制

- 要先手動關系統三指，否則系統會搶手勢
- 拖管理員視窗可能要 elevation
- 敏感度係 App 倍率，唔直接跟系統游標速度
- v1 唔處理四指（只預留分流）

## 文件

- [docs/plan.md](docs/plan.md) — v1 正式 plan／里程碑
- [docs/architecture.md](docs/architecture.md) — 模組、狀態機、資料流
- [docs/setup-touchpad.md](docs/setup-touchpad.md) — **手動**將系統三指設為「無」
- [docs/testing.md](docs/testing.md) — 建置／單測／人手驗證
- [docs/settings-upgrade.md](docs/settings-upgrade.md) — 設定檔版本同升級
- [docs/changelog.md](docs/changelog.md) — 變更摘要
- [docs/oss-references.md](docs/oss-references.md) — 開源參考
- [docs/licenses.md](docs/licenses.md) — 第三方授權聲明
- [LICENSE](LICENSE) — MIT

## 目錄

```
src/WinTouchpad.sln
src/WinTouchpad.Core/     # Raw Input、DragEngine、SettingsStore…
src/WinTouchpad.App/      # WinUI 3 系統匣 + Settings（主路徑）+ Localization/
src/WinTouchpad.Tray/     # 舊 WinForms Tray（legacy）
tests/WinTouchpad.Core.Tests/
docs/
dist/                     # Release zip（gitignore）
```

## 主副本

- **主：** WANGSAN-LP → `C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad`
- **備：** DESKTOP-5T8JV9O → `C:\Users\asus\Desktop\Win-Precision-Touchpad`（由協調端同步）
