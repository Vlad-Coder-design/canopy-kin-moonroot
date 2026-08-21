using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Dense, reusable environment meshes for the close third-person hero region.
    /// These meshes deliberately spend geometry where the camera can resolve it;
    /// the broad forest continues to use the cheaper production LOD kit.
    /// </summary>
    public static class EnvironmentMeshFactory
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh MicroTerrain(
            Vector2 center,
            Vector2 size,
            int xSegments,
            int zSegments,
            Func<float, float, float> height)
        {
            string key = $"micro-ground-{xSegments}-{zSegments}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;

            int row = xSegments + 1;
            var vertices = new Vector3[row * (zSegments + 1)];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[xSegments * zSegments * 6];
            for (int z = 0; z <= zSegments; z++)
            for (int x = 0; x <= xSegments; x++)
            {
                float px = center.x + (x / (float)xSegments - .5f) * size.x;
                float pz = center.y + (z / (float)zSegments - .5f) * size.y;
                int index = z * row + x;
                vertices[index] = new Vector3(px, height(px, pz) + .012f, pz);
                uv[index] = new Vector2(px / 3.35f, pz / 3.35f);

                float dx = px - center.x;
                float dz = pz - center.y;
                float micro = Mathf.PerlinNoise(px * .73f + 31.7f, pz * .73f + 18.2f);
                float rootShelter = 1f - Mathf.SmoothStep(.3f, 2.7f,
                    Mathf.Abs(dz - 2.15f - Mathf.Sin(dx * .55f) * .34f));
                float moss = Mathf.Clamp01(rootShelter * .72f +
                    (Mathf.PerlinNoise(px * .31f, pz * .31f) - .54f) * 2.1f);
                float leafLitter = Mathf.Clamp01(
                    (Mathf.PerlinNoise(px * .19f + 9f, pz * .22f + 4f) - .43f) * 1.55f);
                float puddle = 1f - Mathf.Clamp01(Vector2.Distance(
                    new Vector2(px, pz), center + new Vector2(-3.25f, -.85f)) / 1.35f);
                float wet = Mathf.Clamp01(puddle * .92f + (1f - micro) * .18f);
                colors[index] = new Color(moss, leafLitter, wet, 1f);
            }

            int triangle = 0;
            for (int z = 0; z < zSegments; z++)
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * row + x;
                int b = a + 1;
                int c = a + row;
                int d = c + 1;
                triangles[triangle++] = a;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = b;
                triangles[triangle++] = c;
                triangles[triangle++] = d;
            }

            var mesh = new Mesh
            {
                name = "Moonroot high-density microhabitat ground",
                vertices = vertices,
                uv = uv,
                colors = colors,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh HeroStone(int variant)
        {
            string key = $"hero-stone-{variant % 9}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int rings = 14;
            const int sectors = 24;
            var vertices = new List<Vector3>((rings + 1) * (sectors + 1));
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(rings * sectors * 6);

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = ring / (float)rings;
                float latitude = Mathf.Lerp(-Mathf.PI * .5f, Mathf.PI * .5f, v);
                for (int sector = 0; sector <= sectors; sector++)
                {
                    float u = sector / (float)sectors;
                    float longitude = u * Mathf.PI * 2f;
                    Vector3 p = new(
                        Mathf.Cos(latitude) * Mathf.Cos(longitude),
                        Mathf.Sin(latitude),
                        Mathf.Cos(latitude) * Mathf.Sin(longitude));
                    float broad = Mathf.PerlinNoise(
                        p.x * 1.9f + variant * 1.13f,
                        p.z * 2.1f + p.y * .8f + 11f);
                    float fracture = Mathf.Abs(Mathf.Sin(
                        p.x * 3.7f + p.z * 2.3f + p.y * 1.6f + variant));
                    float radius = .86f + broad * .2f + fracture * .045f;
                    p *= radius;
                    p.y *= .67f;
                    p.x += Mathf.Sign(p.x) * Mathf.Pow(Mathf.Abs(p.x), 1.7f) * .04f;
                    vertices.Add(p);
                    uv.Add(new Vector2(u * 1.7f, v * 1.25f));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            for (int sector = 0; sector < sectors; sector++)
            {
                int a = ring * (sectors + 1) + sector;
                int b = a + sectors + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }

            // Shared vertices preserve weathered normals across the surface.
            // Shape noise still creates a few chipped planes without turning
            // every triangle into a visible low-poly facet.
            var mesh = new Mesh { name = $"Weathered chipped hero stone {variant % 9}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh GroundcoverCluster(int variant, bool lowDetail = false)
        {
            string key = $"groundcover-{variant % 12}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int specimenCount = lowDetail ? 2 : 5;
            int verticalSegments = lowDetail ? 2 : 6;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 48611 + (lowDetail ? 19 : 71));
            for (int specimen = 0; specimen < specimenCount; specimen++)
            {
                int atlas = (variant + specimen) % 4;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .32f;
                float width = Mathf.Lerp(.42f, .72f, (float)random.NextDouble());
                float height = Mathf.Lerp(.54f, .94f, (float)random.NextDouble());
                float lean = Mathf.Lerp(.05f, .22f, (float)random.NextDouble());
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 side = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 forward = Vector3.Cross(Vector3.up, side);
                int start = vertices.Count;
                float u0 = atlas % 2 * .5f;
                float v0 = atlas < 2 ? .5f : 0f;
                for (int segment = 0; segment <= verticalSegments; segment++)
                {
                    float t = segment / (float)verticalSegments;
                    Vector3 center = origin + Vector3.up * height * t +
                                     forward * lean * t * t +
                                     side * Mathf.Sin(t * Mathf.PI) * .035f;
                    float taper = Mathf.Lerp(1f, .86f, t);
                    vertices.Add(center - side * width * .5f * taper);
                    vertices.Add(center + side * width * .5f * taper);
                    uv.Add(new Vector2(u0, v0 + t * .5f));
                    uv.Add(new Vector2(u0 + .5f, v0 + t * .5f));
                    Color wind = new(t, .82f, (float)random.NextDouble(), 1f);
                    colors.Add(wind);
                    colors.Add(wind);
                }
                for (int segment = 0; segment < verticalSegments; segment++)
                {
                    int a = start + segment * 2;
                    triangles.Add(a); triangles.Add(a + 2); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(a + 2); triangles.Add(a + 3);
                }
            }
            var mesh = new Mesh { name = lowDetail ? "Groundcover LOD" : "Mixed woodland groundcover" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh HeroGrassCluster(int variant, bool lowDetail = false)
        {
            string key = $"hero-grass-{variant % 11}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int bladeCount = lowDetail ? 5 : 13;
            int lengthSegments = lowDetail ? 3 : 10;
            int widthSegments = lowDetail ? 1 : 2;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 104729 + (lowDetail ? 17 : 53));

            for (int blade = 0; blade < bladeCount; blade++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .31f;
                float height = Mathf.Lerp(.68f, 1.3f, (float)random.NextDouble());
                float width = Mathf.Lerp(.055f, .13f, (float)random.NextDouble());
                float lean = Mathf.Lerp(.16f, .48f, (float)random.NextDouble());
                float curl = Mathf.Lerp(-.09f, .11f, (float)random.NextDouble());
                float phase = (float)random.NextDouble();
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 forward = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 side = new(-forward.z, 0, forward.x);
                int atlas = (variant + blade) % 4;
                // The generated atlas keeps generous transparent separation.
                // Sample the measured leaf bounds rather than the whole quadrant,
                // otherwise the visible blade collapses into a hairline.
                Vector2 uvMin = atlas switch
                {
                    0 => new Vector2(.214f, .503f),
                    1 => new Vector2(.704f, .503f),
                    2 => new Vector2(.214f, .006f),
                    _ => new Vector2(.695f, .006f)
                };
                Vector2 uvMax = atlas switch
                {
                    0 => new Vector2(.296f, .994f),
                    1 => new Vector2(.872f, .994f),
                    2 => new Vector2(.333f, .497f),
                    _ => new Vector2(.833f, .497f)
                };
                int start = vertices.Count;

                for (int y = 0; y <= lengthSegments; y++)
                {
                    float t = y / (float)lengthSegments;
                    float taper = Mathf.Pow(Mathf.Sin((1f - t) * Mathf.PI * .5f), .68f);
                    Vector3 center = origin + Vector3.up * height * t +
                                     forward * (lean * t * t) +
                                     side * (curl * Mathf.Sin(t * Mathf.PI));
                    for (int x = 0; x <= widthSegments; x++)
                    {
                        float across = x / (float)widthSegments * 2f - 1f;
                        Vector3 crown = forward * (1f - across * across) * width * .11f;
                        vertices.Add(center + side * width * taper * across + crown);
                        float atlasU = Mathf.Lerp(uvMin.x, uvMax.x,
                            x / (float)widthSegments);
                        float atlasV = Mathf.Lerp(uvMin.y, uvMax.y, t);
                        uv.Add(new Vector2(atlasU, atlasV));
                        colors.Add(new Color(t, .64f + (float)random.NextDouble() * .3f, phase, 1f));
                    }
                }

                int strip = widthSegments + 1;
                for (int y = 0; y < lengthSegments; y++)
                for (int x = 0; x < widthSegments; x++)
                {
                    int a = start + y * strip + x;
                    int b = a + strip;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = lowDetail ? "Hero grass LOD" : "Veined hero grass" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh HeroFallenLeaf(int variant)
        {
            string key = $"hero-leaf-{variant % 12}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int lengthSegments = 24;
            const int widthSegments = 6;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            float asymmetry = ((variant % 5) - 2) * .025f;
            for (int z = 0; z <= lengthSegments; z++)
            {
                float along = z / (float)lengthSegments;
                // The atlas alpha owns the species silhouette, holes and tears.
                // Keep the curved support sheet broad enough that no part of a
                // diagonal photographed leaf is clipped before alpha testing.
                float halfWidth = .52f + asymmetry;
                for (int x = 0; x <= widthSegments; x++)
                {
                    float across = x / (float)widthSegments * 2f - 1f;
                    float length = Mathf.Lerp(-.72f, .72f, along);
                    float midrib = Mathf.Exp(-Mathf.Abs(across) * 8f) * .026f;
                    float cup = across * across * .052f;
                    float curl = Mathf.Sin(along * Mathf.PI * 2f + variant * .7f) * .035f;
                    float edgeCrinkle = Mathf.Sin(along * 31f + across * 9f + variant) *
                                        Mathf.Abs(across) * .014f;
                    vertices.Add(new Vector3(
                        across * halfWidth,
                        midrib + cup + curl + edgeCrinkle,
                        length + across * asymmetry));
                    int atlas = variant % 4;
                    float u0 = atlas % 2 * .5f;
                    float v0 = atlas < 2 ? .5f : 0f;
                    uv.Add(new Vector2(
                        u0 + x / (float)widthSegments * .5f,
                        v0 + along * .5f));
                }
            }

            int row = widthSegments + 1;
            for (int z = 0; z < lengthSegments; z++)
            for (int x = 0; x < widthSegments; x++)
            {
                int a = z * row + x;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }

            var mesh = new Mesh { name = $"Damaged veined forest leaf {variant % 12}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh MossCushion(int variant)
        {
            string key = $"moss-cushion-{variant % 5}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int hummocks = 18;
            const int rings = 5;
            const int sectors = 8;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 65537 + 97);
            for (int mound = 0; mound < hummocks; mound++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .43f;
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                float moundRadius = Mathf.Lerp(.12f, .24f, (float)random.NextDouble());
                float moundHeight = Mathf.Lerp(.09f, .2f, (float)random.NextDouble());
                int start = vertices.Count;
                for (int ring = 0; ring <= rings; ring++)
                {
                    float v = ring / (float)rings;
                    float radial = Mathf.Sin(v * Mathf.PI * .5f) * moundRadius;
                    float y = Mathf.Cos(v * Mathf.PI * .5f) * moundHeight;
                    for (int sector = 0; sector <= sectors; sector++)
                    {
                        float u = sector / (float)sectors;
                        float a = u * Mathf.PI * 2f;
                        vertices.Add(origin + new Vector3(
                            Mathf.Cos(a) * radial,
                            y,
                            Mathf.Sin(a) * radial));
                        uv.Add(new Vector2(u * 1.4f, v));
                        colors.Add(Color.Lerp(
                            new Color(.3f, .52f, .14f, 1),
                            new Color(.57f, .69f, .25f, 1),
                            (float)random.NextDouble()));
                    }
                }
                for (int ring = 0; ring < rings; ring++)
                for (int sector = 0; sector < sectors; sector++)
                {
                    int a = start + ring * (sectors + 1) + sector;
                    int b = a + sectors + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = "Layered velvet moss cushion" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh IrregularPuddle(int variant, int segments = 36)
        {
            string key = $"puddle-{variant}-{segments}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int rings = 4;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int ring = 0; ring <= rings; ring++)
            {
                float radialT = ring / (float)rings;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float angle = t * Mathf.PI * 2f;
                    float outline = .86f + Mathf.Sin(angle * 3f + variant) * .09f +
                                    Mathf.Sin(angle * 7f - variant * .31f) * .045f;
                    float radius = outline * radialT;
                    float tension = Mathf.Sin(radialT * Mathf.PI) * .032f +
                                    (1f - radialT) * .008f;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * radius,
                        tension,
                        Mathf.Sin(angle) * radius));
                    uv.Add(new Vector2(
                        Mathf.Cos(angle) * radialT * .5f + .5f,
                        Mathf.Sin(angle) * radialT * .5f + .5f));
                }
            }
            int row = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < segments; i++)
            {
                int a = ring * row + i;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            var mesh = new Mesh { name = "Irregular shallow rain puddle" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh ForestHorizonBank(int variant, int segments = 96)
        {
            string key = $"forest-horizon-bank-{variant}-{segments}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int levels = 5;
            var vertices = new List<Vector3>((levels + 1) * (segments + 1));
            var uv = new List<Vector2>(vertices.Capacity);
            var colors = new List<Color>(vertices.Capacity);
            var triangles = new List<int>(levels * segments * 6);
            for (int level = 0; level <= levels; level++)
            {
                float v = level / (float)levels;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float angle = u * Mathf.PI * 2f;
                    float radius = 52f + Mathf.Sin(angle * 5f + variant) * 2.1f +
                                   Mathf.Sin(angle * 11f - variant * .7f) * .55f;
                    float height = Mathf.Lerp(-4.8f, 24f, v) +
                                   Mathf.Sin(angle * 4f + variant) * v * 1.25f +
                                   Mathf.Sin(angle * 9f) * v * .48f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
                    // Eight mirrored panels keep macro features at a plausible
                    // scale around the 326-metre circumference. A single copy
                    // would stretch each tree across tens of metres.
                    uv.Add(new Vector2(u * 8f, v));
                    colors.Add(Color.Lerp(new Color(.25f, .2f, .12f, 1f),
                        new Color(.12f, .24f, .1f, 1f), v));
                }
            }
            int row = segments + 1;
            for (int level = 0; level < levels; level++)
            for (int segment = 0; segment < segments; segment++)
            {
                int a = level * row + segment;
                int b = a + row;
                // Inward winding: this is only a distant forest closure.
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
            }
            var mesh = new Mesh { name = "Irregular fogged forest horizon bank" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }
    }
}
