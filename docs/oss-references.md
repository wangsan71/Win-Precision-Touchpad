# 開源參考（Three Finger Drag / PTP）

整理日期：2026-11-03。目標：macOS 風格三指拖曳移窗，C# 系統匣原型。

相關：[plan.md](plan.md)｜[architecture.md](architecture.md)｜[licenses.md](licenses.md)

## 首選參考

### [ClementGre/ThreeFingerDragOnWindows](https://github.com/ClementGre/ThreeFingerDragOnWindows)
- 授權：MIT｜語言：C#（WinUI 3）｜系統匣設定
- 做法：Raw Input 讀 PTP → 模擬滑鼠左鍵拖曳（移窗／選字）
- 依賴重點：內嵌 [emoacht/RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad) 思路、`H.NotifyIcon`、TaskScheduler（skip UAC）
- 使用前須手動關閉系統三指手勢（同我哋；見 [setup-touchpad.md](setup-touchpad.md)）
- **最貼合我哋已定：C#、常駐、設定 UI、三指拖曳**（我哋 UI 自寫 WinForms，唔 fork WinUI 主線）

## 其他可對照

| 專案 | 技術 | 備註 |
|------|------|------|
| [austinnixholm/ThreeFingerDrag](https://github.com/austinnixholm/ThreeFingerDrag) | C/C++、GPL-3.0、系統匣 | 輕量、有安裝包；授權偏嚴，宜只學行為唔直接抄 |
| [klkvsk/precise-three-fingers-drag](https://github.com/klkvsk/precise-three-fingers-drag) | C#、MIT、系統匣 | 細、PTP only，可作精簡對照 |
| [gurinderhans/threefingerdrag](https://github.com/gurinderhans/threefingerdrag/) | C# | 邊緣續拖、環境變數調參 |
| [nobu121/win3drag](https://github.com/nobu121/win3drag) | CLI（winget）、無 UI | 極簡、ini 設定；學關閉系統手勢流程 |
| [EsportToys/PrecisionThreeFingerDrag](https://github.com/EsportToys/PrecisionThreeFingerDrag) | AutoIt、GPL | RawInput+SendInput；硬編碼結構，僅作原理參考 |
| [Solez-ai/3-drag-win](https://github.com/Solez-ai/3-drag-win) | Rust+C++ | 視窗移動 vs 滑鼠拖曳模式、敏感度／死區 |

## 輸入層函式庫

- [emoacht/RawInput.Touchpad](https://github.com/emoacht/RawInput.Touchpad) — 讀 Precision Touchpad 多指座標（ThreeFingerDragOnWindows 亦用呢條路）

## 暫唔當主線

- [kamektx/TouchpadGestures_Advanced](https://github.com/kamektx/TouchpadGestures_Advanced) — 偏三指切分頁／快捷鍵，唔係拖曳移窗
- MacType — 字型渲染，無關觸控板

## 對我哋 plan 嘅含義

1. v1 **自寫 WinForms**，精讀 ThreeFingerDragOnWindows（MIT）／RawInput.Touchpad 思路；唔以 WinUI fork 做主線。
2. 共通前置：設定 → 觸控板 → 三指滑動／點擊設為「無」。
3. 核心迴路：偵測 3 contact → 按下左鍵 → 跟住 delta × Sensitivity 移游標 → 抬指放開。
4. 實作狀態見 [plan.md](plan.md) §11、[changelog.md](changelog.md)。
