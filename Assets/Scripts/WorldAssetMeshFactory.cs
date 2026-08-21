using System;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    public enum BroodStage { Egg, Larva, Pupa }

    /// <summary>
    /// Purpose-built close-camera meshes for colony cargo, brood and nest
    /// architecture. These replace the single stretched ellipsoid that used to
    /// stand in for every resource and life stage.
    /// </summary>
    public static class WorldAssetMeshFactory
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh Seed(int variant)
        {
            string key = $"seed-{variant % 5}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int ridges = 8 + variant % 3;
            Mesh mesh = ParametricOvoid(
                key,
                20,
                32,
                (p, longitude, latitude) =>
                {
                    float end = Mathf.Clamp01(Mathf.Cos(latitude));
                    float longitudinalRidge = 1f + Mathf.Pow(Mathf.Abs(Mathf.Cos(longitude * ridges)), 5f) * .065f;
                    float asymmetry = 1f + p.z * .08f + Mathf.Sin(longitude + variant) * .018f;
                    p.x *= .43f * longitudinalRidge * asymmetry;
                    p.y *= .31f * longitudinalRidge;
                    p.z *= .7f;
                    p.z += Mathf.Sign(p.z) * Mathf.Pow(Mathf.Abs(p.z), 4f) * .08f;
                    p.x += Mathf.Sin(latitude * 2f + variant) * end * .018f;
                    return p;
                });
            mesh.name = $"Ridged forest seed {variant % 5}";
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Resin(int variant)
        {
            string key = $"resin-{variant % 4}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            AppendOvoid(vertices, uv, triangles, new Vector3(-.16f, .01f, .04f), new Vector3(.38f, .27f, .36f), 13, 20, variant * .7f);
            AppendOvoid(vertices, uv, triangles, new Vector3(.12f, .03f, -.08f), new Vector3(.34f, .3f, .42f), 13, 20, variant * .7f + 2.1f);
            AppendOvoid(vertices, uv, triangles, new Vector3(.03f, .17f, .14f), new Vector3(.25f, .22f, .28f), 12, 18, variant * .7f + 4.6f);
            Mesh mesh = Finish($"Fused amber resin drops {variant % 4}", vertices, uv, triangles);
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Protein(int variant)
        {
            string key = $"protein-{variant % 4}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            AppendOvoid(vertices, uv, triangles, Vector3.zero, new Vector3(.5f, .18f, .42f), 14, 22, variant + 3.2f);
            AppendOvoid(vertices, uv, triangles, new Vector3(.28f, .02f, -.16f), new Vector3(.24f, .14f, .3f), 10, 16, variant + 8.1f);
            Mesh mesh = Finish($"Broken insect protein fragment {variant % 4}", vertices, uv, triangles);
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Egg(int variant)
        {
            string key = $"egg-{variant % 3}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            Mesh mesh = ParametricOvoid(
                key,
                16,
                24,
                (p, longitude, latitude) =>
                {
                    p.x *= .4f;
                    p.y *= .34f;
                    p.z *= .58f;
                    p.y += Mathf.Sin(longitude * 2f + variant) * Mathf.Cos(latitude) * .006f;
                    return p;
                });
            mesh.name = $"Translucent ant egg {variant % 3}";
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Larva(int variant)
        {
            string key = $"larva-{variant % 4}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int sections = 13;
            var path = new Vector3[sections];
            var radii = new float[sections];
            for (int i = 0; i < sections; i++)
            {
                float t = i / (float)(sections - 1);
                float z = Mathf.Lerp(-.56f, .56f, t);
                float curl = Mathf.Sin(t * Mathf.PI) * (.13f + variant * .012f);
                float segment = 1f - Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 8f)), 14f) * .12f;
                path[i] = new Vector3(Mathf.Sin(t * Mathf.PI * 1.45f + variant * .3f) * .035f,
                    curl * .36f, z);
                radii[i] = Mathf.Sin(Mathf.Lerp(.16f, Mathf.PI - .12f, t)) * .29f * segment + .045f;
            }
            Mesh mesh = OrganicMeshFactory.Tube(path, radii, 18);
            mesh.name = $"Segmented curved ant larva {variant % 4}";
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Pupa(int variant)
        {
            string key = $"pupa-{variant % 4}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            AppendOvoid(vertices, uv, triangles, new Vector3(0, 0, -.05f), new Vector3(.42f, .33f, .68f), 18, 28, variant * .4f);
            AppendOvoid(vertices, uv, triangles, new Vector3(0, .06f, .43f), new Vector3(.28f, .25f, .28f), 14, 22, variant * .4f + 1f);
            // Folded limb impressions sit against the cocoon instead of reading
            // as a featureless capsule.
            for (int side = -1; side <= 1; side += 2)
                AppendOvoid(vertices, uv, triangles, new Vector3(side * .23f, .16f, .02f), new Vector3(.1f, .07f, .48f), 8, 14, variant + side);
            Mesh mesh = Finish($"Detailed ant pupa {variant % 4}", vertices, uv, triangles);
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh ChamberBerm(int variant, float innerRadius = 1.35f, float outerRadius = 2.15f)
        {
            string key = $"chamber-berm-{variant}-{innerRadius:F2}-{outerRadius:F2}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int segments = 64;
            const int bands = 7;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (int band = 0; band <= bands; band++)
            {
                float radialT = band / (float)bands;
                float radius = Mathf.Lerp(innerRadius, outerRadius, radialT);
                float mound = Mathf.Sin(radialT * Mathf.PI);
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float angle = u * Mathf.PI * 2f;
                    float irregular = 1f + Mathf.Sin(angle * 3f + variant) * .05f +
                                      Mathf.Sin(angle * 7f - variant * .7f) * .025f;
                    float forwardDelta = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 90f));
                    float mouth = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(24f, 58f, forwardDelta));
                    float height = (.06f + mound * (.54f + Mathf.Sin(angle * 5f + variant) * .07f)) * mouth;
                    Vector3 p = new(Mathf.Cos(angle) * radius * irregular, height, Mathf.Sin(angle) * radius * irregular);
                    vertices.Add(p);
                    uv.Add(new Vector2(u * 3.2f, radialT * 1.4f));
                    colors.Add(new Color(mound, mouth, .25f + variant * .03f, 1f));
                }
            }
            int row = segments + 1;
            for (int band = 0; band < bands; band++)
            for (int segment = 0; segment < segments; segment++)
            {
                int a = band * row + segment;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            Mesh mesh = Finish($"Irregular packed-soil chamber berm {variant}", vertices, uv, triangles, colors);
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh SoilClod(int variant)
        {
            string key = $"soil-clod-{variant % 9}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            Mesh mesh = ParametricOvoid(
                key,
                14,
                22,
                (p, longitude, latitude) =>
                {
                    float noise = Mathf.PerlinNoise(
                        p.x * 3.7f + variant * 1.19f,
                        p.z * 4.1f + p.y * 2.2f + 17f);
                    p *= .47f + noise * .09f;
                    p.y *= .65f;
                    p.x *= 1.1f;
                    return p;
                });
            mesh.name = $"Rounded packed-soil clod {variant % 9}";
            Cache[key] = mesh;
            return mesh;
        }

        static Mesh ParametricOvoid(
            string name,
            int rings,
            int sectors,
            Func<Vector3, float, float, Vector3> sculpt)
        {
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
                        Mathf.Cos(latitude) * Mathf.Sin(longitude),
                        Mathf.Sin(latitude));
                    vertices.Add(sculpt(p, longitude, latitude));
                    uv.Add(new Vector2(u * 2f, v));
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
            return Finish(name, vertices, uv, triangles);
        }

        static void AppendOvoid(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<int> triangles,
            Vector3 center,
            Vector3 scale,
            int rings,
            int sectors,
            float phase)
        {
            int start = vertices.Count;
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
                        Mathf.Cos(latitude) * Mathf.Sin(longitude),
                        Mathf.Sin(latitude));
                    float irregular = 1f + Mathf.Sin(longitude * 3f + latitude * 4f + phase) * .025f;
                    vertices.Add(center + Vector3.Scale(p * irregular, scale));
                    uv.Add(new Vector2(u * 1.7f, v));
                }
            }
            int row = sectors + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int sector = 0; sector < sectors; sector++)
            {
                int a = start + ring * row + sector;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
        }

        static Mesh Finish(
            string name,
            List<Vector3> vertices,
            List<Vector2> uv,
            List<int> triangles,
            List<Color> colors = null)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            if (colors != null && colors.Count == vertices.Count) mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
