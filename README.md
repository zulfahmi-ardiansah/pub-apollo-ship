# YT Music Download

A small Windows app that saves YouTube Music playlists as local M4A files for listening in your own music player.

![Language](https://img.shields.io/badge/language-C%23-239120?style=flat-square&logo=csharp&logoColor=white)
![Version](https://img.shields.io/badge/version-unversioned-6B7280?style=flat-square)

## Why I built it

I use YouTube Premium and sometimes want to hear my playlists offline through Poweramp with my custom equalizer. Rather than build another player, I made a downloader that saves the music to a folder. Its fixed window borrows the animated ASCII art and light/dark styling of old keygen programs.

The app takes a playlist URL; it does not sign in to YouTube or use a Premium session. Downloading is handled by an external `yt-dlp.exe` installation.

## What it does

- Saves playlist audio as M4A with cover art, metadata, and playlist track numbers.
- Writes English, Japanese, and Indonesian subtitles as separate SRT files when yt-dlp finds them.
- After yt-dlp finishes, looks up lyrics for each reported M4A on LRCLIB and embeds plain lyrics when the title, artist, album, and duration match. A missing match leaves the audio file unchanged.
- Shows one-line progress, supports stopping a download, and opens the destination folder in File Explorer when processing completes.
- Uses a fixed-size native Windows window with a dark default theme, a light theme, and animated ASCII art. The app EXE does not need a browser runtime.

## Getting started

### Requirements

- Windows 10 or 11, x64, with .NET Framework 4.7 or newer.
- [`yt-dlp.exe`](https://github.com/yt-dlp/yt-dlp/releases), `ffmpeg.exe`, and `ffprobe.exe` from an [FFmpeg build](https://ffmpeg.org/download.html). The app looks beside its EXE first, then on `PATH`. FFmpeg and FFprobe must be in the same folder.
- Internet access for the playlist download and LRCLIB lyric lookup. For full YouTube support, yt-dlp also needs a supported JavaScript runtime; its [EJS setup guide](https://github.com/yt-dlp/yt-dlp/wiki/EJS) recommends Deno.

### Install

1. Use the included [Windows EXE](dist/YT%20Music%20Download.exe), or [build it from source](#build-from-source).
2. Place `yt-dlp.exe`, `ffmpeg.exe`, and `ffprobe.exe` beside the app EXE, or add their folders to `PATH`. Keep FFmpeg and FFprobe together.
3. Set up the JavaScript runtime according to the yt-dlp guide above if it is not already available.

The app itself needs no account, API key, or configuration file. Both input fields start empty.

## Use it

From the repository root in PowerShell, start the desktop app:

```powershell
& ".\dist\YT Music Download.exe"
```

1. Paste a YouTube Music playlist URL into **Playlist URL**. The **Paste** button reads the clipboard.
2. Choose a destination with **Browse**, or type a folder path in **Save to folder**.
3. Select **Download**. The button changes to **Stop** while processing. The status line shortens long messages; hover over it to read the full text.
4. When download and lyric processing finish, File Explorer opens the destination folder. Look for M4A files under the playlist uploader and album folders.

The output follows [command.txt](command.txt). Within the destination folder, the path is:

```text
<playlist uploader> - <album>\<track number> - <title>.m4a
```

The album can fall back to the playlist title, and a missing uploader can fall back to `Unknown Artist`. Subtitle SRT files stay separate from the audio. Embedded lyrics are plain text and depend on a reliable LRCLIB match; the presence or display of lyrics in a player is not guaranteed.

## Build from source

Run this from the repository root in PowerShell:

```powershell
.\build.cmd
```

The script uses the Windows .NET Framework compiler and creates one x64 EXE in `dist`. It fails if the EXE exceeds 1 MB. The desktop UI is implemented in [NativeUi.cs](src/NativeUi.cs) and [AsciiDisplay.cs](src/AsciiDisplay.cs); [App.cs](src/App.cs) runs yt-dlp and embeds lyrics. [index.html](src/index.html) is a standalone visual preview and is not loaded by the EXE.

There is no automated test suite in this repository. A successful build confirms compilation and the EXE size check; it does not test a live playlist download.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| The app asks for yt-dlp, FFmpeg, or FFprobe | Confirm the three EXEs are beside the app EXE or on `PATH`, then start the app again. |
| The app says FFmpeg and FFprobe must be in the same folder | Move `ffmpeg.exe` and `ffprobe.exe` into one folder, then retry. |
| yt-dlp reports a YouTube JavaScript challenge | Follow the [yt-dlp EJS setup guide](https://github.com/yt-dlp/yt-dlp/wiki/EJS) for a supported JavaScript runtime and its solver components. |
| Music downloads but lyrics are missing | Check the final status for **No LRCLIB match** or **Failed**. SRT subtitles do not become embedded lyrics; the app only adds them when LRCLIB returns a close metadata match. |
