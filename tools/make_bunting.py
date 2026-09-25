"""The fair's festival bunting: its own colourways of vanilla's festival line.

    build/texvenv/Scripts/python tools/make_bunting.py --data "E:/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"

Vanilla's festival line (Architecture\\Solitude\\Clutter\\SRopefestivalLine01.nif) hangs nine
pennants, each mapped to one of three small triangles in the top-left corner of the Solitude
city banner (textures\\clutter\\CityBannerSolitude01.dds: a grey, a blue and a red one; the
texture's alpha cuts the triangle). A colourway is that texture with the three triangles
repainted: each keeps its own weave (its brightness, relative to its mean) under the new
colour, and its alpha. The rest of the texture is left as it is (nothing else samples it).

In place of Holidays' colourways (FairHolidaysFree.cs), the same six schemes, painted afresh.
Writes textures\\SkyrimFair\\Bunting\\bunting_<scheme>.dds (DXT5, full mips, as vanilla's) and
build/bunting/<scheme>.png for previews. Built from Bethesda's archives (the Steam install's),
so the output is git-ignored, as the cobbles are.
"""
import argparse
import pathlib
import sys

import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
import bsa_extract  # noqa: E402
from make_lantern_colours import dds_bc1_mipped  # noqa: E402

SOURCE = "textures/clutter/citybannersolitude01.dds"
OUT = ROOT / "assets" / "textures" / "SkyrimFair" / "Bunting"
PREVIEW = ROOT / "build" / "bunting"

# The three pennant triangles, from the NIF's UVs (u 0.001..0.146; v bands), in texture shares.
TRIANGLES = [(0.0, 0.146, 0.027, 0.118), (0.0, 0.146, 0.145, 0.236), (0.0, 0.146, 0.269, 0.360)]

# Each scheme's three pennant colours (the triangles in order), after Holidays' colourways.
SCHEMES = {
    "whiterun": ["#e0d4b0", "#c07030", "#d4b040"],
    "saturalia": ["#c0302a", "#3a8a3a", "#e0c060"],
    "riften": ["#e0d4b0", "#c89a38", "#8040a0"],
    "windhelm": ["#dcd8cc", "#c8b050", "#4a7098"],
    "imperial": ["#b02020", "#d0a030", "#701818"],
    "stormcloak": ["#3a64a0", "#b0c0cc", "#28406a"],
}


def colour(hexcode):
    return np.array([int(hexcode[i:i + 2], 16) for i in (1, 3, 5)], float) / 255.0


def paint(rgba, scheme):
    out = rgba.copy()
    h, w = rgba.shape[:2]
    for (u0, u1, v0, v1), hexcode in zip(TRIANGLES, scheme):
        x0, x1, y0, y1 = int(u0 * w), int(np.ceil(u1 * w)), int(v0 * h), int(np.ceil(v1 * h))
        box = rgba[y0:y1, x0:x1]
        lum = box[..., :3].mean(-1)
        shown = box[..., 3] > 0.5
        mean = lum[shown].mean() if shown.any() else lum.mean()
        weave = np.clip(lum / max(mean, 1e-3), 0.55, 1.45)[..., None]   # the cloth's own detail
        out[y0:y1, x0:x1, :3] = np.clip(colour(hexcode) * weave, 0.0, 1.0)
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", required=True, help="a Skyrim Data folder with Bethesda's own archives")
    args = ap.parse_args()
    index = bsa_extract.build_index(args.data)
    raw = bsa_extract.extract(index[SOURCE])
    PREVIEW.mkdir(parents=True, exist_ok=True)
    (PREVIEW / "citybannersolitude01.dds").write_bytes(raw)
    src = Image.open(PREVIEW / "citybannersolitude01.dds").convert("RGBA")
    rgba = np.asarray(src).astype(float) / 255.0
    OUT.mkdir(parents=True, exist_ok=True)
    for name, scheme in SCHEMES.items():
        img = Image.fromarray((paint(rgba, scheme) * 255 + 0.5).astype(np.uint8), "RGBA")
        img.save(PREVIEW / f"{name}.png")
        (OUT / f"bunting_{name}.dds").write_bytes(dds_bc1_mipped(img, "DXT5"))
        print(f"bunting_{name}.dds")


if __name__ == "__main__":
    main()
