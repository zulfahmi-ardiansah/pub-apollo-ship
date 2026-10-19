using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApolloPlaylist
{
    internal sealed class AsciiDisplay : Control
    {
        internal enum DisplayState { Ready, Process, Complete }

        private const int FrameCount = 64;

        private static readonly string[] Logo = {
            "    _    ____   ___  _     _      ___",
            "   / \\  |  _ \\ / _ \\| |   | |    / _ \\",
            "  / _ \\ | |_) | | | | |   | |   | | | |",
            " / ___ \\|  __/| |_| | |___| |___| |_| |",
            "/_/   \\_\\_|    \\___/|_____|_____\\___/"
        };
        private static readonly byte[,] BarHeights = CreateBarHeights();
        private readonly Font metaFont = new Font("Consolas", 7f, FontStyle.Bold);
        private readonly Font artFont = new Font("Consolas", 10f, FontStyle.Bold);
        private readonly Font barsFont = new Font("Consolas", 9f);
        private readonly Pen darkScan = new Pen(Color.FromArgb(20, 20, 20));
        private readonly Pen lightScan = new Pen(Color.FromArgb(237, 237, 237));
        private readonly Pen darkBorder = new Pen(Color.FromArgb(146, 146, 146));
        private readonly Pen lightBorder = new Pen(Color.FromArgb(111, 111, 111));
        private readonly Pen darkBars = new Pen(Color.FromArgb(194, 194, 194));
        private readonly Pen lightBars = new Pen(Color.FromArgb(75, 75, 75));
        private Bitmap background;
        private bool backgroundDirty = true;
        private bool light;
        private DisplayState state;
        private int barCellWidth;
        private int barStartX;
        private int step;
        internal bool Light
        {
            get { return light; }
            set { if (light != value) { light = value; backgroundDirty = true; Invalidate(); } }
        }
        internal DisplayState State
        {
            get { return state; }
            set { if (state != value) { state = value; backgroundDirty = true; Invalidate(); } }
        }

        internal AsciiDisplay()
        {
            DoubleBuffered = true;
            TabStop = false;
        }

        internal void Advance() { step = (step + 1) % FrameCount; Invalidate(); }

        private static byte[,] CreateBarHeights()
        {
            byte[,] heights = new byte[FrameCount, 32];
            for (int frame = 0; frame < FrameCount; frame++)
                for (int col = 0; col < 32; col++)
                {
                    double wave = Math.Sin(col * .48 + frame * Math.PI / 8);
                    double echo = Math.Sin(col * .91 - frame * Math.PI / 16);
                    heights[frame, col] = (byte)Math.Max(0, Math.Min(4, (int)Math.Round(2 + wave + echo)));
                }
            return heights;
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            if (Width <= 0 || Height <= 0) return;
            if (background == null || background.Width != Width || background.Height != Height || backgroundDirty)
            {
                if (background != null) background.Dispose();
                background = new Bitmap(Width, Height);
                using (Graphics layer = Graphics.FromImage(background)) DrawBackground(layer);
                backgroundDirty = false;
            }
            Graphics g = args.Graphics;
            g.DrawImageUnscaled(background, 0, 0);
            Pen bars = Light ? lightBars : darkBars;
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 32; col++)
                    if (BarHeights[step, col] > 3 - row)
                    {
                        int x = barStartX + col * barCellWidth;
                        int y = 109 + row * 12;
                        g.DrawLine(bars, x, y + 2, x, y + 10);
                    }
        }

        private void DrawBackground(Graphics g)
        {
            Color field = Light ? Color.White : Color.FromArgb(8, 8, 8);
            Color ink = Light ? Color.FromArgb(17, 17, 17) : Color.FromArgb(244, 244, 244);
            Color muted = Light ? Color.FromArgb(75, 75, 75) : Color.FromArgb(194, 194, 194);
            g.Clear(field);
            Pen scan = Light ? lightScan : darkScan;
            Pen border = Light ? lightBorder : darkBorder;
            for (int y = 3; y < Height; y += 3) g.DrawLine(scan, 2, y, Width - 3, y);
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            g.DrawRectangle(border, 3, 3, Width - 7, Height - 7);
            TextRenderer.DrawText(g, "Youtube Music Downloader", metaFont, new Point(9, 8), muted, TextFormatFlags.NoPadding);
            string stateText = State == DisplayState.Process ? "\u25B6 Process"
                : State == DisplayState.Complete ? "\u2B24 Complete" : "\u2B24 Ready";
            TextRenderer.DrawText(g, stateText, metaFont, new Rectangle(350, 8, 116, 14), muted,
                TextFormatFlags.Right | TextFormatFlags.NoPadding);
            for (int i = 0; i < Logo.Length; i++)
                TextRenderer.DrawText(g, Logo[i], artFont, new Rectangle(12, 30 + i * 15, Width - 24, 18), ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
            barCellWidth = Math.Max(1, TextRenderer.MeasureText("||||", barsFont, new Size(100, 20),
                TextFormatFlags.NoPadding).Width / 4);
            barStartX = (Width - 36 * barCellWidth) / 2 + 2 * barCellWidth;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (background != null) background.Dispose();
                metaFont.Dispose(); artFont.Dispose(); barsFont.Dispose();
                darkScan.Dispose(); lightScan.Dispose(); darkBorder.Dispose(); lightBorder.Dispose();
                darkBars.Dispose(); lightBars.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
