# 五子棋 Gomoku

> Windows Programming (II) 作業二 — 棋牌類遊戲

## 遊戲介紹

雙人對弈的五子棋遊戲，先在棋盤上連成五顆棋子（橫、縱、斜均可）者獲勝。

## 功能

- ✅ 15×15 標準棋盤，含座標標示（A–O / 1–15）
- ✅ 棋子落子音效（黑棋低頻、白棋高頻）＋勝利琶音
- ✅ 漸層棋子渲染（高光 + 陰影）
- ✅ 滑鼠懸停預覽 / 最後一手紅點標記
- ✅ 勝利棋子金框高亮
- ✅ 悔棋功能
- ✅ 計分板（本局累積）
- ✅ 鍵盤快捷鍵：`N` 新遊戲、`U` 悔棋

## 執行方式

### 步驟

```bash
git clone <your-repo-url>
cd Gomoku
dotnet run
```

或在 Visual Studio 2022 中開啟 `Gomoku.csproj` 並按 **F5**。

## 截圖

<img width="788" height="650" alt="螢幕擷取畫面 2026-05-27 220109" src="https://github.com/user-attachments/assets/5371bb12-f52a-4b1d-a612-a22e0469975a" />


## 玩法說明

1. 黑棋先行，輪流在棋盤交叉點點擊落子。
2. 先在任意方向（橫 / 縱 / 斜）連成 **五子** 者勝。
3. 若棋盤下滿則為平局。
4. 點擊「悔棋」可撤回上一步，按 `N` 開始新局。

## 音效實作

音效以程式動態產生（指數衰減正弦波 WAV），不依賴外部音訊檔案，
使用 `System.Media.SoundPlayer` 播放。

## 開發環境

- C# / .NET 6 Windows Forms
- Visual Studio 2022
