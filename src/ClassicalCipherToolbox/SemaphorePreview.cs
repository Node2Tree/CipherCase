using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox
{
    internal sealed class SemaphorePreview : ScrollableControl
    {
        private const int TileWidth = 108;
        private const int TileHeight = 142;
        private const int PreviewLimit = 256;
        private readonly List<char> letters = new List<char>();
        private bool truncated;

        internal SemaphorePreview()
        {
            DoubleBuffered = true;
            AutoScroll = true;
            BackColor = Color.White;
            AccessibleName = "旗语图形预览，面向观察者";
        }

        internal int SymbolCount { get { return letters.Count; } }

        internal void SetOutput(string output, bool encoded)
        {
            letters.Clear();
            truncated = false;
            if (encoded)
            {
                foreach (string token in (output ?? string.Empty).Split(new[] { '/', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!AddSymbol(SemaphoreCode.Letter(token))) break;
            }
            else
            {
                foreach (char letter in output ?? string.Empty)
                    if (!char.IsWhiteSpace(letter) && !AddSymbol(char.ToUpperInvariant(letter))) break;
            }
            AutoScrollPosition = Point.Empty;
            UpdateScrollArea();
            Invalidate();
        }

        private bool AddSymbol(char letter)
        {
            if (letters.Count >= PreviewLimit) { truncated = true; return false; }
            letters.Add(letter);
            return true;
        }

        private void UpdateScrollArea()
        {
            AutoScrollMinSize = new Size(Math.Max(1, letters.Count) * TileWidth, TileHeight + 24);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
            using (Pen body = new Pen(Color.FromArgb(47, 57, 71), 3))
            using (Pen pole = new Pen(Color.FromArgb(80, 87, 97), 2))
            using (Brush ink = new SolidBrush(Color.FromArgb(47, 57, 71)))
            {
                for (int i = 0; i < letters.Count; i++)
                {
                    float x = i * TileWidth;
                    if (x + TileWidth + AutoScrollPosition.X < 0 || x + AutoScrollPosition.X > ClientSize.Width) continue;
                    string pair = SemaphoreCode.Directions(letters[i]);
                    graphics.DrawString(letters[i].ToString(), Font, ink, x + 48, 122);
                    if (pair.Length != 2) continue;
                    PointF shoulder = new PointF(x + 54, 59);
                    graphics.DrawEllipse(body, x + 47, 32, 14, 14);
                    graphics.DrawLine(body, x + 54, 48, x + 54, 90);
                    graphics.DrawLine(body, x + 54, 90, x + 43, 110);
                    graphics.DrawLine(body, x + 54, 90, x + 65, 110);
                    DrawArm(graphics, body, pole, shoulder, pair[0]);
                    DrawArm(graphics, body, pole, shoulder, pair[1]);
                }
                graphics.ResetTransform();
                graphics.DrawString(truncated ? "面向观察者 · 仅预览前 256 个符号，文本输出完整" : "面向观察者 · 可横向滚动", Font, ink, 4, 3);
            }
        }

        private static void DrawArm(Graphics graphics, Pen body, Pen pole, PointF origin, char arrow)
        {
            string directions = "↑↗→↘↓↙←↖";
            int index = directions.IndexOf(arrow);
            double angle = index * Math.PI / 4 - Math.PI / 2;
            float dx = (float)Math.Cos(angle), dy = (float)Math.Sin(angle);
            PointF hand = new PointF(origin.X + dx * 25, origin.Y + dy * 25);
            PointF end = new PointF(origin.X + dx * 40, origin.Y + dy * 40);
            graphics.DrawLine(body, origin, hand);
            graphics.DrawLine(pole, hand, end);
            // A two-colour flag attached along the outer part of the pole.
            PointF a = new PointF(end.X - dx * 12, end.Y - dy * 12);
            PointF b = new PointF(a.X - dy * 10, a.Y + dx * 10);
            PointF c = new PointF(end.X - dy * 10, end.Y + dx * 10);
            graphics.FillPolygon(Brushes.Goldenrod, new[] { a, b, c });
            graphics.FillPolygon(Brushes.Firebrick, new[] { a, c, end });
        }
    }
}
