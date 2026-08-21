using UnityEngine;

namespace CanopyKin
{
    public enum GameEdition
    {
        WindowsFull,
        WebOptimized
    }

    /// <summary>
    /// Keeps the full Windows presentation independent from the browser budget.
    /// Source assets stay full resolution; Unity platform overrides create the
    /// smaller WebGL representation at build time.
    /// </summary>
    public static class RuntimeQualityProfile
    {
        public static GameEdition Edition =>
            Application.platform == RuntimePlatform.WebGLPlayer
                ? GameEdition.WebOptimized
                : GameEdition.WindowsFull;

        public static bool IsFullQuality => Edition == GameEdition.WindowsFull;

        public static int TerrainResolution(int quality) =>
            IsFullQuality
                ? quality switch { 0 => 160, 1 => 240, _ => 320 }
                : quality switch { 0 => 72, 1 => 96, _ => 128 };

        public static int GrassCount(int quality) =>
            IsFullQuality
                ? quality switch { 0 => 320, 1 => 520, _ => 760 }
                : quality switch { 0 => 105, 1 => 145, _ => 190 };

        public static int LeafCount(int quality) =>
            IsFullQuality
                ? quality switch { 0 => 90, 1 => 145, _ => 220 }
                : quality switch { 0 => 24, 1 => 34, _ => 46 };

        public static int DebrisCount(int quality) =>
            IsFullQuality
                ? quality switch { 0 => 150, 1 => 240, _ => 360 }
                : quality switch { 0 => 55, 1 => 70, _ => 92 };

        public static int DistantTreeCount(int quality) =>
            IsFullQuality
                ? quality switch { 0 => 28, 1 => 36, _ => 44 }
                : quality switch { 0 => 16, 1 => 20, _ => 24 };
    }
}
