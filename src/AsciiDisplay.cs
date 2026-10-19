using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApolloPlaylist
{
    internal sealed class AsciiDisplay : Control
    {
        internal enum DisplayState { Ready, Process, Complete }

        private static readonly string[] Logo = {
            "    _    ____   ___  _     _      ___",
            "   / \\  |  _ \\ / _ \\| |   | |    / _ \\",
            "  / _ \\ | |_) | | | | |   | |   | | | |",
            " / ___ \\|  __/| |_| | |___| |___| |_| |",
            "/_/   \\_\\_|    \\___/|_____|_____\\___/"
        };
        private int step;
        internal bool Light { get; set; }
        internal DisplayState State { get; set; }

        internal AsciiDisplay()
        {
            DoubleBuffered = true;
            TabStop = false;
        }

        internal void Advance() { step++; Invalidate(); }

        protected override void OnPaint(PaintEventArgs args)
        {
            Graphics g = args.Graphics;
            Color field = Light ? Color.White : Color.FromArgb(8, 8, 8);
            Color ink = Light ? Color.FromArgb(17, 17, 17) : Color.FromArgb(244, 244, 244);
            Color muted = Light ? Color.FromArgb(75, 75, 75) : Color.FromArgb(194, 194, 194);
            Color line = Light ? Color.FromArgb(111, 111, 111) : Color.FromArgb(146, 146, 146);
            g.Clear(field);
            using (Pen scan = new Pen(Light ? Color.FromArgb(237, 237, 237) : Color.FromArgb(20, 20, 20)))
                for (int y = 3; y < Height; y += 3) g.DrawLine(scan, 2, y, Width - 3, y);
            using (Pen border = new Pen(line))
            {
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                g.DrawRectangle(border, 3, 3, Width - 7, Height - 7);
            }
            using (Font meta = new Font("Consolas", 7f, FontStyle.Bold))
            using (Font art = new Font("Consolas", 10f, FontStyle.Bold))
            using (Font bars = new Font("Consolas", 9f))
            {
                TextRenderer.DrawText(g, "Youtube Music Downloader", meta, new Point(9, 8), muted, TextFormatFlags.NoPadding);
                string stateText = State == DisplayState.Process ? "\u25B6 Process"
                    : State == DisplayState.Complete ? "\u2B24 Complete" : "\u2B24 Ready";
                TextRenderer.DrawText(g, stateText, meta, new Rectangle(350, 8, 116, 14), muted,
                    TextFormatFlags.Right | TextFormatFlags.NoPadding);
                for (int i = 0; i < Logo.Length; i++)
                    TextRenderer.DrawText(g, Logo[i], art, new Rectangle(12, 30 + i * 15, Width - 24, 18), ink,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
                for (int row = 0; row < 4; row++)
                {
                    char[] chars = new char[36];
                    chars[0] = '|'; chars[1] = ' '; chars[34] = ' '; chars[35] = '|';
                    for (int col = 0; col < 32; col++)
                    {
                        double wave = Math.Sin(col * .48 + step * .35);
                        double echo = Math.Sin(col * .91 - step * .22);
                        int height = Math.Max(0, Math.Min(4, (int)Math.Round(2 + wave + echo)));
                        chars[col + 2] = height > 3 - row ? '|' : ' ';
                    }
                    TextRenderer.DrawText(g, new string(chars), bars, new Rectangle(20, 109 + row * 12, Width - 40, 14), muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                }
            }
        }
    }
}
