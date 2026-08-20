using UnityEngine;

namespace CanopyKin
{
    /// <summary>Slowly moves a procedural canopy cookie so the light feels filtered by leaves.</summary>
    public sealed class CanopyLightMotion : MonoBehaviour
    {
        Quaternion restRotation;

        public void Initialize(Light light)
        {
            restRotation = transform.rotation;
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.R8, false, true)
            {
                name = "Procedural soft canopy light cookie",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float broad = Mathf.PerlinNoise(u * 4.1f + 8.3f, v * 4.1f + 2.7f);
                float leaves = Mathf.PerlinNoise(u * 11.3f + 21.4f, v * 9.7f + 14.2f);
                float holes = Mathf.SmoothStep(.46f, .72f, broad * .67f + leaves * .33f);
                byte value = (byte)Mathf.RoundToInt(Mathf.Lerp(112f, 255f, holes));
                pixels[y * size + x] = new Color32(value, value, value, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            light.cookie = texture;
            light.cookieSize = 16f;
        }

        void Update()
        {
            float drift = Mathf.Sin(Time.time * .075f) * 1.35f;
            transform.rotation = restRotation * Quaternion.Euler(0, drift, Mathf.Sin(Time.time * .11f) * .35f);
        }
    }
}
