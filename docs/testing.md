# 測試清單（M4）

主副本路徑：`C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad`（WANGSAN-LP）。

狀態標記：

- **已驗證（本專案對話）** — 用戶或開發過程已確認過
- **用戶仲要確認** — 建議再做一次／未喺對話明確回報
- **自動化** — `dotnet test` 覆蓋

## 自動化

```powershell
cd C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad\src
dotnet build WinTouchpad.sln -c Debug
dotnet test WinTouchpad.sln -c Debug
```

預期：全部通過（含 `FingerCountRouter`／`ContactTracker`／`DragEngineTests`／`SettingsStoreTests`）。

| 項 | 狀態 |
|----|------|
| `dotnet build` / `dotnet test`（開發機多次綠） | **已驗證（開發）** — 文件更新後若未改碼可跳過；改碼後必跑 |

## 前置（所有人手項）

1. 跟 [setup-touchpad.md](setup-touchpad.md) 將 **三指手勢全部設為「無」**。
2. 建議關閉「點兩下並拖動以選取多項目」。
3. 若有舊 Tray process，先結束再：

```powershell
cd C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad\src
dotnet run --project WinTouchpad.Tray -c Debug
```

## M1 — Raw Input／日誌

| # | 步驟 | 預期 | 狀態 |
|---|------|------|------|
| M1-1 | 開 Tray；系統匣出現 | 常駐、可開日誌窗 | **已驗證（本專案對話）** |
| M1-2 | 三指喺觸控板移動 | 日誌穩定 `contacts=3` 同 x／y | **已驗證（本專案對話）**（用戶：log 正常） |
| M1-3 | 一／兩指 | 唔當三指拖；可有低指數 log | **用戶仲要確認**（非阻塞） |
| M1-4 | 四指 | `FourFingerReserved`（或等價 log），唔拖 | **用戶仲要確認** |

## M2 — 三指拖曳（SendInput）

| # | 步驟 | 預期 | 狀態 |
|---|------|------|------|
| M2-1 | 三指拖視窗 | 游標下視窗跟住郁；抬指結束 | **已驗證（本專案對話）**（用戶：成功拖動） |
| M2-2 | 日誌出現 `Dragging`／結束 | 狀態機可見 | **已驗證（開發＋對話）** |
| M2-3 | 選字／拖檔 | 左鍵拖語意生效 | **用戶仲要確認**（對話主要確認拖窗） |
| M2-4 | 預設敏感度 0.6 | 手感接近系統速度 | **已驗證（本專案對話）**（用戶：比較接近，繼續下一步） |
| M2-5 | Tray「啟用三指拖曳」關 | 唔再拖 | **用戶仲要確認**（M3 開關亦覆蓋） |

## M3 — 設定窗／持久化／自啟

| # | 步驟 | 預期 | 狀態 |
|---|------|------|------|
| M3-1 | 系統匣 Tooltip 含進度標（如 `M3`）同 `[ON]`／`[OFF]` | 通過 | **用戶仲要確認**（非阻塞） |
| M3-2 | 開設定面板 | 窗可開；有啟用／敏感度／開始距離／自啟 | **已驗證（本專案對話）** |
| M3-3 | 「關閉」掣 | 真正關閉設定窗（`Show()` + `Click→Close()`；路徑提示唔蓋掣） | **已驗證（本專案對話）**（用戶：有效了） |
| M3-4 | 改敏感度滑桿，即時試拖 | 手感跟住變；未按儲存亦即時生效 | **用戶仲要確認** |
| M3-5 | 按「儲存」；重開 App | `%LocalAppData%\WinPrecisionTouchpad\settings.json` 保留設定 | **用戶仲要確認** |
| M3-6 | 勾「開機時自動啟動」→ 儲存 | `HKCU\...\Run\WinPrecisionTouchpad` 出現；指向目前 exe | **用戶仲要確認** |
| M3-7 | 取消自啟 → 儲存 | Run 機碼值消失 | **用戶仲要確認** |
| M3-8 | Tray「啟用三指拖曳」開關 | 即時生效並寫入 settings（若有儲存路徑） | **用戶仲要確認** |
| M3-9 | 系統三指手勢 | **仍然**只靠人手設「無」；App **唔**改手勢註冊表 | **已驗證（設計／程式約束）** |

## 已知限制

- 管理員視窗可能要 elevation。
- 自啟寫入嘅係 **Debug／Release 當下 exe 路徑**；換輸出目錄後要再儲存一次自啟。
- 各廠 PTP 敏感度靠設定滑桿微調（預設 0.6；唔跟系統觸控板游標速度）。
- `SendInput` 仍可能受 Windows 滑鼠指標加速輕微影響。

## 相關文件

- [architecture.md](architecture.md) — 模組同資料流
- [setup-touchpad.md](setup-touchpad.md) — 手動關系統三指
- [settings-upgrade.md](settings-upgrade.md) — 設定檔版本
- [changelog.md](changelog.md) — M1–M4 摘要
- [plan.md](plan.md) — 正式 plan／里程碑
