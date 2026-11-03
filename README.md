# Win Precision Touchpad

Windows Precision Touchpad **三指拖曳**（macOS Three Finger Drag 風格）原型。

目前進度：**M4 完成**、**WinUI 3 設定視窗**、**多語系 UI**、**v0.1.2 跟手／延遲優化**。

## 下載（Release）

預先建置的 unpackaged、**.NET 8 self-contained**（win-x64）與 **Windows App SDK framework-dependent**（Bootstrap）發行包：

| 產物 | 說明 |
|------|------|
| `dist/WinTouchpad-Setup-v0.1.2.exe` | **Inno Setup 安裝程式**（建議） |
| `dist/WinTouchpad-win-x64-v0.1.2.zip` | 便攜解壓版 |

GitHub Releases：https://github.com/wangsan71/Win-Precision-Touchpad/releases

### 安裝（Setup.exe）

1. 下載並執行 `WinTouchpad-Setup-v0.1.2.exe`（可選擇開機啟動／桌面捷徑）。
2. 預設安裝路徑為 `%LocalAppData%\Programs\WinTouchpad`（無需系統管理員權限）。
3. 請至 Windows「設定 → 藍牙與裝置 → 觸控板 → 三指手勢」設為 **無**。
4. 開啟系統匣圖示 →「設定…」。

維護者建置安裝檔：`powershell -File scripts\build-installer.ps1 -Version 0.1.2 -MakeZip`（需 [Inno Setup 6](https://jrsoftware.org/isinfo.php)）。

### 執行 zip

1. 解壓至任意資料夾（例如 `C:\Apps\WinTouchpad\`）。
2. 先將 Windows「設定 → 藍牙與裝置 → 觸控板 → 三指手勢」設為 **無**（本應用程式**不會**自動變更系統設定）。
3. 執行 `WinTouchpad.App.exe`。
4. 系統匣圖示 →「設定…」／右鍵選單。

> Release zip 內嵌 **.NET 8**（SelfContained）；Windows App SDK 採 **framework-dependent**：需已安裝 [Windows App Runtime 1.7](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)（Windows 11 多數環境已具備）。解壓後可直接執行 `WinTouchpad.App.exe`，無需安裝 Visual Studio。

## Languages／語言

介面依 **Windows 顯示語言**（`CurrentUICulture`）自動選擇：

| Culture | UI |
|---------|----|
| `zh-TW`／`zh-HK`／`zh-Hant`（預設備援） | 繁體中文 |
| `zh-CN`／`zh-Hans` | 简体中文 |
| `en-US`／`en-GB`／其他 `en*` | English |

資源檔位於 `src/WinTouchpad.App/Localization/Strings*.resx`（設定視窗、日誌視窗標題、系統匣選單）。

## 快速開始（開發）

1. **建置**（需要 .NET 8 Windows Desktop）

```powershell
cd C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad
dotnet build src\WinTouchpad.sln -c Debug
dotnet test src\WinTouchpad.sln -c Debug
```

2. **將系統三指手勢設為「無」**（必要步驟）  
   請參閱 [docs/setup-touchpad.md](docs/setup-touchpad.md)。

3. **執行（WinUI 3）**

```powershell
dotnet run --project src\WinTouchpad.App -c Debug -p:Platform=x64
```

或執行：`src\WinTouchpad.App\bin\Debug\net8.0-windows10.0.19041.0\WinTouchpad.App.exe`

> **舊版 WinForms Tray（legacy）**：`dotnet run --project src\WinTouchpad.Tray -c Debug`

4. **系統匣**
   - 「設定…」／雙擊 → 開關拖曳、敏感度（預設 **0.6**）、開始距離、開機自啟
   - 「啟用三指拖曳」、日誌、結束
   - 設定視窗「關閉」僅關閉視窗（不必結束 Tray）

5. **設定檔**  
   `%LocalAppData%\WinPrecisionTouchpad\settings.json`  
   開機自啟：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\WinPrecisionTouchpad`

### Publish（維護者）

```powershell
powershell -File scripts\publish-release.ps1
# 或：
dotnet publish src\WinTouchpad.App -c Release -p:Platform=x64 `
  -p:RuntimeIdentifier=win-x64 -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=false -p:SelfContained=true `
  -o artifacts\publish\win-x64
```

## 已知限制

- 須先手動關閉系統三指手勢，否則系統會攔截手勢
- 拖曳以系統管理員權限執行的視窗時，可能需要 elevation
- 敏感度為應用程式倍率，不會直接跟隨系統游標速度
- v1 不處理四指手勢（僅預留分流）

## 文件

- [docs/plan.md](docs/plan.md) — v1 正式計畫／里程碑
- [docs/architecture.md](docs/architecture.md) — 模組、狀態機、資料流
- [docs/setup-touchpad.md](docs/setup-touchpad.md) — **手動**將系統三指設為「無」
- [docs/testing.md](docs/testing.md) — 建置／單元測試／手動驗證
- [docs/settings-upgrade.md](docs/settings-upgrade.md) — 設定檔版本與升級
- [docs/changelog.md](docs/changelog.md) — 變更摘要
- [docs/oss-references.md](docs/oss-references.md) — 開源參考
- [docs/licenses.md](docs/licenses.md) — 第三方授權聲明
- [LICENSE](LICENSE) — MIT

## 目錄結構

```
src/WinTouchpad.sln
src/WinTouchpad.Core/     # Raw Input、DragEngine、SettingsStore…
src/WinTouchpad.App/      # WinUI 3 系統匣 + Settings（主路徑）+ Localization/
src/WinTouchpad.Tray/     # 舊版 WinForms Tray（legacy）
tests/WinTouchpad.Core.Tests/
docs/
dist/                     # Release 產物（gitignore）
installer/                # Inno Setup 腳本
scripts/                  # publish／installer 腳本
```

## 主副本

- **主：** WANGSAN-LP → `C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad`
- **備：** DESKTOP-5T8JV9O → `C:\Users\asus\Desktop\Win-Precision-Touchpad`（由協調端同步）