"""Create production Unity-ready albedo, normal, and roughness maps.

The source images are original project textures generated for Canopy Kin.  This
script preserves source detail for the Windows edition, removes only unstable
high-frequency colour noise, and derives physically plausible companion maps.
WebGL downscaling is handled separately by Unity platform import overrides.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter, ImageOps


def make_seamless(image: Image.Image, feather: int = 96) -> Image.Image:
    """Blend a half-tile offset so opposite borders match without a hard seam."""
    base = image.convert("RGB")
    shifted = Image.fromarray(
        np.roll(
            np.roll(np.asarray(base), base.height // 2, axis=0),
            base.width // 2,
            axis=1,
        )
    )
    mask = Image.new("L", base.size, 255)
    width, height = base.size
    pixels = np.full((height, width), 255, dtype=np.uint8)
    x = np.minimum(np.arange(width), np.arange(width)[::-1])
    y = np.minimum(np.arange(height), np.arange(height)[::-1])
    edge_distance = np.minimum(y[:, None], x[None, :])
    pixels[:] = np.clip(edge_distance / max(feather, 1) * 255, 0, 255)
    mask = Image.fromarray(pixels).filter(ImageFilter.GaussianBlur(feather / 4))
    return Image.composite(base, shifted, mask)


def derive_normal(height: np.ndarray, strength: float) -> Image.Image:
    dy, dx = np.gradient(height)
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(height)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    packed = np.stack(
        ((nx / length + 1) * 127.5, (ny / length + 1) * 127.5, nz / length * 255),
        axis=-1,
    )
    return Image.fromarray(np.clip(packed, 0, 255).astype(np.uint8), "RGB")


def process(
    source: Path,
    output: Path,
    size: int,
    strength: float,
    unity_prefix: str | None = None,
) -> None:
    output.mkdir(parents=True, exist_ok=True)
    size_label = f"{size // 1024}k" if size >= 1024 and size % 1024 == 0 else str(size)

    def output_name(channel: str, extension: str) -> Path:
        if not unity_prefix:
            return output / f"{channel}.{extension}"
        unity_channel = {
            "albedo": "diff",
            "normal": "nor_dx",
            "roughness": "rough",
            "ao": "ao",
        }[channel]
        return output / f"{unity_prefix}_{unity_channel}_{size_label}.{extension}"

    image = Image.open(source).convert("RGB")
    image = ImageOps.fit(image, (size, size), method=Image.Resampling.LANCZOS)
    image = make_seamless(image)
    image = ImageEnhance.Color(image).enhance(0.88)
    image = ImageEnhance.Contrast(image).enhance(0.94)
    image.save(
        output_name("albedo", "jpg"),
        quality=95,
        optimize=True,
        progressive=True,
    )

    gray = np.asarray(ImageOps.grayscale(image).filter(ImageFilter.GaussianBlur(1.1)))
    height = gray.astype(np.float32) / 255.0
    derive_normal(height, strength).save(
        output_name("normal", "png"),
        optimize=True,
    )

    roughness = Image.fromarray(
        np.clip(220 - (gray.astype(np.int16) - 128) * 0.22, 150, 238).astype(np.uint8),
        "L",
    )
    roughness.filter(ImageFilter.GaussianBlur(1.5)).save(
        output_name("roughness", "png"),
        optimize=True,
    )

    # Only small recessed pores darken the AO map. Broad albedo colour variation
    # remains in base colour and cannot incorrectly turn into baked lighting.
    broad = np.asarray(
        ImageOps.grayscale(image).filter(ImageFilter.GaussianBlur(max(3.0, size / 900)))
    ).astype(np.int16)
    recess = np.clip(broad - gray.astype(np.int16), 0, 54)
    occlusion = Image.fromarray(
        np.clip(253 - recess * 2.4, 124, 253).astype(np.uint8),
        "L",
    )
    occlusion.filter(ImageFilter.GaussianBlur(0.7)).save(
        output_name("ao", "png"),
        optimize=True,
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--size", type=int, default=4096)
    parser.add_argument("--strength", type=float, default=5.0)
    parser.add_argument(
        "--unity-prefix",
        help="Write Unity channel names such as <prefix>_diff_4k.jpg.",
    )
    args = parser.parse_args()
    process(args.source, args.output, args.size, args.strength, args.unity_prefix)
