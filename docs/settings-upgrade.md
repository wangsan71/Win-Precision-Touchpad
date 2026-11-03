# 設定檔升級說明

相關：[architecture.md](architecture.md)｜[changelog.md](changelog.md)｜[testing.md](testing.md)

## 位置

`%LocalAppData%\WinPrecisionTouchpad\settings.json`

（同日誌目錄。）

## 版本欄

JSON 頂層欄位 **`settingsVersion`**（整數）。

| 版本 | 說明 |
|------|------|
| **1** | 初版（M3）：`dragEnabled`、`sensitivity`、`startDistanceThreshold`、`startWithWindows` |

載入時 `SettingsStore.Migrate` 會將舊／缺版本升到目前 schema；儲存時永遠寫入 `AppSettings.CurrentVersion`。

## 預設值（無檔或損毀時）

| 欄位 | 預設 |
|------|------|
| dragEnabled | `true` |
| sensitivity | `0.6` |
| startDistanceThreshold | `12` |
| startWithWindows | `false` |

敏感度會 clamp 到 **0.2–2.0**；開始距離 **0–200**。

## 日後加欄位

1. 喺 `AppSettings` 加屬性（帶合理預設）
2. `CurrentVersion++`
3. 喺 `Migrate` 對 `settingsVersion < N` 填新欄／轉換
4. 更新本表同 [changelog.md](changelog.md)

舊檔唔使人手改；第一次開新版 App 會自動 migrate 再儲存。
