"""Generate the fixed dark Apollo display used by the native app and HTML preview."""

from math import pi, sin
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "src" / "ascii.gif"
STILL = ROOT / "src" / "ascii.png"
FONT = Path("C:/Windows/Fonts/consola.ttf")
BOLD_FONT = Path("C:/Windows/Fonts/consolab.ttf")
WIDTH, HEIGHT = 480, 161
LOGO = (
    "    _    ____   ___  _     _      ___",
    "   / \\  |  _ \\ / _ \\| |   | |    / _ \\",
    "  / _ \\ | |_) | | | | |   | |   | | | |",
    " / ___ \\|  __/| |_| | |___| |___| |_| |",
    "/_/   \\_\\_|    \\___/|_____|_____\\___/",
)


def make_frame(step, meta_font, logo_font):
    image = Image.new("RGB", (WIDTH, HEIGHT), (8, 8, 8))
    draw = ImageDraw.Draw(image)
    for y in range(3, HEIGHT, 3):
        draw.line((2, y, WIDTH - 3, y), fill=(20, 20, 20))
    draw.rectangle((0, 0, WIDTH - 1, HEIGHT - 1), outline=(146, 146, 146))
    draw.rectangle((3, 3, WIDTH - 4, HEIGHT - 4), outline=(146, 146, 146))
    draw.text((9, 7), "Youtube Music Downloader", font=meta_font, fill=(194, 194, 194))
    for row, line in enumerate(LOGO):
        width = draw.textlength(line, font=logo_font)
        draw.text(((WIDTH - width) / 2, 29 + row * 15), line, font=logo_font, fill=(244, 244, 244))
    bar_spacing = 8
    bar_start = (WIDTH - 31 * bar_spacing) // 2
    for col in range(32):
        wave = sin(col * .48 + step * pi / 8)
        echo = sin(col * .91 - step * pi / 16)
        height = max(0, min(4, round(2 + wave + echo)))
        for row in range(4 - height, 4):
            x = bar_start + col * bar_spacing
            y = 109 + row * 12
            draw.line((x, y + 2, x, y + 10), fill=(194, 194, 194))
    return image


def main():
    meta_font = ImageFont.truetype(BOLD_FONT, 10)
    logo_font = ImageFont.truetype(BOLD_FONT, 15)
    frames = [make_frame(step, meta_font, logo_font) for step in range(32)]
    frames[0].save(STILL, optimize=True)
    frames[0].save(
        OUTPUT,
        save_all=True,
        append_images=frames[1:],
        duration=150,
        loop=0,
        optimize=True,
        disposal=1,
    )
    print(f"Created {OUTPUT}: {OUTPUT.stat().st_size} bytes")


if __name__ == "__main__":
    main()
