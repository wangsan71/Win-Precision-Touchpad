# 架構（M4）

對齊實作至 M3 功能碼 + M4 文件收束。相關：[plan.md](plan.md)｜[testing.md](testing.md)｜[settings-upgrade.md](settings-upgrade.md)｜[changelog.md](changelog.md)

## 模組

| 專案 | 職責 |
|------|------|
| `WinTouchpad.Core` | Raw Input、ContactTracker、FingerCountRouter、DragEngine、MouseInjector、Pipeline、**SettingsStore**、日誌 |
| `WinTouchpad.Tray` | 系統匣、**設定窗**、**HKCU Run 自啟**、日誌視窗、隱藏 `NativeWindow` 收 `WM_INPUT` |
| `WinTouchpad.Core.Tests` | Router／Tracker／DragEngine／**SettingsStore** 單測 |

## 3／4 指分流

`FingerCountRouter` 按同時指數分流：

- `3` → `ThreeFinger` → **DragEngine**（模擬左鍵拖曳）
- `4` → `FourFingerReserved`（只 log，唔執行動作；日後快捷鍵掛呢度）
- `0`／`1–2`／`5+` → 忽略

## 資料流

```
Precision Touchpad HID
        │
        ▼
RegisterRawInputDevices (0x0D / 0x05, RIDEV_INPUTSINK|DEVICENOTIFY)
        │
        ▼
NativeWindow.WndProc  ← WM_INPUT
        │
        ▼
TouchpadRawInput.ParseInput  →  ContactFrame
        │
        ▼
TouchpadPipeline
   ├─ FingerCountRouter
   ├─ ContactTracker (dx/dy)
   ├─ DragEngine (僅 ThreeFinger)
   │     Idle → Pending → Dragging → End → Idle
   │     └─ IMouseInjector / MouseInjector (SendInput)
   └─ AppLogger → UI + %LocalAppData%\WinPrecisionTouchpad\

啟動時：
SettingsStore.Load → Pipeline.ApplySettings → 再註冊 Raw Input

設定窗／Tray 開關：
AppSettings → ApplySettings（即時）→ Save → settings.json
自啟勾選儲存時：AutostartHelper → HKCU\...\Run\WinPrecisionTouchpad
```

## Settings

| 項 | 說明 |
|----|------|
| 檔案 | `%LocalAppData%\WinPrecisionTouchpad\settings.json` |
| 版本欄 | `settingsVersion`（而家 = **1**）；見 [settings-upgrade.md](settings-upgrade.md) |
| 欄位 | `dragEnabled`、`sensitivity`（預設 **0.6**）、`startDistanceThreshold`（預設 **12**）、`startWithWindows` |

敏感度係 **App 對 HID delta 嘅倍率**，唔直接讀系統觸控板游標速度；`SendInput` 仍可能受系統指標加速輕微影響。

## 設定窗實作備註（M3 修復）

- 設定窗以 **`Show()`**（非 `ShowDialog`）開啟，故「關閉」**唔可以**只靠 `DialogResult`。
- 正確做法：`closeButton.Click += (_, _) => Close();`
- 布局：`ClientSize` 高 **310**；儲存／關閉掣約 **y=255**；路徑提示 label 喺 **y=208**、高度限制，避免蓋住掣。

## 開機自啟

- **機制：** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 機碼值 `WinPrecisionTouchpad`
- **權限：** 只寫目前使用者，**唔需要**管理員
- **唔會**改觸控板手勢相關註冊表（系統三指仍要手動設「無」；見 [setup-touchpad.md](setup-touchpad.md)）

## DragEngine 狀態機

| 狀態 | 含義 |
|------|------|
| `Idle` | 等待三指 |
| `Pending` | 已有三指，累積移動距離；未達閾值唔撳左鍵 |
| `Dragging` | 已 `LeftDown`；平均 contact delta × Sensitivity |
| `End` | 過渡；若正在拖會 `LeftUp`，然後返 `Idle` |

## Raw Input

- Usage Page `0x000D`／Usage `0x0005`；Flags `RIDEV_INPUTSINK` \| `RIDEV_DEVICENOTIFY`
- 參考（MIT）：[RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad)、[ThreeFingerDragOnWindows](https://github.com/ClementGre/ThreeFingerDragOnWindows)
- 授權摘要：[licenses.md](licenses.md)；對照清單：[oss-references.md](oss-references.md)

## 點解要關系統三指

Windows 預設會食咗三指手勢。請手動設「無」（[setup-touchpad.md](setup-touchpad.md)）。**本專案唔會自動改手勢註冊表。**

## 已知限制

- 管理員視窗可能要 elevation 先拖到
- 各廠 PTP HID 報告可能唔同；敏感度靠滑桿微調
- 自啟寫入當下 exe 路徑；換 Build 輸出目錄後要再儲存自啟
