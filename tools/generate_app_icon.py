"""Generate WinTouchpad tray/app icon: teal rounded square + centered white '3' -> PNG + multi-size ICO."""
from __future__ import annotations

import struct
from io import BytesIO
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "WinTouchpad.App" / "Assets"
TEAL = (0, 180, 170, 255)  # #00B4AA
WHITE = (255, 255, 255, 255)
SIZES = (16, 24, 32, 48, 64, 128, 256)


def rounded_rect_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def render_master(canvas: int = 256) -> Image.Image:
    """Transparent canvas, teal squircle nearly filling it, optically centered '3'."""
    img = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    pad = max(2, canvas // 10)
    inner = canvas - pad * 2
    radius = max(2, inner * 22 // 100)

    squircle = Image.new("RGBA", (inner, inner), (0, 0, 0, 0))
    base = Image.new("RGBA", (inner, inner), TEAL)
    squircle.paste(base, (0, 0), rounded_rect_mask(inner, radius))

    draw = ImageDraw.Draw(squircle)
    font_size = max(10, int(inner * 0.62))
    font = None
    for path in (
        r"C:\Windows\Fonts\segoeuib.ttf",
        r"C:\Windows\Fonts\seguisb.ttf",
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\arial.ttf",
    ):
        try:
            font = ImageFont.truetype(path, font_size)
            break
        except OSError:
            continue
    if font is None:
        font = ImageFont.load_default()

    text = "3"
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = (inner - tw) / 2 - bbox[0]
    y = (inner - th) / 2 - bbox[1] - th * 0.03
    draw.text((x, y), text, font=font, fill=WHITE)
    img.paste(squircle, (pad, pad), squircle)
    return img


def png_bytes(im: Image.Image) -> bytes:
    buf = BytesIO()
    im.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


def write_ico(path: Path, images: list[Image.Image]) -> None:
    """ICO with PNG-compressed images (Vista+), one directory entry per size."""
    count = len(images)
    offset = 6 + 16 * count
    entries: list[tuple[int, int, int, int]] = []
    blobs: list[bytes] = []
    for im in images:
        data = png_bytes(im)
        w, h = im.size
        entry_w = 0 if w >= 256 else w
        entry_h = 0 if h >= 256 else h
        entries.append((entry_w, entry_h, len(data), offset))
        blobs.append(data)
        offset += len(data)

    out = bytearray()
    out += struct.pack("<HHH", 0, 1, count)
    for w, h, size, off in entries:
        out += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, size, off)
    for b in blobs:
        out += b
    path.write_bytes(out)


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    master = render_master(256)
    (ASSETS / "app.png").write_bytes(png_bytes(master))
    frames = [master.resize((s, s), Image.Resampling.LANCZOS) for s in SIZES]
    write_ico(ASSETS / "app.ico", frames)

    raw = (ASSETS / "app.ico").read_bytes()
    reserved, itype, count = struct.unpack_from("<HHH", raw, 0)
    print(f"Wrote {ASSETS / 'app.png'} ({master.size})")
    print(f"Wrote {ASSETS / 'app.ico'} type={itype} count={count} bytes={len(raw)}")
    off = 6
    for i in range(count):
        w, h, cc, res, planes, bpp, size, o = struct.unpack_from("<BBBBHHII", raw, off)
        print(f"  [{i}] dir={w}x{h} bpp={bpp} size={size}")
        off += 16

    check = Image.open(ASSETS / "app.ico")
    print("Pillow n_frames", getattr(check, "n_frames", 1))
    for i in range(getattr(check, "n_frames", 1)):
        check.seek(i)
        print(f"  pillow[{i}] {check.size}")


if __name__ == "__main__":
    main()
