using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ApolloPlaylist
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlaylistWindow());
        }
    }

    internal sealed partial class PlaylistWindow
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

        private readonly object processSync = new object();
        private Process downloadProcess;
        private System.Net.HttpWebRequest lyricRequest;
        private int downloadId;
        private volatile bool cancelRequested;
        private string lastError = "";
        private readonly JavaScriptSerializer uiJson = new JavaScriptSerializer();
        private readonly System.Windows.Forms.Timer animation = new System.Windows.Forms.Timer();
        private readonly ToolTip statusTip = new ToolTip();
        private readonly Font smallFont = new Font("Consolas", 8f);
        private readonly Font normalFont = new Font("Consolas", 9f);
        private readonly Font boldFont = new Font("Consolas", 9f, FontStyle.Bold);
        private readonly Label titleLabel, urlLabel, folderLabel, noteLabel, formatLabel, statusLabel, statusArrow;
        private readonly TextBox urlInput, folderInput;
        private readonly Panel urlBox, folderBox;
        private readonly Button themeButton, minimizeButton, closeButton, pasteButton, browseButton, startButton;
        private readonly AsciiDisplay ascii;
        private SoundPlayer loopPlayer;
        private Stream loopStream;
        private bool lightTheme;
        private bool downloading;

        internal PlaylistWindow()
        {
            Text = "Apollo Ship";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(560, 600);
            ShowInTaskbar = true;
            DoubleBuffered = true;
            KeyPreview = true;

            titleLabel = Label("APOLLO SHIP", 34, 29, 345, 22, boldFont);
            titleLabel.MouseDown += DragWindow;
            themeButton = Button("LIGHT", 388, 18, 63, 42, smallFont, delegate { Theme(!lightTheme); });
            minimizeButton = Button("_", 452, 18, 43, 42, boldFont, delegate { WindowState = FormWindowState.Minimized; });
            closeButton = Button("×", 496, 18, 43, 42, boldFont, delegate { Close(); });
            themeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.BorderSize = 0;
            ascii = new AsciiDisplay { Location = new Point(40, 80), Size = new Size(480, 161) };
            Controls.Add(ascii);
            urlLabel = Label("PLAYLIST URL", 51, 266, 450, 17, boldFont);
            urlBox = Input(51, 285, out urlInput);
            pasteButton = Button("Paste", 437, 285, 72, 44, smallFont, delegate { Paste(); });
            folderLabel = Label("SAVE TO FOLDER", 51, 360, 450, 17, boldFont);
            folderBox = Input(51, 380, out folderInput);
            browseButton = Button("Browse", 437, 380, 72, 44, smallFont, delegate { Browse(); });
            noteLabel = Label("Artist - Album / track number - title.m4a", 51, 430, 455, 18, smallFont);
            startButton = Button("Download", 40, 471, 99, 44, boldFont, delegate { DownloadOrStop(); });
            formatLabel = Label("M4A · cover · lyrics · subtitles", 153, 484, 355, 20, smallFont);
            statusArrow = Label(">", 40, 545, 12, 18, smallFont);
            statusLabel = Label("Ready. Enter a playlist URL and choose a folder.", 55, 545, 465, 18, smallFont);
            statusLabel.AutoEllipsis = true;
            statusTip.SetToolTip(statusLabel, statusLabel.Text);

            animation.Interval = 150;
            animation.Tick += delegate { ascii.Advance(); };
            animation.Start();
            Resize += delegate { animation.Enabled = WindowState != FormWindowState.Minimized; };
            Shown += delegate { StartLoopSound(); };
            FormClosing += delegate { StopLoopSound(); StopDownload(); };
            KeyDown += delegate(object sender, KeyEventArgs args) { if (args.KeyCode == Keys.Escape && !downloading) Close(); };
            Theme(false);
        }

        private void StartLoopSound()
        {
            try
            {
                loopStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApolloShip.Loop");
                if (loopStream == null) throw new InvalidOperationException("Embedded audio was not found.");
                loopPlayer = new SoundPlayer(loopStream);
                loopPlayer.Load();
                loopPlayer.PlayLooping();
            }
            catch (Exception error)
            {
                StopLoopSound();
                SetStatus("Audio could not play: " + error.Message);
            }
        }

        private void StopLoopSound()
        {
            if (loopPlayer != null)
            {
                loopPlayer.Stop();
                loopPlayer.Dispose();
                loopPlayer = null;
            }
            if (loopStream != null)
            {
                loopStream.Dispose();
                loopStream = null;
            }
        }

        private Label Label(string text, int x, int y, int width, int height, Font font)
        {
            Label label = new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height),
                Font = font, TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(label);
            return label;
        }

        private Button Button(string text, int x, int y, int width, int height, Font font, EventHandler click)
        {
            Button button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height),
                Font = font, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderSize = 1;
            button.Click += click;
            Controls.Add(button);
            return button;
        }

        private Panel Input(int x, int y, out TextBox input)
        {
            Panel host = new Panel { Location = new Point(x, y), Size = new Size(379, 44), BorderStyle = BorderStyle.FixedSingle };
            TextBox textBox = new TextBox { BorderStyle = BorderStyle.None, Location = new Point(11, 13), Width = 355,
                Font = normalFont, ShortcutsEnabled = true };
            input = textBox;
            host.Controls.Add(textBox);
            host.Click += delegate { textBox.Focus(); };
            Controls.Add(host);
            return host;
        }

        private void Theme(bool light)
        {
            lightTheme = light;
            Color backdrop = light ? Color.FromArgb(222, 222, 222) : Color.FromArgb(23, 23, 23);
            Color surface = light ? Color.FromArgb(237, 237, 237) : Color.FromArgb(26, 26, 26);
            Color window = light ? Color.FromArgb(250, 250, 250) : Color.FromArgb(14, 14, 14);
            Color field = light ? Color.White : Color.FromArgb(8, 8, 8);
            Color ink = light ? Color.FromArgb(17, 17, 17) : Color.FromArgb(244, 244, 244);
            Color muted = light ? Color.FromArgb(75, 75, 75) : Color.FromArgb(194, 194, 194);
            Color line = light ? Color.FromArgb(111, 111, 111) : Color.FromArgb(146, 146, 146);
            BackColor = backdrop;
            foreach (Label label in new[] { titleLabel, urlLabel, folderLabel, noteLabel, formatLabel, statusLabel, statusArrow })
            {
                label.BackColor = label == titleLabel || label == urlLabel || label == folderLabel || label == noteLabel
                    ? surface : window;
                label.ForeColor = label == noteLabel || label == formatLabel || label == statusLabel ? muted : ink;
            }
            foreach (Panel host in new[] { urlBox, folderBox }) { host.BackColor = field; host.ForeColor = line; }
            foreach (TextBox input in new[] { urlInput, folderInput }) { input.BackColor = field; input.ForeColor = ink; }
            foreach (Button button in new[] { themeButton, minimizeButton, closeButton, pasteButton, browseButton })
            {
                button.BackColor = button == themeButton || button == minimizeButton || button == closeButton ? surface : window;
                button.ForeColor = ink;
                button.FlatAppearance.BorderColor = line;
                button.FlatAppearance.MouseOverBackColor = field;
            }
            startButton.BackColor = ink;
            startButton.ForeColor = window;
            startButton.FlatAppearance.BorderColor = ink;
            themeButton.Text = light ? "DARK" : "LIGHT";
            ascii.Light = light;
            Invalidate();
        }

        private void DragWindow(object sender, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            base.OnMouseDown(args);
            if (args.Y >= 18 && args.Y <= 60 && args.X < 387) DragWindow(this, args);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            Graphics g = args.Graphics;
            Color line = lightTheme ? Color.FromArgb(111, 111, 111) : Color.FromArgb(146, 146, 146);
            Color surface = lightTheme ? Color.FromArgb(237, 237, 237) : Color.FromArgb(26, 26, 26);
            Color window = lightTheme ? Color.FromArgb(250, 250, 250) : Color.FromArgb(14, 14, 14);
            using (SolidBrush fill = new SolidBrush(window)) g.FillRectangle(fill, 20, 17, 520, 566);
            using (SolidBrush fill = new SolidBrush(surface)) g.FillRectangle(fill, 21, 18, 518, 44);
            using (SolidBrush fill = new SolidBrush(surface))
            {
                g.FillRectangle(fill, 41, 257, 478, 83);
                g.FillRectangle(fill, 41, 351, 478, 104);
            }
            using (Pen pen = new Pen(line))
            {
                g.DrawRectangle(pen, 17, 14, 526, 572);
                g.DrawRectangle(pen, 20, 17, 519, 565);
                g.DrawLine(pen, 21, 60, 538, 60);
                g.DrawLine(pen, 21, 62, 538, 62);
                g.DrawLine(pen, 387, 18, 387, 59);
                g.DrawLine(pen, 451, 18, 451, 59);
                g.DrawLine(pen, 495, 18, 495, 59);
                g.DrawRectangle(pen, 40, 256, 479, 84);
                g.DrawRectangle(pen, 40, 350, 479, 105);
                g.DrawLine(pen, 40, 532, 519, 532);
            }
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
            statusTip.SetToolTip(statusLabel, message);
        }

        private void Paste()
        {
            try
            {
                string value = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : "";
                if (value.Length == 0) { SetStatus("Clipboard has no text."); return; }
                urlInput.Text = value;
                urlInput.Focus();
                SetStatus("Playlist URL pasted.");
            }
            catch (Exception error) { SetStatus("Could not read clipboard: " + error.Message); }
        }

        private void Browse()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (System.IO.Directory.Exists(folderInput.Text)) dialog.SelectedPath = folderInput.Text;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                folderInput.Text = dialog.SelectedPath;
                SetStatus("Folder selected.");
            }
        }

        private void DownloadOrStop()
        {
            if (downloading)
            {
                StopDownload();
                startButton.Enabled = false;
                SetStatus("Stopping download…");
                return;
            }
            string url = urlInput.Text.Trim();
            string folder = folderInput.Text.Trim();
            Uri playlist;
            if (!Uri.TryCreate(url, UriKind.Absolute, out playlist) ||
                (playlist.Scheme != Uri.UriSchemeHttp && playlist.Scheme != Uri.UriSchemeHttps))
            { SetStatus("Enter a valid playlist URL."); return; }
            if (folder.Length == 0) { SetStatus("Enter a destination folder."); return; }
            downloading = true;
            startButton.Text = "Stop";
            ascii.State = AsciiDisplay.DisplayState.Process;
            ascii.Invalidate();
            SetStatus("Downloading…");
            StartDownload(++downloadId, url, folder);
        }

        private void Send(object message)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired) { BeginInvoke((Action)delegate { Send(message); }); return; }
            Dictionary<string, object> data = uiJson.Deserialize<Dictionary<string, object>>(uiJson.Serialize(message));
            string type = Convert.ToString(data["type"]);
            if (type == "progress") { SetStatus(Convert.ToString(data["message"])); return; }
            if (type != "result") return;
            downloading = false;
            startButton.Text = "Download";
            startButton.Enabled = true;
            if (!Convert.ToBoolean(data["ok"]))
            {
                ascii.State = AsciiDisplay.DisplayState.Ready;
                ascii.Invalidate();
                SetStatus(Convert.ToString(data["error"]));
                return;
            }
            ascii.State = AsciiDisplay.DisplayState.Complete;
            ascii.Invalidate();
            Dictionary<string, object> value = data["value"] as Dictionary<string, object>;
            int tracks = ResultCount(value, "tracks");
            string summary = tracks > 0
                ? " Lyrics added: " + ResultCount(value, "lyricsAdded") + "/" + tracks + "."
                : " No audio files reported for lyrics.";
            if (ResultCount(value, "lyricsUnavailable") > 0)
                summary += " No LRCLIB match for " + ResultCount(value, "lyricsUnavailable") + ".";
            if (ResultCount(value, "lyricsFailed") > 0)
                summary += " Failed for " + ResultCount(value, "lyricsFailed") + ": " + Convert.ToString(value["lyricsError"]);
            if (value != null && value.ContainsKey("openError") && value["openError"] != null)
                summary += " Could not open folder: " + Convert.ToString(value["openError"]);
            SetStatus("Download complete." + summary);
        }

        private static int ResultCount(Dictionary<string, object> value, string key)
        {
            return value != null && value.ContainsKey(key) ? Convert.ToInt32(value[key]) : 0;
        }

        private void Fail(int id, string error)
        {
            Send(new { type = "result", id = id, ok = false, error = error });
        }
    }
}
