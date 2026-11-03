# Win Precision Touchpad — v1 正式 Plan

> Grilling 設計樹已收束（Round 1–5）。**Plan 已確認。** M1–M4 已完成；仍然**唔自動改**系統設定。

## 1. 目標（v1）

喺 Windows Precision Touchpad 上，提供接近 **macOS Three Finger Drag** 嘅體驗：

- 三指喺觸控板上移動 → **模擬滑鼠左鍵按下並拖曳**（可移窗、選字、拖檔）
- 拖曳對象語意：開始拖曳嗰下，作用於 **游標下嘅內容／視窗**（似 Mac；底層係系統對「左鍵拖曳」嘅反應）
- 交付：**可用原型** — C# **WinForms** 系統匣常駐 + **簡易設定視窗** + 可開機自啟
- 架構預留：**3 指／4 指訊息分開處理**；v1 **只實作 3 指**；4 指快捷鍵之後再接

## 2. 非目標（v1 不做）

- 唔自動改註冊表／系統三指手勢（只提供 **設定 App 手動步驟**）
- 唔做四指手勢／四指快捷鍵（只留訊息分流接口）
- 唔做「只搬視窗座標、唔模擬滑鼠鍵」模式（除非第二期加）
- 唔以 PowerToys 做主方案
- 唔直接 fork WinUI 版主參考做發行分叉（見 §5）

## 3. 已定決策摘要

