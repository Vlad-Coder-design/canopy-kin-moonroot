"""Convert generated QA TGA frames to reviewable PNG files."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SCREENSHOTS = ROOT / "QA" / "Screenshots"


def main() -> None:
    converted = 0
    for source in sorted(SCREENSHOTS.glob("ant-051-windows-*.tga")):
        target = source.with_suffix(".png")
        with Image.open(source) as image:
            image.save(target, format="PNG", optimize=True)
        converted += 1
        print(f"CANOPY_KIN_QA_PNG path={target}")
    print(f"CANOPY_KIN_QA_PNG_OK converted={converted}")


if __name__ == "__main__":
    main()
