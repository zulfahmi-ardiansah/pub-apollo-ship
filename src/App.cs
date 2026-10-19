using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ApolloPlaylist
{
    internal sealed partial class PlaylistWindow : Form
    {
        private void StartDownload(int id, string url, string folder)
        {
            if (downloadProcess != null) { Fail(id, "A download is already running."); return; }

            Uri playlist;
            if (!Uri.TryCreate(url, UriKind.Absolute, out playlist) ||
                (playlist.Scheme != Uri.UriSchemeHttp && playlist.Scheme != Uri.UriSchemeHttps))
            { Fail(id, "Enter a valid playlist URL."); return; }
            if (String.IsNullOrWhiteSpace(folder)) { Fail(id, "Enter a destination folder."); return; }

            string ytDlp = FindTool("yt-dlp.exe");
            string ffmpeg = FindTool("ffmpeg.exe");
            string ffprobe = FindTool("ffprobe.exe");
            if (ytDlp == null || ffmpeg == null || ffprobe == null)
            { Fail(id, "Install yt-dlp, ffmpeg, and ffprobe, then add them to PATH."); return; }
            if (!String.Equals(Path.GetDirectoryName(ffmpeg), Path.GetDirectoryName(ffprobe), StringComparison.OrdinalIgnoreCase))
            { Fail(id, "ffmpeg and ffprobe must be in the same folder."); return; }

            string destination;
            try
            {
                destination = Path.GetFullPath(Environment.ExpandEnvironmentVariables(folder.Trim()));
                Directory.CreateDirectory(destination);
            }
            catch (Exception error) { Fail(id, "Folder error: " + error.Message); return; }

            string output = "%(playlist_uploader,uploader|Unknown Artist)s - %(album,playlist_title|Unknown Album)s\\%(playlist_index)02d - %(title)s.%(ext)s";
            string arguments = "-f " + Quote("bestaudio[ext=m4a]/bestaudio")
                + " -x --audio-format m4a --embed-metadata --embed-thumbnail --newline"
                + " --write-subs --write-auto-subs --sub-langs " + Quote("en.*,ja.*,id.*")
                + " --convert-subs srt --parse-metadata " + Quote("%(uploader|)s:%(meta_album_artist)s")
                + " --parse-metadata " + Quote("%(playlist_index)s:%(meta_track)s")
                + " --no-simulate --progress --print " + Quote("after_move:APOLLO_FILE:%(filepath)s")
                + " --ffmpeg-location " + Quote(Path.GetDirectoryName(ffmpeg))
                + " -P " + Quote(destination) + " -o " + Quote(output) + " " + Quote(playlist.AbsoluteUri);

            List<string> completedFiles = new List<string>();
            object filesSync = new object();
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(ytDlp, arguments);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs line)
            {
                if (line.Data != null && line.Data.StartsWith("APOLLO_FILE:", StringComparison.Ordinal))
                {
                    lock (filesSync) completedFiles.Add(line.Data.Substring("APOLLO_FILE:".Length));
                }
                else ShowProcessLine(sender, line);
            };
            process.ErrorDataReceived += ShowProcessLine;
            try
            {
                process.Start();
                lock (processSync) downloadProcess = process;
                downloadId = id;
                cancelRequested = false;
                lastError = "";
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Task.Run(delegate
                {
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    int lyricsAdded = 0, lyricsUnavailable = 0, lyricsFailed = 0;
                    string lyricsError = null;
                    HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (exitCode == 0 && !cancelRequested)
                    {
                        List<string> files;
                        lock (filesSync) files = new List<string>(completedFiles);
                        foreach (string file in files)
                        {
                            if (cancelRequested) break;
                            try
                            {
                                string fullPath = Path.GetFullPath(file);
                                string root = destination.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                                if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                                    || !String.Equals(Path.GetExtension(fullPath), ".m4a", StringComparison.OrdinalIgnoreCase)
                                    || !File.Exists(fullPath) || !seen.Add(fullPath)) continue;
                                ReportProgress("Adding lyrics: " + Path.GetFileName(fullPath));
                                if (EmbedLyrics(ffprobe, ffmpeg, fullPath)) lyricsAdded++;
                                else lyricsUnavailable++;
                            }
                            catch (Exception error)
                            {
                                lyricsFailed++;
                                lyricsError = error.Message;
                            }
                        }
                    }
                    if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)delegate
                    {
                        if (cancelRequested) Fail(id, "Download stopped.");
                        else if (exitCode == 0)
                        {
                            string openError = null;
                            try
                            {
                                Process.Start(new ProcessStartInfo("explorer.exe", Quote(destination)) { UseShellExecute = true });
                            }
                            catch (Exception error) { openError = error.Message; }
                            Send(new { type = "result", id = id, ok = true,
                                value = new { openError = openError, lyricsAdded = lyricsAdded,
                                    lyricsUnavailable = lyricsUnavailable, lyricsFailed = lyricsFailed,
                                    lyricsError = lyricsError, tracks = seen.Count } });
                        }
                        else Fail(id, "Download failed. " + lastError);
                        lock (processSync) downloadProcess = null;
                        process.Dispose();
                    });
                });
            }
            catch (Exception error)
            {
                process.Dispose();
                Fail(id, "Could not start yt-dlp: " + error.Message);
            }
        }

        private void StopDownload()
        {
            lock (processSync)
            {
                cancelRequested = true;
                try { if (downloadProcess != null && !downloadProcess.HasExited) downloadProcess.Kill(); }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
                if (lyricRequest != null) lyricRequest.Abort();
            }
        }

        private void ReportProgress(string message)
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke((Action)delegate { Send(new { type = "progress", id = downloadId, message = message }); });
        }

        private bool EmbedLyrics(string ffprobe, string ffmpeg, string audioPath)
        {
            string infoText = RunTool(ffprobe, "-v error -show_entries "
                + Quote("format=duration:format_tags=title,artist,album,album_artist")
                + " -of json " + Quote(audioPath), true);
            Dictionary<string, object> info = new JavaScriptSerializer()
                .Deserialize<Dictionary<string, object>>(infoText);
            Dictionary<string, object> format = GetObject(info, "format");
            Dictionary<string, object> tags = GetObject(format, "tags");
            string title = GetString(tags, "title");
            string artist = GetString(tags, "artist");
            if (String.IsNullOrWhiteSpace(artist)) artist = GetString(tags, "album_artist");
            string album = GetString(tags, "album");
            double duration;
            if (String.IsNullOrWhiteSpace(title) || String.IsNullOrWhiteSpace(artist)
                || !Double.TryParse(GetString(format, "duration"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out duration) || duration < 1 || duration > 3600)
                return false;
            string lyrics = FetchLyrics(title, artist, album, duration);
            if (String.IsNullOrWhiteSpace(lyrics)) return false;

            string suffix = ".apollo-" + Guid.NewGuid().ToString("N");
            string metadataPath = audioPath + suffix + ".ffmeta";
            string temporaryPath = audioPath + suffix + ".m4a";
            string backupPath = audioPath + suffix + ".bak";
            try
            {
                RunTool(ffmpeg, "-hide_banner -loglevel error -nostdin -i " + Quote(audioPath)
                    + " -f ffmetadata -y " + Quote(metadataPath), false);
                string metadata = File.ReadAllText(metadataPath, Encoding.UTF8);
                int headerEnd = metadata.IndexOf('\n');
                if (headerEnd < 0) throw new InvalidDataException("FFmpeg returned invalid metadata.");
                string entry = "lyrics=" + EscapeMetadata(lyrics) + "\n";
                int chapterStart = metadata.IndexOf("[CHAPTER]", headerEnd + 1, StringComparison.Ordinal);
                int insertAt = chapterStart < 0 ? metadata.Length : chapterStart;
                if (insertAt > 0 && metadata[insertAt - 1] != '\n') entry = "\n" + entry;
                metadata = metadata.Insert(insertAt, entry);
                File.WriteAllText(metadataPath, metadata, new UTF8Encoding(false));
                RunTool(ffmpeg, "-hide_banner -loglevel error -nostdin -i " + Quote(audioPath)
                    + " -f ffmetadata -i " + Quote(metadataPath)
                    + " -map 0 -map_metadata 1 -map_chapters 1 -c copy -f ipod -y " + Quote(temporaryPath), false);
                if (cancelRequested) throw new OperationCanceledException();
                File.Replace(temporaryPath, audioPath, backupPath);
                return true;
            }
            finally
            {
                if (File.Exists(metadataPath)) File.Delete(metadataPath);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                if (File.Exists(backupPath)) File.Delete(backupPath);
            }
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
        {
            object value;
            return source != null && source.TryGetValue(key, out value)
                ? value as Dictionary<string, object> : null;
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            object value;
            return source != null && source.TryGetValue(key, out value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
        }

        private string FetchLyrics(string title, string artist, string album, double duration)
        {
            string primaryArtist = PrimaryArtist(artist);
            string lyrics = BestLyrics(SearchLyrics(title, primaryArtist), title, artist, album, duration);
            if (lyrics == null && !String.Equals(primaryArtist, artist, StringComparison.Ordinal))
                lyrics = BestLyrics(SearchLyrics(title, artist), title, artist, album, duration);
            return lyrics;
        }

        private object[] SearchLyrics(string title, string artist)
        {
            string url = "https://lrclib.net/api/search?track_name=" + Uri.EscapeDataString(title)
                + "&artist_name=" + Uri.EscapeDataString(artist);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = "application/json";
                request.UserAgent = "ApolloPlaylist/1.0";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                lock (processSync)
                {
                    if (cancelRequested) throw new OperationCanceledException();
                    lyricRequest = request;
                }
                try
                {
                    using (WebResponse response = request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        return new JavaScriptSerializer().Deserialize<object[]>(reader.ReadToEnd());
                }
                catch (WebException error)
                {
                    HttpWebResponse response = error.Response as HttpWebResponse;
                    HttpStatusCode status = response != null ? response.StatusCode : 0;
                    string retryAfter = response != null ? response.Headers["Retry-After"] : null;
                    if (response != null) response.Dispose();
                    if (cancelRequested) throw new OperationCanceledException();
                    if (status == HttpStatusCode.NotFound) return new object[0];
                    if (((int)status == 429 || status == HttpStatusCode.ServiceUnavailable)
                        && attempt < 2)
                    {
                        int seconds;
                        if (!Int32.TryParse(retryAfter, out seconds) || seconds < 1) seconds = 1;
                        WaitForLyricsServer(seconds * 1000);
                        continue;
                    }
                    throw new InvalidOperationException("LRCLIB search failed: " + error.Message, error);
                }
                finally
                {
                    lock (processSync)
                    {
                        if (Object.ReferenceEquals(lyricRequest, request)) lyricRequest = null;
                    }
                    WaitForLyricsServer(300);
                }
            }
            return new object[0];
        }

        private void WaitForLyricsServer(int milliseconds)
        {
            while (milliseconds > 0)
            {
                if (cancelRequested) throw new OperationCanceledException();
                int step = Math.Min(milliseconds, 100);
                Thread.Sleep(step);
                milliseconds -= step;
            }
        }

        private static string BestLyrics(object[] results, string title, string artist, string album, double duration)
        {
            string fullArtist = NormalizeArtist(artist);
            string primaryArtist = NormalizeArtist(PrimaryArtist(artist));
            double bestScore = Double.NegativeInfinity;
            string bestLyrics = null;
            foreach (object item in results ?? new object[0])
            {
                Dictionary<string, object> record = item as Dictionary<string, object>;
                if (record == null || NormalizeMatch(GetString(record, "trackName")) != NormalizeMatch(title)) continue;
                string recordArtist = NormalizeArtist(GetString(record, "artistName"));
                bool fullMatch = recordArtist == fullArtist;
                bool primaryMatch = recordArtist == primaryArtist;
                if (!fullMatch && !primaryMatch) continue;
                if (!String.IsNullOrWhiteSpace(album)
                    && NormalizeMatch(GetString(record, "albumName")) != NormalizeMatch(album)) continue;
                double recordDuration;
                if (!Double.TryParse(GetString(record, "duration"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out recordDuration)) continue;
                double difference = Math.Abs(recordDuration - duration);
                if (difference > 2) continue;
                object instrumental;
                if (record.TryGetValue("instrumental", out instrumental) && Convert.ToBoolean(instrumental)) continue;
                string lyrics = GetString(record, "plainLyrics");
                if (String.IsNullOrWhiteSpace(lyrics)) lyrics = PlainLyricsFromLrc(GetString(record, "syncedLyrics"));
                if (String.IsNullOrWhiteSpace(lyrics)) continue;
                double score = (fullMatch ? 10 : 0) - difference;
                if (score > bestScore) { bestScore = score; bestLyrics = lyrics.Trim(); }
            }
            return bestLyrics;
        }

        private static string PrimaryArtist(string artist)
        {
            int comma = artist.IndexOf(',');
            return comma > 0 ? artist.Substring(0, comma).Trim() : artist;
        }

        private static string NormalizeArtist(string artist)
        {
            return Regex.Replace(Regex.Replace(NormalizeMatch(artist), @"\b(feat|featuring|ft)\b", " "),
                @"\s+", " ").Trim();
        }

        private static string NormalizeMatch(string value)
        {
            return Regex.Replace((value ?? "").Normalize(NormalizationForm.FormKC).ToLowerInvariant(),
                @"[\p{P}\p{Z}\s]+", " ").Trim();
        }

        private static string PlainLyricsFromLrc(string lrc)
        {
            if (String.IsNullOrWhiteSpace(lrc)) return null;
            StringBuilder result = new StringBuilder();
            foreach (string raw in lrc.Split('\n'))
            {
                string line = Regex.Replace(raw.Trim(), @"^(?:\[\d{1,3}:\d{2}(?:\.\d+)?\])+", "").Trim();
                if (line.Length == 0 || line.StartsWith("[", StringComparison.Ordinal)) continue;
                if (result.Length > 0) result.Append('\n');
                result.Append(line);
            }
            return result.Length > 0 ? result.ToString() : null;
        }

        private static string EscapeMetadata(string value)
        {
            return value.Replace("\\", "\\\\").Replace("=", "\\=").Replace(";", "\\;")
                .Replace("#", "\\#").Replace("\n", "\\\n");
        }

        private string RunTool(string executable, string arguments, bool captureOutput)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(executable, arguments);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.RedirectStandardOutput = captureOutput;
                if (captureOutput) process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                lock (processSync)
                {
                    if (cancelRequested) throw new OperationCanceledException();
                    process.Start();
                    downloadProcess = process;
                }
                try
                {
                    string output = captureOutput ? process.StandardOutput.ReadToEnd() : null;
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (cancelRequested) throw new OperationCanceledException();
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException(Path.GetFileName(executable) + " failed: " + error.Trim());
                    return output;
                }
                finally
                {
                    lock (processSync)
                    {
                        if (Object.ReferenceEquals(downloadProcess, process)) downloadProcess = null;
                    }
                }
            }
        }

        private void ShowProcessLine(object sender, DataReceivedEventArgs args)
        {
            if (String.IsNullOrWhiteSpace(args.Data) || IsDisposed || !IsHandleCreated) return;
            string line = args.Data.Trim();
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)) lastError = line;
                    Send(new { type = "progress", id = downloadId, message = line });
                });
            }
            catch (InvalidOperationException) { }
        }

        private static string FindTool(string filename)
        {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            if (File.Exists(local)) return local;
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string directory in path.Split(Path.PathSeparator))
            {
                if (String.IsNullOrWhiteSpace(directory)) continue;
                try
                {
                    string candidate = Path.Combine(directory.Trim('"'), filename);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { }
            }
            return null;
        }

        private static string Quote(string value)
        {
            StringBuilder result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in value)
            {
                if (character == '\\') { slashes++; continue; }
                if (character == '"')
                {
                    result.Append('\\', slashes * 2 + 1);
                    result.Append('"');
                    slashes = 0;
                    continue;
                }
                result.Append('\\', slashes);
                slashes = 0;
                result.Append(character);
            }
            result.Append('\\', slashes * 2);
            result.Append('"');
            return result.ToString();
        }
    }
}
