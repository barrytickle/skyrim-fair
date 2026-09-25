"""The paper lanterns' colours: recoloured paper textures (also their glow maps), as DDS.

    build/texvenv/Scripts/python tools/make_lantern_colours.py        (numpy, Pillow)

Astra's two lanterns (assets/Skyrim_Paper_Lanterns_Authoring, meshes\\barry_paper_lanterns\\)
each have one paper texture that is also the Glow shader's glow map, so a recoloured paper
glows in its own colour. Each paper runs from its colour at the edges (the tall one red, the
round one blue) to a warm core where the light shows through, with motifs in the colour.

For every colour and shape, each pixel is moved from the paper's own colour toward the new
one by how much of the paper's colour it carries: its share, by hue, between the paper's
colour and the warm core. The core stays warm, the motifs follow the colour, and the pale
ribs barely change. The new colour is mixed in RGB, so the fade from colour to core goes
through paler tints (as the blue paper's does), not through every hue between.

Writes textures\\barry_paper_lanterns\\lantern_<shape>_<colour>_d.dds (BC1, 1024, full mip
chain, as Astra's) and build/lanterns/<shape>_<colour>.png for the previews. Each shape's own
colour is Astra's texture as it is (lantern_tall_red_d.dds, lantern_round_blue_d.dds, which her
NIFs name): never written here.
"""
import io
import pathlib
import struct

import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = ROOT / "assets" / "Skyrim_Paper_Lanterns_Authoring" / "source_textures"
OUT = ROOT / "assets" / "textures" / "barry_paper_lanterns"
PREVIEW = ROOT / "build" / "lanterns"
SIZE = 1024

# Each shape: its paper's own colour (hue, degrees) and its warm core's.
SHAPES = {
    "tall": {"source": "lantern_tall_red_d.png", "base": 3.0, "core": 48.0, "own": "red", "boost": 1.0},
    # Astra's blue is a soft one: other colours from it come out washed, so more colour.
    "round": {"source": "lantern_round_blue_d.png", "base": 205.0, "core": 47.0, "own": "blue", "boost": 1.35},
}

# The colours (hue in degrees; saturation and brightness scales for the coloured part).
COLOURS = {
    "red": (2.0, 1.0, 1.0),
    "orange": (24.0, 1.0, 1.0),
    "yellow": (46.0, 0.95, 1.05),
    "green": (118.0, 0.85, 0.95),
    "blue": (208.0, 1.0, 1.0),
    "purple": (278.0, 0.8, 0.95),
}


def circular(a, b):
    """Hue distance in degrees, 0..180."""
    d = np.abs(a - b) % 360.0
    return np.minimum(d, 360.0 - d)


def hsv_to_rgb(h, s, v):
    h = (h % 360.0) / 60.0
    c = v * s
    x = c * (1 - np.abs(h % 2 - 1))
    z = np.zeros_like(h)
    i = np.floor(h).astype(int) % 6
    r = np.choose(i, [c, x, z, z, x, c])
    g = np.choose(i, [x, c, c, x, z, z])
    b = np.choose(i, [z, z, x, c, c, x])
    m = v - c
    return np.stack([r + m, g + m, b + m], -1)


def recolour(rgb, base, core, target, boost=1.0):
    """rgb in 0..1. The pixels' own colour moved from `base` toward `target`, by how much of it they carry."""
    hue_target, sat_scale, val_scale = target
    sat_scale *= boost
    hsv = np.asarray(Image.fromarray((rgb * 255).astype(np.uint8)).convert("HSV")).astype(float) / 255.0
    h, s, v = hsv[..., 0] * 360.0, hsv[..., 1], hsv[..., 2]
    to_base, to_core = circular(h, base), circular(h, core)
    carries = to_core / np.maximum(to_base + to_core, 1e-6)          # 1 at the paper's colour, 0 at the core
    carries = np.clip((carries - 0.1) / 0.8, 0.0, 1.0) * np.clip(s / 0.25, 0.0, 1.0)  # pale pixels barely move
    moved_hue = hue_target + (h - base + 180.0) % 360.0 - 180.0      # keep each pixel's offset from the base
    moved = hsv_to_rgb(moved_hue, np.clip(s * sat_scale, 0, 1), np.clip(v * val_scale, 0, 1))
    w = carries[..., None]
    mixed = rgb * (1 - w) + moved * w
    # Keep the fade bright: a mix of opposite colours (blue into the warm core) goes grey-brown,
    # so each pixel is lifted back to the brighter of its two sources, and the fade passes
    # through paler tints, as Astra's own blue-to-cream does.
    bright = np.maximum(rgb.max(-1), moved.max(-1))[..., None]
    mixed = mixed * (bright / np.maximum(mixed.max(-1, keepdims=True), 1e-6))
    return np.clip(mixed, 0.0, 1.0)


def dds_bc1_mipped(img: Image.Image, fourcc: str = "DXT1") -> bytes:
    """A BC1 (DXT1) DDS, or DXT5 for alpha, with every mip level down to 1x1, each level encoded by Pillow."""
    levels = []
    level = img
    while True:
        buf = io.BytesIO()
        level.save(buf, "DDS", pixel_format=fourcc)
        data = buf.getvalue()
        levels.append(data[128:])  # Pillow writes a plain 128-byte header for DXT1 and DXT5
        if level.size == (1, 1):
            break
        level = level.resize((max(1, level.width // 2), max(1, level.height // 2)), Image.LANCZOS)
    w, h = img.size
    flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x20000 | 0x80000       # caps, height, width, pixelformat, mipmapcount, linearsize
    pixel_format = struct.pack("<II4s5I", 32, 0x4, fourcc.encode(), 0, 0, 0, 0, 0)
    caps = 0x8 | 0x1000 | 0x400000                              # complex, texture, mipmap
    header = struct.pack("<7I44x", 124, flags, h, w, len(levels[0]), 0, len(levels)) + pixel_format + struct.pack("<4I4x", caps, 0, 0, 0)
    assert len(header) == 124
    return b"DDS " + header + b"".join(levels)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for shape, spec in SHAPES.items():
        src = Image.open(SOURCE / spec["source"]).convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)
        rgb = np.asarray(src).astype(float) / 255.0
        for colour, target in COLOURS.items():
            if colour == spec["own"]:
                src.save(PREVIEW / f"{shape}_{colour}.png")  # Astra's own, as it is
                continue
            out = recolour(rgb, spec["base"], spec["core"], target, spec["boost"])
            img = Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGB")
            img.save(PREVIEW / f"{shape}_{colour}.png")
            (OUT / f"lantern_{shape}_{colour}_d.dds").write_bytes(dds_bc1_mipped(img))
            print(f"lantern_{shape}_{colour}_d.dds")


if __name__ == "__main__":
    main()
