"""Generate a sharp Windows icon from the supplied 16 px pixel art."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "icon.png"
OUTPUT = ROOT / "assets" / "icon.ico"
SIZES = (16, 24, 32, 48, 64, 128, 256)


def main():
    with Image.open(SOURCE) as image:
        source = image.convert("RGBA")
        frames = [source.resize((size, size), Image.Resampling.NEAREST) for size in SIZES]
    frames[-1].save(
        OUTPUT,
        format="ICO",
        sizes=[(size, size) for size in SIZES],
        append_images=frames[:-1],
    )
    print(f"Created {OUTPUT}: {OUTPUT.stat().st_size} bytes")


if __name__ == "__main__":
    main()
