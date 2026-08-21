"""Convert generated QA TGA frames to reviewable PNG files."""

from pathlib import Path
import glob
import sys

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SCREENSHOTS = ROOT / "QA" / "Screenshots"


def main() -> None:
    patterns = sys.argv[1:] or [str(SCREENSHOTS / "*.tga")]
    converted = 0
    for pattern in patterns:
        candidate = Path(pattern)
        if not candidate.is_absolute():
            candidate = ROOT / candidate
        for match in sorted(glob.glob(str(candidate))):
            source = Path(match)
            target = source.with_suffix(".png")
            with Image.open(source) as image:
                image.save(target, format="PNG", optimize=True)
            converted += 1
            print(f"CANOPY_KIN_QA_PNG path={target}")
    print(f"CANOPY_KIN_QA_PNG_OK converted={converted}")


if __name__ == "__main__":
    main()
