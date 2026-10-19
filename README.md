# Easier Faux Hollows

[awgil/vfaux](https://github.com/awgil/vfaux) 的台服移植版。
「幻巧拼圖」的解謎輔助工具，比對已知盤面圖樣，即時提示最佳下一步。

## 功能

- 開啟幻巧拼圖介面時，自動讀取目前盤面、比對已知圖樣庫，直接在遊戲原生面板上為各格著色：
  藍色＝已確定的雙劍、黃色＝已確定的箱子或寶箱、紫色＝可能的狐狸、綠色＝目前期望值最高的格子
- 「一律優先找出雙劍」求解策略開關
- 除錯／模擬盤面：可手動任意翻牌測試比對邏輯
- 與 ezFauxHollows 不能併用（同時啟用時會跳出提示）

## 使用

- `/vfaux` 開啟視窗

## 台服版的修改

只做台服相容性修正（Dalamud 版本、繁中翻譯），功能與上游相同。

## 台服（TC）分支說明

安裝方式：在 Dalamud 設定的「自訂插件庫」加入
`https://raw.githubusercontent.com/ffxiv-tc-port/DalamudPluginsTC/main/repo.json` 並啟用，
再從插件列表安裝。

請**不要**改用上游的 `https://puni.sh/api/repository/veyn` 安裝 —— 上游版本已改用新版 Dalamud API，
在台服客戶端載不起來。

原作者：[awgil](https://github.com/awgil)
