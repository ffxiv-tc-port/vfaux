# Easier Faux Hollows

無人島開拓「Faux Hollows」翻牌小遊戲的解謎輔助工具，比對已知盤面樣式，即時提示最佳下一步。

## 功能

- 自動讀取目前遊戲內的翻牌盤面，比對已知樣式庫並標出建議分數最高的格子
- 「優先找劍」策略開關
- 除錯／模擬盤面：可手動任意翻牌測試比對邏輯

## 使用

- `/vfaux` 開啟視窗

## 台服（TC）分支說明

這是 [awgil/vfaux](https://github.com/awgil/vfaux) 針對**台服官方繁中版**（Dalamud API 13）
維護的 fork，由 [ffxiv-tc-port](https://github.com/ffxiv-tc-port) 發佈。

安裝方式：在 Dalamud 設定的「自訂插件庫」加入
`https://raw.githubusercontent.com/ffxiv-tc-port/DalamudPluginsTC/main/repo.json` 並啟用，
再從插件列表安裝。

請**不要**改用上游的 `https://puni.sh/api/repository/veyn` 安裝 —— 上游版本已進入 API 15，
在台服客戶端載不起來。
