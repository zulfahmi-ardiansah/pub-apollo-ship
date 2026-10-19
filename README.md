# Apollo Ship

**Apollo Ship** is a small Windows app that saves YouTube Music playlists as local M4A files for listening in your own music player.

![Language](https://img.shields.io/badge/language-C%23-239120?style=flat-square&logo=csharp&logoColor=white)
![Version](https://img.shields.io/badge/version-unversioned-6B7280?style=flat-square)

I use YouTube Premium, but sometimes I want to listen offline in Poweramp with my custom equalizer. I wanted the files for the player I already use, so I built a downloader instead of another music player. The fixed window and animated ASCII art bring back the feel of old keygen programs.

Apollo Ship accepts a playlist URL. It does not sign in to YouTube or use a Premium session; an external `yt-dlp.exe` handles the download.

## How it works

1. Enter a YouTube Music playlist URL and choose a destination folder.
2. `yt-dlp` downloads the audio as M4A, adds cover art and metadata, and writes playlist track numbers. If available, it saves English, Japanese, and Indonesian subtitles as separate SRT files.
3. Apollo Ship looks up each downloaded track on LRCLIB. When the title, artist, album, and duration match, it uses FFmpeg to embed plain lyrics in the M4A. A track without a match keeps its audio unchanged.
4. When processing finishes, the destination opens in File Explorer.

The window shows one-line progress and lets you stop a download. It has a fixed size, a dark default theme, a light theme, and animated ASCII art. The EXE uses native Windows controls, so it does not need a browser runtime.

## Requirements

- Windows 10 or 11, x64, with .NET Framework 4.7 or newer.
- [`yt-dlp.exe`](https://github.com/yt-dlp/yt-dlp/releases), `ffmpeg.exe`, and `ffprobe.exe` from an [FFmpeg build](https://ffmpeg.org/download.html). The app looks beside its EXE first, then on `PATH`. FFmpeg and FFprobe must be in the same folder.
- Internet access for the playlist download and LRCLIB lyric lookup. For full YouTube support, yt-dlp also needs a supported JavaScript runtime; its [EJS setup guide](https://github.com/yt-dlp/yt-dlp/wiki/EJS) recommends Deno.

## Installation

1. Use the included [Windows EXE](dist/Apollo-Ship.exe), or [build it from source](#build-from-source).
2. Place `yt-dlp.exe`, `ffmpeg.exe`, and `ffprobe.exe` beside the app EXE, or add their folders to `PATH`. Keep FFmpeg and FFprobe together.
3. Set up the JavaScript runtime according to the yt-dlp guide above if it is not already available.

The app itself needs no account, API key, or configuration file. Both input fields start empty.

## Usage

From the repository root in PowerShell, start the desktop app:

```powershell
& ".\dist\Apollo-Ship.exe"
```

1. Paste a YouTube Music playlist URL into **Playlist URL**. The **Paste** button reads the clipboard.
2. Choose a destination with **Browse**, or type a folder path in **Save to folder**.
3. Select **Download**. The button changes to **Stop** while processing. The status line shortens long messages; hover over it to read the full text.
4. When download and lyric processing finish, File Explorer opens the destination folder. Look for M4A files under the playlist uploader and album folders.

## Output structure

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

The script uses the Windows .NET Framework compiler and creates one x64 EXE in `dist`. It fails if the EXE exceeds 1 MB.

There is no automated test suite in this repository. A successful build confirms compilation and the EXE size check; it does not test a live playlist download.

## Project structure

| Path | Purpose |
| --- | --- |
| [src/NativeUi.cs](src/NativeUi.cs) | Native Windows window and controls. |
| [src/AsciiDisplay.cs](src/AsciiDisplay.cs) | Animated ASCII display. |
| [src/App.cs](src/App.cs) | Download process and lyric embedding. |
| [src/index.html](src/index.html) | Standalone visual preview; the EXE does not load it. |
| [command.txt](command.txt) | Reference yt-dlp command and output naming. |
| [build.cmd](build.cmd) | Compiles the EXE and checks its size. |

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| The app asks for yt-dlp, FFmpeg, or FFprobe | Confirm the three EXEs are beside the app EXE or on `PATH`, then start the app again. |
| The app says FFmpeg and FFprobe must be in the same folder | Move `ffmpeg.exe` and `ffprobe.exe` into one folder, then retry. |
| yt-dlp reports a YouTube JavaScript challenge | Follow the [yt-dlp EJS setup guide](https://github.com/yt-dlp/yt-dlp/wiki/EJS) for a supported JavaScript runtime and its solver components. |
| Music downloads but lyrics are missing | Check the final status for **No LRCLIB match** or **Failed**. SRT subtitles do not become embedded lyrics; the app only adds them when LRCLIB returns a close metadata match. |
