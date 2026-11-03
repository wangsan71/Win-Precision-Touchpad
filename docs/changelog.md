## 多語系 UI + Release 打包（2026-11-03，Asia/Taipei）
- Localization/Strings*.resx：zh-TW（預設）、zh-CN、en / en-US / en-GB
- 跟 Windows 顯示語言；未知語系後備繁中
- Self-contained win-x64 publish → `dist/WinTouchpad-win-x64-v0.1.0.zip`
# Changelog

## WinUI 3 設定／Tray 遷移（2026-11-03，Asia/Taipei）
- 新增 `WinTouchpad.App`（WinUI 3 / Windows App SDK 1.7，unpackaged `WindowsPackageType=None`、self-contained）
- Settings：卡片式頁面、敏感度 Slider **0.2–2.0**（預設 **0.6**）、開關、路徑、儲存、開機自啟
- Tray：`H.NotifyIcon.WinUI`；Core `RawInputHost` 保留；拖曳邏輯仍在 Core
- `Directory.Build.targets`：喺無 VS AppxPackage MSBuild 任務嘅機器 stub `GetMrtPackagingOutputs`／`CopyLocalFilesOutputGroup` 等，令 unpackaged 建置可綠
- `WinTouchpad.Tray`（WinForms）保留作 legacy；**請改跑 `WinTouchpad.App`**
- 建置：`dotnet build src\WinTouchpad.App -c Debug -p:Platform=x64`；Core 單測 19 通過
## UI — 設定面板升級（2026-11-03）

- `SettingsForm`：分區 GroupBox（拖曳／啟動／關於・路徑）
- 視覺：淺灰底、白卡片、Microsoft JhengHei UI、間距與說明文字（zh-HK）
- 行為不變：即時套用、儲存 + AutostartHelper、`Close()`、`SyncFrom`；預設敏感度仍 0.6
摘要至 **M4**（2026-11-03，Asia/Taipei）。細節見 [plan.md](plan.md) §11、[testing.md](testing.md)。

## M4 — 文件收束

- README：建置 → 關系統三指 → 執行 → 設定／已知限制
- `testing.md`：M1–M3 清單；標示「已驗證（本專案對話）」vs「用戶仲要確認」
- `architecture.md`／`setup-touchpad.md`／`settings-upgrade.md`／`licenses.md`／`oss-references.md` 交叉連結對齊
- `plan.md`：M4 完成；shared understanding 註記
- 本檔新增

## M3 — 設定窗／持久化／自啟

- `SettingsStore` + `settings.json`（`settingsVersion = 1`）
- WinForms 設定窗：啟用、敏感度、開始距離、開機自啟
- `AutostartHelper`（HKCU Run）
- **修復：** `Show()` 開窗時「關閉」改 `Click → Close()`；窗高 310、掣 y=255，路徑提示唔蓋掣
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

