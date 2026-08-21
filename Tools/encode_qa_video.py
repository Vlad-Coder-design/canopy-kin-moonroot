"""Encode Moonroot QA image sequences to a browser-friendly MP4."""

from pathlib import Path
import subprocess

import imageio_ffmpeg


ROOT = Path(__file__).resolve().parents[1]
FRAMES = ROOT / "QA" / "VideoFrames" / "environment-070-contact"
OUTPUT = ROOT / "QA" / "Videos" / "environment-070-contact.mp4"


def main() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    command = [
        imageio_ffmpeg.get_ffmpeg_exe(),
        "-y",
        "-framerate",
        "15",
        "-i",
        str(FRAMES / "frame-%04d.tga"),
        "-c:v",
        "libx264",
        "-preset",
        "slow",
        "-crf",
        "18",
        "-pix_fmt",
        "yuv420p",
        "-movflags",
        "+faststart",
        str(OUTPUT),
    ]
    subprocess.run(command, check=True)
    print(f"MOONROOT_QA_VIDEO_OK path={OUTPUT} bytes={OUTPUT.stat().st_size}")


if __name__ == "__main__":
    main()
