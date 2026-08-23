using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Creates opaque, closed vegetation meshes.  Every blade and leaf has a
    /// top, underside and connecting edge wall; no alpha card or billboard is
    /// used to define its silhouette.
    /// </summary>
    public static class VolumetricVegetationMeshFactory
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh GrassCluster(int variant, bool lowDetail = false)
        {
            string key = $"solid-grass-{variant % 17}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;

            int bladeCount = lowDetail ? 5 : 11;
            int segments = lowDetail ? 4 : 10;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 104729 + (lowDetail ? 17 : 53));

            for (int blade = 0; blade < bladeCount; blade++)
            {
                float angle = Next(random, 0f, Mathf.PI * 2f);
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .31f;
                float height = Next(random, .62f, 1.08f);
                float width = Next(random, .068f, .145f);
                float lean = Next(random, .14f, .46f);
                float curl = Next(random, -.085f, .095f);
                float thickness = lowDetail ? .018f : Next(random, .012f, .02f);
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 forward = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 side = new(-forward.z, 0, forward.x);
                AddBlade(vertices, uv, colors, triangles, origin, forward, side,
                    height, width, lean, curl, thickness, segments,
                    (float)random.NextDouble());
            }

            return Store(key, Finish(lowDetail
                ? "Solid curved grass LOD"
                : "Solid curved ant-scale grass cluster", vertices, uv, colors, triangles));
        }

        public static Mesh GroundcoverCluster(int variant, bool lowDetail = false)
        {
            string key = $"solid-groundcover-{variant % 19}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;

            int specimenCount = lowDetail ? 2 : 6;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 48611 + (lowDetail ? 19 : 71));

            for (int specimen = 0; specimen < specimenCount; specimen++)
            {
                float angle = Next(random, 0f, Mathf.PI * 2f);
                float radius = Mathf.Sqrt((float)random.NextDouble()) * .34f;
                float height = Next(random, .48f, .92f);
                Vector3 origin = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 lean = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) *
                               Next(random, .05f, .2f);
                Vector3[] stemPath =
                {
                    origin,
                    origin + Vector3.up * height * .34f + lean * .12f,
                    origin + Vector3.up * height * .7f + lean * .52f,
                    origin + Vector3.up * height + lean
                };
                AddTube(vertices, uv, colors, triangles, stemPath,
                    Next(random, .018f, .029f), lowDetail ? 5 : 7,
                    (float)random.NextDouble());

                int leafCount = lowDetail ? 2 : 4;
                for (int leaf = 0; leaf < leafCount; leaf++)
                {
                    float t = Mathf.Lerp(.28f, .94f, leaf / Mathf.Max(1f, leafCount - 1f));
                    Vector3 attachment = BezierStem(stemPath, t);
                    float sideAngle = angle + (leaf % 2 == 0 ? -1f : 1f) *
                                      Next(random, .72f, 1.36f) + leaf * .31f;
                    Vector3 direction = new(
                        Mathf.Cos(sideAngle) * Next(random, .66f, .92f),
                        Next(random, .18f, .46f),
                        Mathf.Sin(sideAngle) * Next(random, .66f, .92f));
                    AddLeaf(vertices, uv, colors, triangles, attachment,
                        direction.normalized, Next(random, .27f, .48f),
                        Next(random, .11f, .22f), lowDetail ? .016f : .012f,
                        lowDetail ? 5 : 9, (float)random.NextDouble(),
                        specimen % 3);
                }
            }

            return Store(key, Finish(lowDetail
                ? "Solid woodland plant LOD"
                : "Solid sedge sorrel and seedling cluster", vertices, uv, colors, triangles));
        }

        public static Mesh FallenLeaf(int variant)
        {
            string key = $"solid-fallen-leaf-{variant % 23}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int lengthSegments = 22;
            const int widthSegments = 6;
            float thickness = .024f + variant % 3 * .004f;
            float asymmetry = ((variant % 5) - 2) * .025f;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            int row = widthSegments + 1;

            for (int surface = 0; surface < 2; surface++)
            {
                float normalOffset = surface == 0 ? thickness * .5f : -thickness * .5f;
                for (int z = 0; z <= lengthSegments; z++)
                {
                    float along = z / (float)lengthSegments;
                    // Mathf.Sin(PI) can be a tiny negative value on some platforms.
                    // Fractional Pow of that value produces NaN and invalidates the
                    // complete mesh, so clamp the analytic profile before Pow.
                    float outline = Mathf.Pow(
                        Mathf.Max(0f, Mathf.Sin(along * Mathf.PI)), .62f);
                    float damage = 1f - Mathf.Max(0,
                        Mathf.Sin(along * 33f + variant * 2.7f)) *
                        (.035f + (variant % 4) * .012f);
                    float halfWidth = outline * (.44f + asymmetry) * damage + .006f;
                    for (int x = 0; x <= widthSegments; x++)
                    {
                        float across = x / (float)widthSegments * 2f - 1f;
                        float length = Mathf.Lerp(-.72f, .72f, along);
                        float midrib = Mathf.Exp(-Mathf.Abs(across) * 9f) * .036f;
                        float veins = Mathf.Pow(Mathf.Abs(Mathf.Sin(along * Mathf.PI * 9f)), 11f) *
                                      (1f - Mathf.Abs(across)) * .012f;
                        float cup = across * across * .058f;
                        float curl = Mathf.Sin(along * Mathf.PI * 2f + variant * .7f) * .038f;
                        float edge = Mathf.Sin(along * 29f + across * 8f + variant) *
                                     Mathf.Abs(across) * .017f;
                        vertices.Add(new Vector3(
                            across * halfWidth,
                            midrib + veins + cup + curl + edge + normalOffset,
                            length + across * asymmetry));
                        uv.Add(new Vector2(x / (float)widthSegments, along));
                        colors.Add(new Color(along, .72f, (variant % 11) / 10f, 1f));
                    }
                }
            }

            int surfaceStride = (lengthSegments + 1) * row;
            for (int z = 0; z < lengthSegments; z++)
            for (int x = 0; x < widthSegments; x++)
            {
                int a = z * row + x;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                int c = surfaceStride + a;
                int d = surfaceStride + b;
                triangles.Add(c); triangles.Add(c + 1); triangles.Add(d);
                triangles.Add(c + 1); triangles.Add(d + 1); triangles.Add(d);
            }

            for (int z = 0; z < lengthSegments; z++)
            {
                AddEdgeQuad(triangles, z * row, (z + 1) * row,
                    surfaceStride + z * row, surfaceStride + (z + 1) * row, true);
                int right = z * row + widthSegments;
                AddEdgeQuad(triangles, right, right + row,
                    surfaceStride + right, surfaceStride + right + row, false);
            }
            for (int x = 0; x < widthSegments; x++)
            {
                AddEdgeQuad(triangles, x, x + 1, surfaceStride + x,
                    surfaceStride + x + 1, false);
                int end = lengthSegments * row + x;
                AddEdgeQuad(triangles, end, end + 1, surfaceStride + end,
                    surfaceStride + end + 1, true);
            }

            return Store(key, Finish($"Solid curled veined fallen leaf {variant % 23}",
                vertices, uv, colors, triangles));
        }

        public static Mesh CanopyCluster(int variant, bool lowDetail = false)
        {
            string key = $"solid-canopy-{variant % 13}-{lowDetail}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            int leafCount = lowDetail ? 10 : 24;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var random = new System.Random(variant * 65537 + (lowDetail ? 113 : 211));
            for (int i = 0; i < leafCount; i++)
            {
                float y = Next(random, -.78f, .78f);
                float angle = Next(random, 0f, Mathf.PI * 2f);
                float radial = Mathf.Sqrt(Mathf.Max(0, 1f - y * y));
                Vector3 outward = new(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);
                Vector3 origin = Vector3.Scale(outward,
                    new Vector3(1.05f, .72f, .94f)) * Next(random, .18f, .72f);
                Vector3 direction = (outward + Vector3.up * Next(random, -.16f, .32f) +
                                     new Vector3(Next(random, -.25f, .25f), 0,
                                         Next(random, -.25f, .25f))).normalized;
                AddLeaf(vertices, uv, colors, triangles, origin, direction,
                    Next(random, .46f, .92f), Next(random, .17f, .34f),
                    lowDetail ? .026f : .021f, lowDetail ? 5 : 8,
                    (float)random.NextDouble(), i % 3);
            }
            return Store(key, Finish(lowDetail
                ? "Solid distant canopy cluster LOD"
                : "Solid individual-leaf canopy cluster", vertices, uv, colors, triangles));
        }

        static void AddBlade(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<Color> colors,
            List<int> triangles,
            Vector3 origin,
            Vector3 forward,
            Vector3 side,
            float height,
            float width,
            float lean,
            float curl,
            float thickness,
            int segments,
            float phase)
        {
            int start = vertices.Count;
            for (int y = 0; y <= segments; y++)
            {
                float t = y / (float)segments;
                float taper = Mathf.Pow(Mathf.Sin((1f - t) * Mathf.PI * .5f), .68f);
                float halfWidth = Mathf.Max(.0025f, width * taper);
                Vector3 center = origin + Vector3.up * height * t +
                                 forward * lean * t * t +
                                 side * curl * Mathf.Sin(t * Mathf.PI);
                Vector3 tangent = (Vector3.up * height + forward * lean * 2f * t +
                                   side * curl * Mathf.PI * Mathf.Cos(t * Mathf.PI)).normalized;
                Vector3 normal = Vector3.Cross(side, tangent).normalized;
                Vector3 top = normal * thickness * .5f;
                vertices.Add(center - side * halfWidth + top);
                vertices.Add(center + side * halfWidth + top);
                vertices.Add(center - side * halfWidth - top);
                vertices.Add(center + side * halfWidth - top);
                for (int i = 0; i < 4; i++)
                {
                    uv.Add(new Vector2((i & 1), t));
                    colors.Add(new Color(t, .72f, phase, 1f));
                }
            }
            AddRibbonFaces(triangles, start, segments);
        }

        static void AddLeaf(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<Color> colors,
            List<int> triangles,
            Vector3 origin,
            Vector3 direction,
            float length,
            float width,
            float thickness,
            int segments,
            float phase,
            int shape)
        {
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            if (side.sqrMagnitude < .02f) side = Vector3.Cross(Vector3.forward, direction);
            side.Normalize();
            Vector3 normal = Vector3.Cross(side, direction).normalized;
            int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float sineOutline = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI));
                float outline = shape switch
                {
                    0 => Mathf.Pow(sineOutline, .62f),
                    1 => Mathf.Pow(sineOutline, .88f) * (.8f + t * .22f),
                    _ => Mathf.Pow(sineOutline, .48f) * (1f - t * .18f)
                };
                float halfWidth = Mathf.Max(.002f, width * outline);
                Vector3 center = origin + direction * length * t +
                                 normal * Mathf.Sin(t * Mathf.PI) * length * .09f +
                                 side * Mathf.Sin(t * Mathf.PI * 2f + phase * 6f) * length * .018f;
                Vector3 raisedNormal = normal * (thickness * .5f +
                    Mathf.Exp(-Mathf.Abs(0f) * 8f) * .003f);
                vertices.Add(center - side * halfWidth + raisedNormal);
                vertices.Add(center + side * halfWidth + raisedNormal);
                vertices.Add(center - side * halfWidth - normal * thickness * .5f);
                vertices.Add(center + side * halfWidth - normal * thickness * .5f);
                for (int vertex = 0; vertex < 4; vertex++)
                {
                    uv.Add(new Vector2((vertex & 1), t));
                    colors.Add(new Color(t, .66f + shape * .1f, phase, 1f));
                }
            }
            AddRibbonFaces(triangles, start, segments);
        }

        static void AddRibbonFaces(List<int> triangles, int start, int segments)
        {
            for (int row = 0; row < segments; row++)
            {
                int a = start + row * 4;
                int b = a + 4;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                triangles.Add(a + 2); triangles.Add(a + 3); triangles.Add(b + 2);
                triangles.Add(a + 3); triangles.Add(b + 3); triangles.Add(b + 2);
                triangles.Add(a); triangles.Add(a + 2); triangles.Add(b);
                triangles.Add(a + 2); triangles.Add(b + 2); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(a + 3);
                triangles.Add(a + 3); triangles.Add(b + 1); triangles.Add(b + 3);
            }
            int first = start;
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first + 1); triangles.Add(first + 3); triangles.Add(first + 2);
            int last = start + segments * 4;
            triangles.Add(last); triangles.Add(last + 2); triangles.Add(last + 1);
            triangles.Add(last + 1); triangles.Add(last + 2); triangles.Add(last + 3);
        }

        static void AddTube(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<Color> colors,
            List<int> triangles,
            IReadOnlyList<Vector3> path,
            float radius,
            int sides,
            float phase)
        {
            int start = vertices.Count;
            for (int point = 0; point < path.Count; point++)
            {
                Vector3 tangent = point == 0 ? path[1] - path[0] :
                    point == path.Count - 1 ? path[point] - path[point - 1] :
                    path[point + 1] - path[point - 1];
                tangent.Normalize();
                Vector3 side = Vector3.Cross(tangent, Vector3.forward);
                if (side.sqrMagnitude < .01f) side = Vector3.Cross(tangent, Vector3.right);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, tangent).normalized;
                for (int ring = 0; ring <= sides; ring++)
                {
                    float u = ring / (float)sides;
                    float angle = u * Mathf.PI * 2f;
                    float taper = Mathf.Lerp(1f, .58f, point / (float)(path.Count - 1));
                    vertices.Add(path[point] +
                        (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius * taper);
                    uv.Add(new Vector2(u, point / (float)(path.Count - 1)));
                    colors.Add(new Color(point / (float)(path.Count - 1), .78f, phase, 1f));
                }
            }
            int row = sides + 1;
            for (int point = 0; point < path.Count - 1; point++)
            for (int ring = 0; ring < sides; ring++)
            {
                int a = start + point * row + ring;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
        }

        static Vector3 BezierStem(IReadOnlyList<Vector3> path, float t)
        {
            float scaled = t * (path.Count - 1);
            int index = Mathf.Min(path.Count - 2, Mathf.FloorToInt(scaled));
            return Vector3.Lerp(path[index], path[index + 1], scaled - index);
        }

        static void AddEdgeQuad(
            List<int> triangles,
            int topA,
            int topB,
            int bottomA,
            int bottomB,
            bool reverse)
        {
            if (!reverse)
            {
                triangles.Add(topA); triangles.Add(bottomA); triangles.Add(topB);
                triangles.Add(topB); triangles.Add(bottomA); triangles.Add(bottomB);
            }
            else
            {
                triangles.Add(topA); triangles.Add(topB); triangles.Add(bottomA);
                triangles.Add(topB); triangles.Add(bottomB); triangles.Add(bottomA);
            }
        }

        static float Next(System.Random random, float min, float max) =>
            Mathf.Lerp(min, max, (float)random.NextDouble());

        static Mesh Finish(
            string name,
            List<Vector3> vertices,
            List<Vector2> uv,
            List<Color> colors,
            List<int> triangles)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                if (float.IsNaN(vertex.x) || float.IsInfinity(vertex.x) ||
                    float.IsNaN(vertex.y) || float.IsInfinity(vertex.y) ||
                    float.IsNaN(vertex.z) || float.IsInfinity(vertex.z))
                {
                    throw new InvalidOperationException(
                        $"Vegetation mesh '{name}' contains a non-finite vertex at index {i}: {vertex}");
                }
            }

            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Store(string key, Mesh mesh)
        {
            Cache[key] = mesh;
            return mesh;
        }
    }
}
