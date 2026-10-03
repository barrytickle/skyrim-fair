"""Build the project-owned Skyrim Fair paving texture set.

The committed source image was generated specifically for Skyrim Fair and is
made exactly periodic here before normal/specular derivation.  The DDS stage
uses Microsoft's texconv so the game receives block compression and full
mipmaps rather than a mip-less convenience export.

Example:

    python assets/textures/build_paving_material.py \
      --texconv "E:/Modlists/Still In Skyrim/tools/acmosrg/texconv/texconv.exe"
"""

from __future__ import annotations

import argparse
import math
import random
import subprocess
from pathlib import Path

from PIL import Image, ImageChops, ImageEnhance, ImageFilter, ImageOps


SIZE = 1024
LAYOUT_SIZE = 512


def periodic_micrograin(source: Image.Image) -> Image.Image:
    """Keep only fine authored grain, with low-frequency edge differences removed."""
    source = ImageOps.fit(source.convert("L"), (SIZE, SIZE), method=Image.Resampling.LANCZOS)
    low = source.filter(ImageFilter.GaussianBlur(radius=14))
    high = ImageChops.subtract(source, low, scale=1.0, offset=128)
    # A half-tile roll puts the source's former image boundary in the interior.  The
    # high-pass has no large forms left to expose it, while the wrap itself now samples
    # an ordinary continuous interior neighbourhood.
    return ImageChops.offset(high, SIZE // 2, SIZE // 2)


def synthesize_cobbles(source: Image.Image) -> Image.Image:
    """Build a non-mirrored, exactly periodic irregular stone layout on a torus."""
    rng = random.Random(0x5F41_2026)
    grid = 13
    cell = LAYOUT_SIZE / grid
    seeds: dict[tuple[int, int], tuple[float, float, int]] = {}
    palette: list[tuple[int, int, int]] = []
    for gy in range(grid):
        for gx in range(grid):
            seed_id = len(palette)
            seeds[(gx, gy)] = (
                (gx + 0.5 + rng.uniform(-0.34, 0.34)) * cell,
                (gy + 0.5 + rng.uniform(-0.34, 0.34)) * cell,
                seed_id,
            )
            base = rng.choice(
                [(88, 86, 82), (78, 80, 79), (96, 91, 82), (72, 75, 76),
                 (104, 96, 84), (84, 82, 77), (68, 71, 72)]
            )
            delta = rng.randint(-9, 10)
            palette.append(tuple(max(0, min(255, c + delta)) for c in base))

    labels = Image.new("I", (LAYOUT_SIZE, LAYOUT_SIZE))
    label_pixels = labels.load()
    for y in range(LAYOUT_SIZE):
        gy = int(y / cell) % grid
        for x in range(LAYOUT_SIZE):
            gx = int(x / cell) % grid
            best_distance = float("inf")
            best_seed = 0
            for oy in range(-2, 3):
                for ox in range(-2, 3):
                    sx, sy, seed_id = seeds[((gx + ox) % grid, (gy + oy) % grid)]
                    dx = abs(x - sx)
                    dy = abs(y - sy)
                    dx = min(dx, LAYOUT_SIZE - dx)
                    dy = min(dy, LAYOUT_SIZE - dy)
                    distance = dx * dx + dy * dy
                    if distance < best_distance:
                        best_distance = distance
                        best_seed = seed_id
            label_pixels[x, y] = best_seed

    stone = Image.new("RGB", labels.size)
    stone_pixels = stone.load()
    joint = Image.new("L", labels.size, 0)
    joint_pixels = joint.load()
    for y in range(LAYOUT_SIZE):
        for x in range(LAYOUT_SIZE):
            label = label_pixels[x, y]
            stone_pixels[x, y] = palette[label]
            if any(
                label_pixels[(x + dx) % LAYOUT_SIZE, (y + dy) % LAYOUT_SIZE] != label
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))
            ):
                joint_pixels[x, y] = 255

    stone = stone.resize((SIZE, SIZE), Image.Resampling.BICUBIC)
    joint = joint.resize((SIZE, SIZE), Image.Resampling.NEAREST).filter(ImageFilter.MaxFilter(7))
    bevel = joint.filter(ImageFilter.GaussianBlur(radius=5.5))

    grain = periodic_micrograin(source).convert("RGB")
    stone = ImageChops.add(stone, grain, scale=1.0, offset=-128)
    stone = Image.composite(Image.new("RGB", stone.size, (112, 107, 97)), stone, bevel.point(
        lambda p: round(p * 0.16)
    ))
    stone = Image.composite(Image.new("RGB", stone.size, (34, 31, 26)), stone, joint)
    return stone


