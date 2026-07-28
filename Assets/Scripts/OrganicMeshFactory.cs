using System;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    /// <summary>
    /// Produces authored-looking organic geometry without exposing Unity primitive
    /// meshes. Meshes are cached and shared so the dense forest remains WebGL-safe.
    /// </summary>
    public static class OrganicMeshFactory
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public enum BodyShape { Head, Thorax, Abdomen, Eye, BeetleShell, SpiderBody, Brood }

        public static Mesh Body(BodyShape shape)
        {
            string key = $"body-{shape}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int rings = shape is BodyShape.Eye or BodyShape.Brood ? 8 : 12;
            int sectors = shape is BodyShape.Eye or BodyShape.Brood ? 12 : 18;
            var vertices = new List<Vector3>((rings + 1) * (sectors + 1));
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(rings * sectors * 6);

            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                float latitude = Mathf.Lerp(-Mathf.PI * .5f, Mathf.PI * .5f, v);
                float y = Mathf.Sin(latitude);
                float radial = Mathf.Cos(latitude);
                for (int s = 0; s <= sectors; s++)
                {
                    float u = s / (float)sectors;
                    float longitude = u * Mathf.PI * 2f;
                    float x = radial * Mathf.Cos(longitude);
                    float z = radial * Mathf.Sin(longitude);
                    Vector3 point = Sculpt(shape, new Vector3(x, y, z));
                    vertices.Add(point);
                    uv.Add(new Vector2(u * 1.8f, v));
                    if (r == rings || s == sectors) continue;
                    int a = r * (sectors + 1) + s;
                    int b = a + sectors + 1;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = $"Original {shape} mesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        static Vector3 Sculpt(BodyShape shape, Vector3 p)
        {
            float front = Mathf.InverseLerp(-1f, 1f, p.z);
            switch (shape)
            {
                case BodyShape.Head:
                    p.x *= Mathf.Lerp(.82f, 1.07f, front);
                    p.y *= .82f + .1f * Mathf.Cos(p.z * Mathf.PI);
                    p.z *= .92f;
                    p.y += .08f * (1f - p.z * p.z);
                    break;
                case BodyShape.Thorax:
                    p.x *= .82f + .12f * Mathf.Cos(p.z * 2.6f);
                    p.y *= .8f + .16f * (1f - Mathf.Abs(p.z));
                    p.z *= 1.12f;
                    p.y += .1f * (1f - p.z * p.z);
                    break;
                case BodyShape.Abdomen:
                    float band = 1f - Mathf.Pow(Mathf.Max(0, Mathf.Cos((p.z + 1f) * Mathf.PI * 2.65f)), 10f) * .075f;
                    float posteriorTaper = Mathf.Lerp(.67f, 1.04f, Mathf.SmoothStep(0f, 1f, 1f - front));
                    p.x *= posteriorTaper * band;
                    p.y *= (.58f + .12f * (1f - p.z * p.z)) * band;
                    p.z *= 1.32f;
                    p.y += .06f * (1f - p.z);
                    break;
                case BodyShape.Eye:
                    p.x *= .7f;
                    p.z *= .42f;
                    break;
                case BodyShape.BeetleShell:
                    p.x *= 1.08f;
                    p.y = p.y * .64f + .18f * (1f - p.x * p.x);
                    p.z *= 1.3f;
                    break;
                case BodyShape.SpiderBody:
                    p.y *= .7f;
                    p.z *= 1.18f;
                    break;
                case BodyShape.Brood:
                    p.x *= .62f;
                    p.y *= .55f;
                    p.z *= 1.25f;
                    break;
            }
            return p * .5f;
        }

        public static Mesh Tube(IReadOnlyList<Vector3> path, IReadOnlyList<float> radii, int sides = 8)
        {
            var vertices = new List<Vector3>(path.Count * (sides + 1));
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>((path.Count - 1) * sides * 6);
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 tangent = i == 0 ? path[1] - path[0] :
                    i == path.Count - 1 ? path[i] - path[i - 1] :
                    path[i + 1] - path[i - 1];
                tangent.Normalize();
                Vector3 normal = Vector3.Cross(tangent, Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .9f ? Vector3.right : Vector3.up).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal).normalized;
                for (int s = 0; s <= sides; s++)
                {
                    float angle = s / (float)sides * Mathf.PI * 2f;
                    Vector3 radial = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                    vertices.Add(path[i] + radial * radii[Mathf.Min(i, radii.Count - 1)]);
                    uv.Add(new Vector2(s / (float)sides, i / (float)(path.Count - 1)));
                    if (i == path.Count - 1 || s == sides) continue;
                    int a = i * (sides + 1) + s;
                    int b = a + sides + 1;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }
            var mesh = new Mesh { name = "Tapered organic tube" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Stone(int variant)
        {
            string key = $"stone-{variant}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            Mesh source = Body(BodyShape.SpiderBody);
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = $"Weathered stone {variant}";
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                float noise = Mathf.PerlinNoise(p.x * 5.7f + variant * 1.3f, p.z * 5.1f + p.y * 2.4f) - .5f;
                p *= 1f + noise * .25f;
                p.y *= .7f;
                vertices[i] = p;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh BladeCluster(int variant, bool lowDetail = false)
        {
            string key = $"grass-{variant}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int bladeCount = lowDetail ? 3 : 8;
            int segments = lowDetail ? 2 : 5;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 7919 + (lowDetail ? 13 : 31));
            for (int blade = 0; blade < bladeCount; blade++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = (float)random.NextDouble() * .24f;
                float height = Mathf.Lerp(.72f, 1.28f, (float)random.NextDouble());
                float width = Mathf.Lerp(.09f, .16f, (float)random.NextDouble());
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 side = new(Mathf.Cos(angle + Mathf.PI * .5f), 0, Mathf.Sin(angle + Mathf.PI * .5f));
                Vector3 bend = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                int start = vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float taper = Mathf.Sin((1f - t) * Mathf.PI * .5f);
                    Vector3 center = origin + Vector3.up * height * t + bend * (t * t * .32f);
                    vertices.Add(center - side * width * taper);
                    vertices.Add(center + side * width * taper);
                    uv.Add(new Vector2(0, t));
                    uv.Add(new Vector2(1, t));
                    Color wind = new(t, .72f + (float)random.NextDouble() * .28f, 0, 1);
                    colors.Add(wind);
                    colors.Add(wind);
                    if (i == segments) continue;
                    int a = start + i * 2;
                    triangles.Add(a);
                    triangles.Add(a + 2);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(a + 2);
                    triangles.Add(a + 3);
                }
            }
            var mesh = new Mesh { name = lowDetail ? "Grass LOD" : "Broad leaf grass" };
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

        public static Mesh FallenLeaf(int variant)
        {
            string key = $"leaf-{variant}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int sections = 8;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            for (int i = 0; i <= sections; i++)
            {
                float t = i / (float)sections;
                float width = Mathf.Sin(t * Mathf.PI) * (.42f + .08f * Mathf.Sin(t * 5f + variant));
                float z = Mathf.Lerp(-.68f, .68f, t);
                float y = Mathf.Sin(t * Mathf.PI * 2f + variant) * .025f + Mathf.Sin(t * Mathf.PI) * .055f;
                vertices.Add(new Vector3(-width, y, z));
                vertices.Add(new Vector3(width, y + .012f, z));
                uv.Add(new Vector2(0, t));
                uv.Add(new Vector2(1, t));
            }
            var triangles = new List<int>();
            for (int i = 0; i < sections; i++)
            {
                int a = i * 2;
                triangles.Add(a); triangles.Add(a + 2); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(a + 2); triangles.Add(a + 3);
            }
            var mesh = new Mesh { name = "Curled fallen leaf" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh CaveShell(int segments = 28, int levels = 8)
        {
            string key = $"cave-shell-{segments}-{levels}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int level = 0; level <= levels; level++)
            {
                float v = level / (float)levels;
                float radius = Mathf.Lerp(5.35f, .55f, Mathf.Pow(v, 1.7f));
                float y = v * 3.85f;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float angle = u * Mathf.PI * 2f;
                    float irregular = 1f + Mathf.Sin(angle * 3f + level * .7f) * .035f +
                                      Mathf.Sin(angle * 7f - level * .4f) * .018f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius * irregular, y, Mathf.Sin(angle) * radius * irregular));
                    uv.Add(new Vector2(u * 4f, v * 2.4f));
                    if (level == levels || segment == segments) continue;
                    int a = level * (segments + 1) + segment;
                    int b = a + segments + 1;
                    // Reversed winding exposes the interior surface.
                    triangles.Add(a);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(b + 1);
                    triangles.Add(b);
                }
            }
            var mesh = new Mesh { name = "Continuous sculpted cave shell" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        public static Mesh Mandible(bool left)
        {
            string key = left ? "mandible-left" : "mandible-right";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            float sign = left ? -1f : 1f;
            Vector3[] v =
            {
                new(sign * .03f, .03f, 0), new(sign * .12f, .05f, .16f),
                new(sign * .16f, 0, .31f), new(sign * .06f, -.03f, .19f),
                new(sign * .015f, -.035f, .02f), new(sign * .08f, .11f, .13f)
            };
            int[] t =
            {
                0,1,5, 1,2,5, 2,3,5, 3,0,5,
                0,4,1, 1,4,2, 2,4,3, 3,4,0
            };
            var mesh = new Mesh { name = left ? "Left hooked mandible" : "Right hooked mandible" };
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }
    }
}
