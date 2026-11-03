# Changelog

## v0.1.3 — 安裝精靈視窗拖曳修正（2026-11-03，Asia/Taipei）

- 維持 `WizardStyle=modern` 與系統原生標題列
- 以 `CreateCallback` 子類化精靈視窗：於客戶區頂部（約 72 DIP）收到滑鼠左鍵時發送 `SC_DRAGMOVE`，便於以滑鼠／觸控板點擊拖移安裝視窗
- Release：`WinTouchpad-Setup-v0.1.3.exe` + `WinTouchpad-win-x64-v0.1.3.zip`
## v0.1.2 — 拖曳跟手／延遲優化（2026-11-03，Asia/Taipei）

- 快取裝置 HID preparsed data 與排序後的 value caps，避免每次 `WM_INPUT` 重算
- 拖曳期間抑制每幀詳細日誌；其餘詳細日誌間隔改為 250ms；INFO 改為緩衝寫入（WARN／ERROR 仍立即寫入）
- 相對滑鼠移動使用 `MOUSEEVENTF_MOVE_NOCOALESCE`；重用 SendInput 緩衝區
- Raw Input 執行緒優先權 `AboveNormal`；拖曳期間進程優先權短暫提升（`DragProcessBoost`）
- 僅在剛新增接觸點／仍在取得接觸時套用 settle frame
- `ContactTracker` 重用列表；單元測試 20／20 通過
- Release：`WinTouchpad-Setup-v0.1.2.exe` + `WinTouchpad-win-x64-v0.1.2.zip`
- README 改為書面語撰寫

## v0.1.1 — Inno Setup 安裝檔（2026-11-03，Asia/Taipei）

- `installer/WinTouchpad.iss` 與 `scripts/publish-release.ps1`／`build-installer.ps1`
- Setup.exe：安裝至 `LocalAppData\Programs\WinTouchpad`、開始功能表、可選桌面捷徑／開機啟動／安裝後啟動
- 安裝精靈語言：en／zh-TW／zh-CN
- Release 產物：`WinTouchpad-Setup-v0.1.1.exe` + zip（延續 v0.1.0 崩潰修復：WASDK FDD + Bootstrap + `.xbf`）

## 多語系 UI + Release 打包（2026-11-03，Asia/Taipei）

- `Localization/Strings*.resx`：zh-TW（預設）、zh-CN、en／en-US／en-GB
- 依 Windows 顯示語言選擇介面；未知語系後備為繁體中文
- Self-contained win-x64 publish → `dist/WinTouchpad-win-x64-v0.1.0.zip`

## WinUI 3 設定／Tray 遷移（2026-11-03，Asia/Taipei）

- 新增 `WinTouchpad.App`（WinUI 3／Windows App SDK 1.7，unpackaged `WindowsPackageType=None`、self-contained）
- Settings：卡片式頁面、敏感度 Slider **0.2–2.0**（預設 **0.6**）、開關、路徑、儲存、開機自啟
- Tray：`H.NotifyIcon.WinUI`；Core `RawInputHost` 保留；拖曳邏輯仍在 Core
- `Directory.Build.targets`：在缺少 Visual Studio AppxPackage MSBuild 任務的環境 stub `GetMrtPackagingOutputs`／`CopyLocalFilesOutputGroup` 等，使 unpackaged 建置可通過
- `WinTouchpad.Tray`（WinForms）保留為 legacy；**請改執行 `WinTouchpad.App`**
- 建置：`dotnet build src\WinTouchpad.App -c Debug -p:Platform=x64`；Core 單元測試通過

## UI — 設定面板升級（2026-11-03）

- `SettingsForm`：分區 GroupBox（拖曳／啟動／關於・路徑）
- 視覺：淺灰底、白卡片、Microsoft JhengHei UI、間距與說明文字
- 行為不變：即時套用、儲存 + AutostartHelper、`Close()`、`SyncFrom`；預設敏感度仍為 0.6

摘要至 **M4**（2026-11-03，Asia/Taipei）。細節見 [plan.md](plan.md) §11、[testing.md](testing.md)。

## M4 — 文件收束

- README：建置 → 關閉系統三指 → 執行 → 設定／已知限制
- `testing.md`：M1–M3 清單；標示「已驗證（本專案對話）」與「用戶尚待確認」
- `architecture.md`／`setup-touchpad.md`／`settings-upgrade.md`／`licenses.md`／`oss-references.md` 交叉連結對齊
- `plan.md`：M4 完成；shared understanding 註記
- 本檔新增

## M3 — 設定窗／持久化／自啟

- `SettingsStore` + `settings.json`（`settingsVersion = 1`）
- WinForms 設定窗：啟用、敏感度、開始距離、開機自啟
- `AutostartHelper`（HKCU Run）
- **修復：** `Show()` 開窗時「關閉」改為 `Click → Close()`；窗高 310、按鈕 y=255，路徑提示不遮擋按鈕
- `docs/settings-upgrade.md`

## M2 — DragEngine + SendInput

- `MouseInjector`、`DragEngine`（Idle→Pending→Dragging→End）
- Pipeline 接三指；Tray 啟用開關
- 預設 **Sensitivity 0.6**（由 1.0 調低；用戶確認手感較接近系統）

## M1 — Raw Input 骨架

- Core／Tray 方案；PTP Raw Input；`contacts=3` 日誌
- FingerCountRouter（3／4 指分流預留）
- 單元測試起步

## M0 — Plan

- Grilling Round 1–5；正式 `plan.md`；用戶確認開始實作