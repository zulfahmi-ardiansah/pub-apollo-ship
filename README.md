# YT Music Download

A small Windows playlist downloader. The fixed-size desktop window renders [src/index.html](src/index.html), so the EXE and HTML preview use the same layout, animation, and light/dark themes. The window is DPI aware for sharp text at Windows display scaling. Keyboard, mouse-wheel, and pinch zoom are disabled in the desktop app.

## Run

1. Open the single `dist\YT Music Download.exe` file on Windows 10/11 x64.
2. Install the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) if it is missing. The app also needs .NET Framework 4.7 or newer.
3. Put `yt-dlp.exe`, `ffmpeg.exe`, and `ffprobe.exe` on `PATH`. They can also go beside the EXE. Keep FFmpeg and FFprobe in the same folder.
4. Paste or enter a playlist URL, then select **Browse** or enter a destination folder. Both fields start empty. Select **Download** to begin or **Stop** to cancel.

The window starts in dark mode. Use **LIGHT** in the title bar to switch themes. Text can be selected and copied from the two input fields; labels and other display text cannot be selected.

The command follows [command.txt](command.txt): M4A audio, embedded cover art and metadata (including the playlist track number), English/Japanese/Indonesian subtitles converted to SRT, and playlist-uploader/album/track output folders. After yt-dlp finishes, the app searches [LRCLIB](https://lrclib.net/docs) for lyrics. It checks the returned title, artist, album, and duration before embedding text in the M4A lyrics tag, and retries temporary service errors. Tracks with missing metadata or no reliable match remain unchanged. LRCLIB needs an internet connection and does not require an API key. Any SRT files downloaded by yt-dlp remain separate; they are no longer used as embedded lyrics. You can run the same playlist again to add lyrics to tracks already downloaded.

Download progress appears on one line in the window, with long messages shortened visually. When processing completes, the destination folder opens in File Explorer.

The EXE is under 1 MB. On first launch, it unpacks its HTML and WebView2 SDK files into `%LOCALAPPDATA%\ApolloPlaylist\Cache`. yt-dlp, FFmpeg, .NET Framework, and the shared WebView2 Runtime remain external. yt-dlp may also need a JavaScript runtime for YouTube extraction; see [its dependency notes](https://github.com/yt-dlp/yt-dlp#dependencies).

## Build

Run `build.cmd` on Windows. It uses the .NET Framework compiler installed with Windows and embeds `src/index.html` and the WebView2 SDK files from `vendor` into one x64 EXE in `dist`. The build checks that the EXE stays under 1 MB.

The vendored DLLs come from [Microsoft.Web.WebView2 1.0.4258.31](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31). Keep their version aligned if you update them.
