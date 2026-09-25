"""Build the avenue's cobble texture set from Bethesda's own Whiterun stone floor.

    python tools/make_cobble.py --data "<Skyrim SE Data with the vanilla Skyrim - Textures*.bsa>"

Needs numpy and Pillow (build/texvenv has both: py -3.13 -m venv build/texvenv, then
build/texvenv/Scripts/python -m pip install numpy pillow).

Writes assets/textures/SkyrimFair/Ground/ (git-ignored build output):
- Cobble01.dds, Cobble01_n.dds: vanilla wrstonefloor01's diffuse and normal map, byte for
  byte, under the fair's own path, so a Whiterun texture replacer can't pull them out of
  line with the height map.
- Cobble01_p.dds: the fair's own parallax (height) map, white high, as an uncompressed 8-bit
  luminance DDS with a full mip chain. It's integrated from the vanilla normal map's slopes
  (Frankot-Chellappa, in the Fourier domain: the texture tiles, so it wraps without a seam),
  the broad undulation taken out, a little of the diffuse's light and dark added for grain.
  The normal map's green channel convention is picked by which reading agrees with the
  diffuse (the mortar is dark, and low).

The vanilla texture archives must be Bethesda's: a modlist's may be a replacer's (Vanilla
Remastered's in Barry's), so point --data at the Steam install.
"""
import argparse
import io
import pathlib
import struct
import sys

import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
import bsa_extract  # noqa: E402

SOURCE = "textures/architecture/whiterun/wrstonefloor01"
OUT = ROOT / "assets" / "textures" / "SkyrimFair" / "Ground"

# Features broader than this share of the texture are taken out of the height (the
# integration's slow drift); the stones themselves are well under it.
BROAD = 0.12
# How much of the diffuse's light and dark goes into the height.
GRAIN = 0.15
# Height percentiles mapped to black and white.
LOW, HIGH = 1.0, 95.0


def high_pass(img, broad):
    """Take out features wider than `broad` of the texture (a Gaussian in the Fourier domain)."""
    h, w = img.shape
    v = np.fft.fftfreq(h)[:, None]
    u = np.fft.fftfreq(w)[None, :]
    k0 = 1.0 / (broad * max(w, h))
    keep = 1.0 - np.exp(-(u * u + v * v) / (k0 * k0))
    return np.real(np.fft.ifft2(np.fft.fft2(img) * keep))


def integrate(normal, green_sign):
    """Height from a tangent-space normal map: the slopes' least-squares surface, periodic."""
    nx = normal[..., 0] * 2.0 - 1.0
    ny = (normal[..., 1] * 2.0 - 1.0) * green_sign
    nz = np.sqrt(np.clip(1.0 - nx * nx - ny * ny, 0.05, 1.0))
    p = -nx / nz          # dh/dx, x to the right
    q = ny / nz           # dh/dy, y down the image (a green-up normal points up the image)
    h, w = p.shape
    u = 2j * np.pi * np.fft.fftfreq(w)[None, :]
    v = 2j * np.pi * np.fft.fftfreq(h)[:, None]
    denom = (u * np.conj(u) + v * np.conj(v)).real
    denom[0, 0] = 1.0
    height = (np.conj(u) * np.fft.fft2(p) + np.conj(v) * np.fft.fft2(q)) / denom
    height[0, 0] = 0.0
    return np.real(np.fft.ifft2(height))


def standard(a):
    return (a - a.mean()) / (a.std() + 1e-9)


def dds_l8_mipped(height):
    """An uncompressed 8-bit luminance DDS with every mip level down to 1x1 (box-filtered)."""
    h, w = height.shape
    levels = [height]
    while levels[-1].shape[0] > 1 or levels[-1].shape[1] > 1:
        a = levels[-1]
        a = a[: a.shape[0] // 2 * 2 or 1, : a.shape[1] // 2 * 2 or 1]
        if a.shape[0] > 1:
            a = (a[0::2] + a[1::2]) / 2.0
        if a.shape[1] > 1:
            a = (a[:, 0::2] + a[:, 1::2]) / 2.0
        levels.append(a)
    flags = 0x1 | 0x2 | 0x4 | 0x8 | 0x1000 | 0x20000   # caps, height, width, pitch, pixel format, mip count
    pixel_format = struct.pack("<8I", 32, 0x20000, 0, 8, 0xFF, 0, 0, 0)  # DDPF_LUMINANCE, 8 bits
    caps = 0x1000 | 0x400000 | 0x8                       # texture, mipmap, complex
    header = struct.pack("<7I", 124, flags, h, w, w, 0, len(levels)) + b"\0" * 44 + pixel_format \
        + struct.pack("<4I", caps, 0, 0, 0) + b"\0" * 4
    out = io.BytesIO()
    out.write(b"DDS " + header)
    for a in levels:
        out.write(np.clip(np.rint(a * 255.0), 0, 255).astype(np.uint8).tobytes())
    return out.getvalue()


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--data", required=True, help="a Skyrim SE Data folder with Bethesda's texture archives")
    ap.add_argument("--preview", help="also write the height map as a PNG here")
    args = ap.parse_args()

    index = bsa_extract.build_index(args.data)
    raw = {}
    for suffix in ("", "_n"):
        key = f"{SOURCE}{suffix}.dds"
        if key not in index:
            sys.exit(f"{key} is in none of the archives in {args.data}")
        raw[suffix] = bsa_extract.extract(index[key])

    normal = np.asarray(Image.open(io.BytesIO(raw["_n"])).convert("RGB"), dtype=np.float64) / 255.0
    size = normal.shape[1], normal.shape[0]
    diffuse = Image.open(io.BytesIO(raw[""])).convert("L").resize(size, Image.LANCZOS)
    grain = standard(high_pass(np.asarray(diffuse, dtype=np.float64) / 255.0, BROAD))

    # The green convention whose height agrees best with the diffuse's light and dark.
    tries = []
    for sign in (1.0, -1.0):
        height = standard(high_pass(integrate(normal, sign), BROAD))
        tries.append((float(np.mean(height * grain)), sign, height))
    tries.sort(reverse=True)
    agree, sign, height = tries[0]
    print(f"green {'up' if sign > 0 else 'down'}: agrees with the diffuse {agree:+.2f} "
          f"(the other reading {tries[1][0]:+.2f})")

    height = standard((1.0 - GRAIN) * height + GRAIN * grain)
    lo, hi = np.percentile(height, [LOW, HIGH])
    height = np.clip((height - lo) / (hi - lo), 0.0, 1.0)
    print(f"height map {size[0]}x{size[1]}: mean {height.mean():.2f}, "
          f"{(height < 0.25).mean():.0%} below a quarter, {(height > 0.75).mean():.0%} above three quarters")

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "Cobble01.dds").write_bytes(raw[""])
    (OUT / "Cobble01_n.dds").write_bytes(raw["_n"])
    (OUT / "Cobble01_p.dds").write_bytes(dds_l8_mipped(height))
    if args.preview:
        Image.fromarray(np.rint(height * 255).astype(np.uint8)).save(args.preview)
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