def grade_albedo(image: Image.Image) -> Image.Image:
    image = ImageEnhance.Color(image).enhance(0.72)
    image = ImageEnhance.Contrast(image).enhance(0.90)
    image = ImageEnhance.Brightness(image).enhance(0.82)
    # A restrained cool-earth wash keeps the stones out of clean concrete grey.
    wash = Image.new("RGB", image.size, (83, 78, 70))
    return Image.blend(image, wash, 0.10)


def material_height(albedo: Image.Image) -> Image.Image:
    """Single-channel relief: joints low, worn stone faces high.

    The range is deliberately restrained.  This is medium/small surface relief for
    Community Shaders or vanilla parallax, never a substitute for mesh silhouette.
    """
    height = ImageOps.autocontrast(albedo.convert("L"), cutoff=1)
    height = height.filter(ImageFilter.GaussianBlur(radius=2.2))
    return ImageEnhance.Contrast(height).enhance(1.18)


def normal_and_specular(albedo: Image.Image, height: Image.Image) -> Image.Image:
    # Lighter stone faces sit above dark compacted joints.  A mild blur suppresses
    # photographic grain so the normal map responds to stones, not every albedo fleck.
    left = ImageChops.offset(height, -2, 0)
    right = ImageChops.offset(height, 2, 0)
    up = ImageChops.offset(height, 0, -2)
    down = ImageChops.offset(height, 0, 2)
    gx = ImageChops.subtract(right, left, scale=1.0, offset=128)
    gy = ImageChops.subtract(down, up, scale=1.0, offset=128)

    height_pixels = height.load()
    gx_pixels = gx.load()
    gy_pixels = gy.load()
    normal = Image.new("RGBA", albedo.size)
    out = normal.load()
    strength = 2.35
    for y in range(SIZE):
        for x in range(SIZE):
            dx = (gx_pixels[x, y] - 128) / 127.0
            dy = (gy_pixels[x, y] - 128) / 127.0
            nx = -dx * strength
            ny = dy * strength  # DirectX-style tangent-space Y for Skyrim.
            nz = 1.0
            inv = 1.0 / math.sqrt(nx * nx + ny * ny + nz * nz)
            # Skyrim reads the normal texture alpha as gloss/specular strength.
            # Dirt joints stay almost matte; worn stone receives only a muted response.
            specular = 14 + int(height_pixels[x, y] / 255.0 * 42)
            out[x, y] = (
                round((nx * inv * 0.5 + 0.5) * 255),
                round((ny * inv * 0.5 + 0.5) * 255),
                round((nz * inv * 0.5 + 0.5) * 255),
                specular,
            )

    normal.paste(normal.crop((0, 0, 1, SIZE)), (SIZE - 1, 0))
    normal.paste(normal.crop((0, 0, SIZE, 1)), (0, SIZE - 1))
    return normal


def run_texconv(texconv: Path, source: Path, output: Path, pixel_format: str) -> None:
    subprocess.run(
        [
            str(texconv), "-nologo", "-y", "-m", "0", "-f", pixel_format,
            "-o", str(output), str(source),
        ],
        check=True,
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--texconv", type=Path, required=True)
    args = parser.parse_args()

    root = Path(__file__).resolve().parents[2]
    source_path = root / "assets" / "textures" / "source" / "SkyrimFair_Cobble_Source.png"
    output = root / "assets" / "textures" / "SkyrimFair"
    output.mkdir(parents=True, exist_ok=True)

    periodic = grade_albedo(synthesize_cobbles(Image.open(source_path)))
    height = material_height(periodic)
    normal = normal_and_specular(periodic, height)
    diffuse_png = output / "SkyrimFair_Cobble01.png"
    normal_png = output / "SkyrimFair_Cobble01_n.png"
    height_png = output / "SkyrimFair_Cobble01_p.png"
    periodic.save(diffuse_png, optimize=True)
    normal.save(normal_png, optimize=True)
    height.save(height_png, optimize=True)

    run_texconv(args.texconv, diffuse_png, output, "BC1_UNORM_SRGB")
    run_texconv(args.texconv, normal_png, output, "BC3_UNORM")
    run_texconv(args.texconv, height_png, output, "BC4_UNORM")

    print(f"source:  {source_path}")
    print(f"diffuse: {output / 'SkyrimFair_Cobble01.dds'}")
    print(f"normal:  {output / 'SkyrimFair_Cobble01_n.dds'}")
    print(f"height:  {output / 'SkyrimFair_Cobble01_p.dds'} (optional)")


if __name__ == "__main__":
    main()
