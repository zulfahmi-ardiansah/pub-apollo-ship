# YT Music Download

A small Windows playlist downloader. The fixed-size desktop window uses native Windows controls styled after the [HTML preview](src/index.html), with animated ASCII art and light/dark themes. It has no browser engine or page scrollbars. The window is DPI aware for sharp text at Windows display scaling.

## Run

1. Open the single `dist\YT Music Download.exe` file on Windows 10/11 x64.
2. Ensure .NET Framework 4.7 or newer is installed.
3. Put `yt-dlp.exe`, `ffmpeg.exe`, and `ffprobe.exe` on `PATH`. They can also go beside the EXE. Keep FFmpeg and FFprobe in the same folder.
4. Paste or enter a playlist URL, then select **Browse** or enter a destination folder. Both fields start empty. Select **Download** to begin or **Stop** to cancel.

The window starts in dark mode. Use **LIGHT** in the title bar to switch themes. Text can be selected and copied from the two input fields; labels and other display text cannot be selected.

The command follows [command.txt](command.txt): M4A audio, embedded cover art and metadata (including the playlist track number), English/Japanese/Indonesian subtitles converted to SRT, and playlist-uploader/album/track output folders. After yt-dlp finishes, the app searches [LRCLIB](https://lrclib.net/docs) for lyrics. It checks the returned title, artist, album, and duration before embedding text in the M4A lyrics tag, and retries temporary service errors. Tracks with missing metadata or no reliable match remain unchanged. LRCLIB needs an internet connection and does not require an API key. Any SRT files downloaded by yt-dlp remain separate; they are no longer used as embedded lyrics. You can run the same playlist again to add lyrics to tracks already downloaded.

Download progress appears on one line in the window, with long messages shortened visually. Hover over it to read the full message. When processing completes, the destination folder opens in File Explorer.

The EXE is under 1 MB and does not unpack files on launch. yt-dlp, FFmpeg, and .NET Framework remain external. yt-dlp may also need a JavaScript runtime for YouTube extraction; see [its dependency notes](https://github.com/yt-dlp/yt-dlp#dependencies).

## Build

Run `build.cmd` on Windows. It uses the .NET Framework compiler installed with Windows to compile `src/App.cs`, `src/NativeUi.cs`, and `src/AsciiDisplay.cs` into one x64 EXE in `dist`. The build checks that the EXE stays under 1 MB. `src/index.html` remains a standalone visual preview; the desktop UI is implemented in native controls.