| 項 | 決定 |
|----|------|
| 產品形態 | 完全自訂手勢管線（偵測 → 動作） |
| v1 手勢 | 只做三指；拖曳＝模擬左鍵拖 |
| 方向語意 | 連續拖曳跟住手指（唔係四向滑一下觸發快捷鍵） |
| UI | WinForms 系統匣 + 簡易設定窗；可開機自啟 |
| 關系統三指 | 只文件教手動設為「無」 |
| 品質要求 | **易維護、易升級**；要有 **詳細技術文檔** |
| 主參考 | [ClementGre/ThreeFingerDragOnWindows](https://github.com/ClementGre/ThreeFingerDragOnWindows)（MIT）+ [RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad) |
| 專案路徑 | `C:\Users\asus\Desktop\APP\_PersonalProject\Win-Precision-Touchpad`（主副本：WANGSAN-LP；DESKTOP-5T8JV9O 桌面版為備份） |
| 預設敏感度 | `Sensitivity = 0.6`（HID delta 倍率；唔跟系統游標速度） |

## 4. 架構（邏輯）

```
PTP HID / Raw Input
        │
        ▼
┌───────────────────┐
│ Contact Tracker   │  指數、座標、delta、抬指
└─────────┬─────────┘
          │
    ┌─────┴─────┐
    ▼           ▼
 3-finger     4-finger
  pipeline     pipeline（v1：忽略／預留）
    │
    ▼
┌───────────────────┐
│ Drag Controller   │  閾值 → mouse down → move → mouse up
│ (SendInput)       │
└───────────────────┘
          │
          ▼
   Tray + Settings UI（啟用、自啟、敏感度等）
```

要點：

1. **FingerCountRouter**：按同時 contact 數分流，避免 3／4 指邏輯糾纏。
2. **ThreeFingerDragEngine**：狀態機 `Idle → Pending → Dragging → End`。
3. **MouseInjector**：`SendInput`／`mouse_event` 封裝，方便單測同日後換實作。
4. **SettingsStore**：簡單本地設定（JSON 喺 `%LocalAppData%`），設定窗讀寫；唔逼用戶手改檔。

詳細模組／資料流見 [architecture.md](architecture.md)。

## 5. 程式碼策略

- **自寫 WinForms 原型**（對齊 Q12），精讀主參考嘅 Raw Input／拖曳狀態機／干擾排除，**唔**以 WinUI3 fork 做主線（減依賴、易維護）。
- 許可證：主參考 MIT，可學習／適度移植思路；第三方授權聲明見 [licenses.md](licenses.md)。
- 可選 **v0 手感驗證**：本機先裝官方 ThreeFingerDragOnWindows 對照手感（唔改系統以外設定）；唔取代自寫交付。

開源清單：[oss-references.md](oss-references.md)。

## 6. 倉庫／目錄規劃

```
Win-Precision-Touchpad/
  README.md                 # 用戶向：點裝、點關系統三指、點用
  docs/
    plan.md                 # 本檔
    architecture.md         # 模組、狀態機、訊息流
    setup-touchpad.md       # 手動將三指設為「無」
    testing.md              # 測試清單（M1–M3 + 自動化）
    settings-upgrade.md     # 設定檔版本／升級
    oss-references.md       # 開源參考
    licenses.md             # 授權聲明
    changelog.md            # M1–M4 變更摘要
  src/
    WinTouchpad.Tray/       # WinForms 進駐系統匣 + 設定窗
    WinTouchpad.Core/       # Router、DragEngine、RawInput、Injector、Settings
  scripts/                  # 建置、打包（唔自動改註冊表）
  tests/                    # 狀態機／Settings 單元測試
  config/                   # 預設設定範例
```

## 7. 技術文檔要求（你特別強調）

最少要有：

1. **architecture.md**：模組邊界、3／4 指分流、拖曳狀態機、點解要關系統三指。
2. **setup-touchpad.md**：Win11 設定路徑（藍牙與裝置 → 觸控板 → 三指手勢 → 無）。
3. **Raw Input／PTP 備註**：用邊啲 API、已知機型限制、管理員視窗拖曳限制（升高權限先拖到 admin 視窗）。
4. **升級指南**：設定檔版本欄、變更紀錄（[settings-upgrade.md](settings-upgrade.md)、[changelog.md](changelog.md)）。
5. README 保持短；細節放 `docs/`。

## 8. 里程碑

| 階段 | 內容 | 完成準則 |
|------|------|----------|
| M0 | 文件定稿（本 plan 確認） | 你喺群組確認「按呢個 plan 做」 |
| M1 | Core 骨架：Raw Input 讀到指數／座標 + 日誌 | 三指移動時 log 穩定 |
| M2 | DragEngine + SendInput | 三指可拖視窗／選字；抬指結束 |
| M3 | Tray + 設定窗 + 自啟 | 可開關手勢、改敏感度、自啟 |
| M4 | docs 齊 + 測試清單走過 | `testing.md` 齊；README 可跟住用 |

## 9. 風險同緩解

| 風險 | 緩解 |
|------|------|
| 系統三指手勢食 input | 文件強制手動設「無」；啟動時偵測／提示（唔自動改） |
| 各廠 PTP HID 報告唔同 | 先跟 RawInput.Touchpad／主參考驗證；留 adapter |
| 拖唔到管理員視窗 | 文件說明；設定可提示「以系統管理員執行」 |
| WinForms vs 主參考 WinUI 差異 | 只移植輸入／拖曳核心，UI 自寫 |
| 設定窗 `Show()` 唔關 | 「關閉」必須 `Click → Close()`；唔依賴淨 `DialogResult` |

## 10. 確認後先做嘅下一手

（歷史：M0 確認後已執行。）

1. 更新根 `README.md` 簡介同連結本 plan  
2. 起草 `docs/architecture.md` 同 `docs/setup-touchpad.md`  
3. 建立 `src` 方案（.NET 8 WinForms）  

---

**Shared understanding 已確認（群組「可以開始了」）。** M1–M4 已落地；見 §11。

## 11. 里程碑實作狀態

### M1
- **狀態：** 完成（Raw Input + Tray 日誌 + 單測；用戶確認 log 正常／`contacts=3`）

### M2
- **狀態：** 完成（程式／單測／文件；用戶確認拖曳 OK）
- **內容：** `MouseInjector`（SendInput）、`DragEngine` 狀態機、Pipeline 接線、Tray 啟用開關、`DragEngineTests`
- **手感：** 預設 `Sensitivity` 由 1.0 調至 **0.6**（用戶確認較接近系統速度）

### M3
- **狀態：** 完成（程式／單測／文件；用戶確認設定窗可開、「關閉」修復後有效）
- **內容：** `SettingsStore`（`%LocalAppData%\WinPrecisionTouchpad\settings.json` + `settingsVersion`）、WinForms 設定窗（啟用／敏感度／開始距離／自啟）、`AutostartHelper`（HKCU Run）、Tray「設定…」、啟動載入設定、`SettingsStoreTests`、`docs/settings-upgrade.md`
- **修復：** 設定窗以 `Show()` 開啟；「關閉」改為 `Click → Close()`；`ClientSize` 高 310、按鈕 y=255，路徑提示唔再蓋掣

### M4
- **狀態：** **完成**（本輪）
- **內容：** README 用戶路徑 polish；`testing.md` 全清單（標示已驗證 vs 用戶仲要人手）；`architecture`／`setup`／`settings-upgrade`／`licenses`／`oss-references` 交叉連結對齊；新增 `changelog.md`；本 plan §11 收束
- **人手：** 跟 [testing.md](testing.md)「用戶仲要確認」各項（尤其自啟重開機、選字／拖檔若未試）
