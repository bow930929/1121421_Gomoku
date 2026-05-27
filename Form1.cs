using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace Gomoku
{
    public class Form1 : Form
    {
        // ── 棋盤常數 ──────────────────────────────────────────
        private const int BOARD_SIZE  = 15;
        private const int CELL_SIZE   = 38;
        private const int MARGIN      = 36;
        private const int STONE_R     = 15;   // 棋子半徑

        // ── 遊戲狀態 ──────────────────────────────────────────
        private int[,] board       = new int[BOARD_SIZE, BOARD_SIZE]; // 0=空 1=黑 2=白
        private int    currentPlayer = 1;
        private bool   gameOver      = false;
        private Point  lastMove      = new Point(-1, -1);
        private Point  hoverCell     = new Point(-1, -1);
        private Point[] winLine      = null;
        private int    blackWins     = 0;
        private int    whiteWins     = 0;
        private readonly Stack<Point> history = new Stack<Point>();

        // ── UI 元件 ───────────────────────────────────────────
        private Panel  pnlBoard;
        private Label  lblStatus;
        private Label  lblScore;
        private Button btnNew;
        private Button btnUndo;

        // ─────────────────────────────────────────────────────
        public Form1()
        {
            BuildUI();
            StartNewGame();
        }

        // ══════════════════════════════════════════════════════
        //  UI 建立
        // ══════════════════════════════════════════════════════
        private void BuildUI()
        {
            int boardPx = MARGIN * 2 + CELL_SIZE * (BOARD_SIZE - 1);

            Text            = "五子棋  Gomoku";
            ClientSize      = new Size(boardPx + 185, boardPx + 20);
            BackColor       = Color.FromArgb(28, 28, 28);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = new Font("微軟正黑體", 10);
            KeyPreview      = true;
            KeyDown        += OnKeyDown;

            // ── 棋盤 Panel ────────────────────────────────────
            pnlBoard = new Panel
            {
                Location = new Point(10, 10),
                Size     = new Size(boardPx, boardPx)
            };
            pnlBoard.Paint      += Board_Paint;
            pnlBoard.MouseClick += Board_Click;
            pnlBoard.MouseMove  += Board_MouseMove;
            pnlBoard.MouseLeave += (s, e) => { hoverCell = new Point(-1, -1); pnlBoard.Invalidate(); };
            Controls.Add(pnlBoard);

            int sx = boardPx + 22; // 側邊欄 X

            // 標題
            Controls.Add(new Label
            {
                Text      = "五子棋",
                Location  = new Point(sx, 12),
                Size      = new Size(148, 36),
                Font      = new Font("微軟正黑體", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 180, 90),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // 回合狀態
            lblStatus = new Label
            {
                Location  = new Point(sx, 58),
                Size      = new Size(148, 58),
                Font      = new Font("微軟正黑體", 12, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(48, 48, 48)
            };
            Controls.Add(lblStatus);

            // 計分板
            lblScore = new Label
            {
                Location  = new Point(sx, 126),
                Size      = new Size(148, 46),
                Font      = new Font("微軟正黑體", 10),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(42, 42, 42)
            };
            Controls.Add(lblScore);

            // 按鈕
            btnNew  = MakeBtn("新遊戲  (N)", new Point(sx, 185), Color.FromArgb(60, 120, 175));
            btnUndo = MakeBtn("悔棋  (U)",   new Point(sx, 233), Color.FromArgb(100, 90, 60));
            btnNew.Click  += (s, e) => StartNewGame();
            btnUndo.Click += (s, e) => UndoMove();

            // 分隔線
            Controls.Add(new Label
            {
                Location  = new Point(sx, 285),
                Size      = new Size(148, 1),
                BackColor = Color.FromArgb(70, 70, 70)
            });

            // 規則說明
            Controls.Add(new Label
            {
                Text      = "【規則】\n先連成五子者勝\n黑棋先行\n\n快捷鍵\nN：新遊戲\nU：悔棋",
                Location  = new Point(sx, 292),
                Size      = new Size(148, 110),
                Font      = new Font("微軟正黑體", 8.5f),
                ForeColor = Color.FromArgb(140, 140, 140),
                TextAlign = ContentAlignment.TopCenter
            });
        }

        private Button MakeBtn(string text, Point loc, Color baseColor)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new Size(148, 36),
                Font      = new Font("微軟正黑體", 10, FontStyle.Bold),
                BackColor = baseColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ControlPaint.LightLight(baseColor);
            btn.FlatAppearance.BorderSize  = 1;
            Controls.Add(btn);
            return btn;
        }

        // ══════════════════════════════════════════════════════
        //  遊戲控制
        // ══════════════════════════════════════════════════════
        private void StartNewGame()
        {
            board         = new int[BOARD_SIZE, BOARD_SIZE];
            currentPlayer = 1;
            gameOver      = false;
            lastMove      = new Point(-1, -1);
            hoverCell     = new Point(-1, -1);
            winLine       = null;
            history.Clear();
            RefreshStatus();
            pnlBoard.Invalidate();
        }

        private void UndoMove()
        {
            if (history.Count == 0 || gameOver) return;
            Point p = history.Pop();
            board[p.Y, p.X] = 0;
            lastMove      = history.Count > 0 ? history.Peek() : new Point(-1, -1);
            currentPlayer = currentPlayer == 1 ? 2 : 1;
            RefreshStatus();
            pnlBoard.Invalidate();
        }

        private void PlaceStone(int row, int col)
        {
            board[row, col] = currentPlayer;
            lastMove        = new Point(col, row);
            history.Push(lastMove);

            PlayStoneSound(currentPlayer);

            Point[] wl = FindWinLine(row, col);
            if (wl != null)
            {
                winLine  = wl;
                gameOver = true;
                if (currentPlayer == 1) blackWins++; else whiteWins++;
                RefreshStatus();
                pnlBoard.Invalidate();
                PlayWinSound();
                return;
            }

            // 是否平局
            bool hasFree = false;
            for (int r = 0; r < BOARD_SIZE && !hasFree; r++)
                for (int c = 0; c < BOARD_SIZE && !hasFree; c++)
                    if (board[r, c] == 0) hasFree = true;

            if (!hasFree)
            {
                gameOver         = true;
                lblStatus.Text   = "平  局！";
                lblStatus.BackColor = Color.FromArgb(70, 60, 40);
                pnlBoard.Invalidate();
                return;
            }

            currentPlayer = currentPlayer == 1 ? 2 : 1;
            hoverCell     = new Point(-1, -1);
            RefreshStatus();
            pnlBoard.Invalidate();
        }

        // ── 輸贏判斷 ──────────────────────────────────────────
        private Point[] FindWinLine(int row, int col)
        {
            int player = board[row, col];
            int[][] dirs = { new[]{0,1}, new[]{1,0}, new[]{1,1}, new[]{1,-1} };

            foreach (var d in dirs)
            {
                var cells = new List<Point> { new Point(col, row) };
                for (int k = 1; k <= 4; k++)
                {
                    int r = row + d[0]*k, c = col + d[1]*k;
                    if (r<0||r>=BOARD_SIZE||c<0||c>=BOARD_SIZE||board[r,c]!=player) break;
                    cells.Add(new Point(c, r));
                }
                for (int k = 1; k <= 4; k++)
                {
                    int r = row - d[0]*k, c = col - d[1]*k;
                    if (r<0||r>=BOARD_SIZE||c<0||c>=BOARD_SIZE||board[r,c]!=player) break;
                    cells.Add(new Point(c, r));
                }
                if (cells.Count >= 5) return cells.ToArray();
            }
            return null;
        }

        // ── 狀態更新 ──────────────────────────────────────────
        private void RefreshStatus()
        {
            if (gameOver && winLine != null)
            {
                string w = currentPlayer == 1 ? "⚫ 黑棋" : "⚪ 白棋";
                lblStatus.Text      = $"{w}\n獲  勝！";
                lblStatus.BackColor = Color.FromArgb(55, 95, 45);
            }
            else if (!gameOver)
            {
                string p = currentPlayer == 1 ? "⚫ 黑棋" : "⚪ 白棋";
                lblStatus.Text      = $"輪到\n{p}";
                lblStatus.BackColor = Color.FromArgb(48, 48, 48);
            }
            lblScore.Text = $"⚫ 黑: {blackWins} 勝\n⚪ 白: {whiteWins} 勝";
        }

        // ══════════════════════════════════════════════════════
        //  事件處理
        // ══════════════════════════════════════════════════════
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.N) StartNewGame();
            if (e.KeyCode == Keys.U) UndoMove();
        }

        private void Board_MouseMove(object sender, MouseEventArgs e)
        {
            if (gameOver) return;
            int col = (int)Math.Round((e.X - MARGIN) / (float)CELL_SIZE);
            int row = (int)Math.Round((e.Y - MARGIN) / (float)CELL_SIZE);
            Point nh = (col >= 0 && col < BOARD_SIZE && row >= 0 && row < BOARD_SIZE && board[row, col] == 0)
                       ? new Point(col, row) : new Point(-1, -1);
            if (nh != hoverCell) { hoverCell = nh; pnlBoard.Invalidate(); }
        }

        private void Board_Click(object sender, MouseEventArgs e)
        {
            if (gameOver) return;
            int col = (int)Math.Round((e.X - MARGIN) / (float)CELL_SIZE);
            int row = (int)Math.Round((e.Y - MARGIN) / (float)CELL_SIZE);
            if (col < 0 || col >= BOARD_SIZE || row < 0 || row >= BOARD_SIZE) return;
            if (board[row, col] != 0) return;
            PlaceStone(row, col);
        }

        // ══════════════════════════════════════════════════════
        //  繪圖
        // ══════════════════════════════════════════════════════
        private void Board_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;

            DrawBackground(g);
            DrawGrid(g);
            DrawLabels(g);
            DrawStones(g);
            DrawWinHighlight(g);
            DrawHoverStone(g);
        }

        private void DrawBackground(Graphics g)
        {
            var r = pnlBoard.ClientRectangle;
            using (var bg = new LinearGradientBrush(r,
                Color.FromArgb(215, 178, 120), Color.FromArgb(188, 148, 90), 135f))
                g.FillRectangle(bg, r);

            // 外框
            using (var p = new Pen(Color.FromArgb(115, 72, 22), 3))
                g.DrawRectangle(p, 2, 2, r.Width - 5, r.Height - 5);
        }

        private void DrawGrid(Graphics g)
        {
            int lo = MARGIN, hi = MARGIN + (BOARD_SIZE - 1) * CELL_SIZE;
            using (var pen = new Pen(Color.FromArgb(75, 45, 15), 1))
            {
                for (int i = 0; i < BOARD_SIZE; i++)
                {
                    int v = MARGIN + i * CELL_SIZE;
                    g.DrawLine(pen, v, lo, v, hi);
                    g.DrawLine(pen, lo, v, hi, v);
                }
            }

            // 星位
            int[] sp = { 3, 7, 11 };
            foreach (int sr in sp)
                foreach (int sc in sp)
                {
                    int px = MARGIN + sc * CELL_SIZE, py = MARGIN + sr * CELL_SIZE;
                    g.FillEllipse(Brushes.Black, px - 4, py - 4, 8, 8);
                }
        }

        private void DrawLabels(Graphics g)
        {
            using (var f = new Font("Arial", 8))
            using (var b = new SolidBrush(Color.FromArgb(95, 58, 18)))
            {
                for (int i = 0; i < BOARD_SIZE; i++)
                {
                    // 欄位（A-O）
                    g.DrawString(((char)('A' + i)).ToString(), f, b,
                        MARGIN + i * CELL_SIZE - 5, 6);
                    // 列號（15-1）
                    string row = (BOARD_SIZE - i).ToString();
                    g.DrawString(row, f, b,
                        row.Length == 2 ? 1 : 5, MARGIN + i * CELL_SIZE - 7);
                }
            }
        }

        private void DrawStones(Graphics g)
        {
            for (int r = 0; r < BOARD_SIZE; r++)
                for (int c = 0; c < BOARD_SIZE; c++)
                    if (board[r, c] != 0)
                        RenderStone(g,
                            MARGIN + c * CELL_SIZE,
                            MARGIN + r * CELL_SIZE,
                            board[r, c],
                            lastMove.X == c && lastMove.Y == r);
        }

        private void RenderStone(Graphics g, int cx, int cy, int player, bool isLast)
        {
            int r   = STONE_R;
            var rect = new Rectangle(cx - r, cy - r, r * 2, r * 2);

            // 外陰影
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                g.FillEllipse(shadowBrush, cx - r + 3, cy - r + 3, r * 2, r * 2);

            if (player == 1) // ── 黑棋
            {
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var pgb = new PathGradientBrush(path))
                    {
                        pgb.CenterColor    = Color.FromArgb(80, 80, 80);
                        pgb.SurroundColors = new[] { Color.FromArgb(8, 8, 8) };
                        pgb.CenterPoint    = new PointF(cx - r * 0.28f, cy - r * 0.28f);
                        g.FillEllipse(pgb, rect);
                    }
                }
            }
            else              // ── 白棋
            {
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var pgb = new PathGradientBrush(path))
                    {
                        pgb.CenterColor    = Color.White;
                        pgb.SurroundColors = new[] { Color.FromArgb(160, 160, 160) };
                        pgb.CenterPoint    = new PointF(cx - r * 0.28f, cy - r * 0.28f);
                        g.FillEllipse(pgb, rect);
                    }
                }
                using (var p = new Pen(Color.FromArgb(120, 120, 120), 1))
                    g.DrawEllipse(p, rect);
            }

            // 高光
            var hlRect = new RectangleF(cx - r * 0.52f, cy - r * 0.52f, r * 0.55f, r * 0.38f);
            if (hlRect.Width > 1 && hlRect.Height > 1)
            {
                using (var hb = new SolidBrush(Color.FromArgb(player == 1 ? 60 : 150, 255, 255, 255)))
                    g.FillEllipse(hb, hlRect);
            }

            // 最後一手標記
            if (isLast)
            {
                using (var mp = new Pen(Color.FromArgb(220, Color.Red), 2))
                {
                    int ms = 5;
                    g.DrawLine(mp, cx - ms, cy, cx + ms, cy);
                    g.DrawLine(mp, cx, cy - ms, cx, cy + ms);
                }
            }
        }

        private void DrawWinHighlight(Graphics g)
        {
            if (winLine == null) return;
            using (var hlPen = new Pen(Color.FromArgb(210, Color.Gold), 3))
            {
                foreach (var pt in winLine)
                {
                    int cx = MARGIN + pt.X * CELL_SIZE;
                    int cy = MARGIN + pt.Y * CELL_SIZE;
                    g.DrawEllipse(hlPen, cx - STONE_R - 2, cy - STONE_R - 2,
                                  (STONE_R + 2) * 2, (STONE_R + 2) * 2);
                }
            }
        }

        private void DrawHoverStone(Graphics g)
        {
            if (hoverCell.X < 0 || gameOver) return;
            int cx = MARGIN + hoverCell.X * CELL_SIZE;
            int cy = MARGIN + hoverCell.Y * CELL_SIZE;
            int alpha = currentPlayer == 1 ? 70 : 90;
            Color c   = currentPlayer == 1 ? Color.FromArgb(alpha, 10, 10, 10)
                                           : Color.FromArgb(alpha, 245, 245, 245);
            using (var hb = new SolidBrush(c))
                g.FillEllipse(hb, cx - STONE_R, cy - STONE_R, STONE_R * 2, STONE_R * 2);
        }

        // ══════════════════════════════════════════════════════
        //  音效（程式內產生 WAV 波形）
        // ══════════════════════════════════════════════════════
        private void PlayStoneSound(int player)
        {
            int freq = player == 1 ? 620 : 880;   // 黑棋低沉、白棋清亮
            PlayToneAsync(freq, 55);
        }

        private void PlayWinSound()
        {
            // 上行五度琶音
            int[] melody = { 523, 659, 784, 1047, 1319 };
            System.Threading.Tasks.Task.Run(() =>
            {
                foreach (int f in melody)
                {
                    PlayToneSync(f, 160);
                    System.Threading.Thread.Sleep(25);
                }
            });
        }

        private void PlayToneAsync(int freq, int ms)
            => System.Threading.Tasks.Task.Run(() => PlayToneSync(freq, ms));

        private void PlayToneSync(int freq, int ms)
        {
            try
            {
                byte[] wav = BuildWav(freq, ms);
                using (var stream = new MemoryStream(wav))
                using (var sp    = new SoundPlayer(stream))
                    sp.PlaySync();
            }
            catch { /* 靜默失敗 */ }
        }

        /// <summary>以程式產生正弦波 WAV 位元組（16-bit, mono, 22050 Hz）</summary>
        private static byte[] BuildWav(int frequency, int durationMs)
        {
            const int sampleRate = 22050;
            int samples  = sampleRate * durationMs / 1000;
            int dataSize = samples * 2;
            byte[] wav   = new byte[44 + dataSize];

            // ── RIFF / fmt chunk ─────────────────────────────
            void WriteStr(int pos, string s) => System.Text.Encoding.ASCII.GetBytes(s).CopyTo(wav, pos);
            void WriteI32(int pos, int  v)   => BitConverter.GetBytes(v).CopyTo(wav, pos);
            void WriteI16(int pos, short v)  => BitConverter.GetBytes(v).CopyTo(wav, pos);

            WriteStr(0,  "RIFF");
            WriteI32(4,  36 + dataSize);
            WriteStr(8,  "WAVE");
            WriteStr(12, "fmt ");
            WriteI32(16, 16);
            WriteI16(20, 1);                     // PCM
            WriteI16(22, 1);                     // mono
            WriteI32(24, sampleRate);
            WriteI32(28, sampleRate * 2);
            WriteI16(32, 2);
            WriteI16(34, 16);
            WriteStr(36, "data");
            WriteI32(40, dataSize);

            // ── 樣本（指數衰減正弦波）────────────────────────
            for (int i = 0; i < samples; i++)
            {
                double t       = (double)i / sampleRate;
                double envelope = Math.Exp(-t * 14.0);
                short  sample  = (short)(short.MaxValue * 0.45 * envelope
                                         * Math.Sin(2.0 * Math.PI * frequency * t));
                BitConverter.GetBytes(sample).CopyTo(wav, 44 + i * 2);
            }
            return wav;
        }
    }
}
