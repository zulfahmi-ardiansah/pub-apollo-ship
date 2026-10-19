# The story of Apollo Ship

Apollo Ship began with a command, not an app. I use YouTube Premium, but sometimes I want to listen offline in Poweramp with my own equalizer. I did not want to build another player. I wanted a small Windows window that could save a YouTube Music playlist as local M4A files, using the behavior already written in [command.txt](command.txt).

The other half of the idea was visual. Old keygen programs had tiny, strange windows, sharp borders, ASCII art, and motion that made them memorable. I wanted that feeling around an ordinary playlist downloader.

## Finding the window

We started with an HTML template and reference images of old keygen interfaces. The early layout had too much on it, so we kept cutting: one playlist URL, one destination folder, a Download button, and a short status line. The playlist heading moved into the title bar. We kept only close and minimize controls there, then placed the light and dark switch beside them.

The window became fixed-size, with no resize or zoom. Dark mode became the default. Labels were made non-selectable while the two inputs kept normal copy and paste. We added a Paste button for the URL, Browse for the folder, animated ASCII art, and a concise progress line that truncates long messages instead of creating a horizontal scrollbar. When processing finishes, the destination opens in File Explorer.

The design took several passes because a plain black form did not feel like the references. Thin double borders, scan lines, monospace type, and the moving bar pattern gave it a more specific identity. The HTML design survives in [src/index.html](src/index.html) as a standalone visual preview.

## Trying Electron, then setting a size limit

Electron was the first desktop direction we explored. It let us keep the HTML, CSS, and JavaScript interface, and an early implementation wired a renderer to the downloader through a preload bridge. It also explored synthesized chiptune audio. Source checks passed, but the Electron runtime did not install correctly in that environment, so we never verified a working portable Electron build.

Then the packaging target became clear: the launcher should be under **1 MB**, with yt-dlp and FFmpeg installed separately. That ruled out the Electron package we had been considering. We moved to a .NET Framework launcher with WebView2: the EXE could be small while the web engine was supplied by a separate runtime. The first local Git snapshot, `77266e3`, preserves that WebView2 version. Its EXE was 987,648 bytes, just under the limit.

WebView2 kept the HTML design close to the running app, but the result did not look as sharp or as faithful to the preview as we wanted. It also carried the memory cost of a browser-based window. We revised the borders and title controls, but the desktop technology was still at odds with the small-app goal.

## Making the download useful

The downloader kept [command.txt](command.txt) as its reference for M4A audio, cover art, metadata, playlist track numbers, and English, Japanese, and Indonesian SRT subtitles. When that command changed, we updated the app arguments to follow its track-number and output-folder rules.

Lyrics needed a separate path. The SRT files from YouTube Music were not a reliable way to get lyrics into the music file. We tried the M4A metadata route instead: after yt-dlp reports a finished track, Apollo Ship checks LRCLIB for a close title, artist, album, and duration match, then uses FFmpeg to embed plain lyrics. Tracks without a match keep their audio. The sidecar SRT files remain separate, and a player's ability to display embedded lyrics still varies.

## Returning to native Windows controls

Before changing the desktop implementation again, we initialized local Git and saved the WebView2 version. Then we replaced the browser window with a WinForms interface in [src/NativeUi.cs](src/NativeUi.cs), while [src/AsciiDisplay.cs](src/AsciiDisplay.cs) drew the ASCII display. The downloader and lyric work stayed in [src/App.cs](src/App.cs).

That change removed the browser runtime from the running UI and made the output much smaller: the first native EXE was 36,352 bytes. We kept the fixed window, themes, ASCII animation, custom title bar, Paste and Browse controls, and progress behavior. Later passes simplified the title-bar button borders, named the app **Apollo Ship**, added the Ready / Process / Complete indicator, and renamed the packaged file to `Apollo-Ship.exe`.

## The memory investigation

The first memory concern was the browser-based version, but we also noticed the native app's memory number rising after launch. We first reused fonts and pens and precomputed the animated bar text. That reduced allocations, but the number still rose, so we compared the same window with its animation running and stopped.

Over 40 seconds, the animation-on probe's managed heap grew by about 4 MB; the animation-off probe grew by about 1 MB. We then cached the static display and drew only the moving bars each frame. In the same probe, animation-on growth fell to about 1.9 MB. An accelerated run showed the managed heap dropping from 6.4 MB to 1.4 MB when garbage collection ran. That showed temporary redraw allocations being reclaimed; it did not establish an unbounded leak or promise a flat Task Manager reading from the first second.

## Where it stands

Apollo Ship is now a single [Windows EXE](dist/Apollo-Ship.exe) built by [build.cmd](build.cmd). The current file is 37,888 bytes, below the 1 MB launcher target. yt-dlp, FFmpeg, and FFprobe remain external tools. The native app is the runnable product; the HTML file is a design preview. [README.md](README.md) covers setup and use, while this file records why the app took this route.
