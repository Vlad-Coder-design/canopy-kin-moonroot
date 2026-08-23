using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Closed, collidable excavation meshes for the playable colony. Chambers
    /// and tunnels are generated as curved three-dimensional surfaces rather
    /// than flat floors beneath an open dome.
    /// </summary>
    public static class NestGeometryFactory
    {
        static readonly Dictionary<string, Mesh> Cache = new();

        public static Mesh ChamberShell(
            int variant,
            Vector3 radii,
            params float[] portalAnglesDegrees)
        {
            string portals = portalAnglesDegrees == null
                ? "none"
                : string.Join("-", portalAnglesDegrees);
            string key = $"nest-chamber-shell-{variant}-{radii.x:F2}-{radii.y:F2}-{radii.z:F2}-{portals}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int segments = 56;
            const int levels = 14;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            for (int level = 0; level <= levels; level++)
            {
                float v = level / (float)levels;
                float elevation = v * Mathf.PI * .5f;
                float radial = Mathf.Cos(elevation);
                float y = Mathf.Sin(elevation) * radii.y;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float angle = u * Mathf.PI * 2f;
                    float irregular = 1f +
                        Mathf.Sin(angle * 3f + level * .62f + variant) * .045f +
                        Mathf.Sin(angle * 7f - level * .41f + variant * .7f) * .022f;
                    float excavation = (Mathf.PerlinNoise(
                        segment * .17f + variant * 1.9f,
                        level * .29f + 12.7f) - .5f) * .11f;
                    Vector3 point = new(
                        Mathf.Cos(angle) * radii.x * radial * irregular,
                        y + excavation * (1f - v * .55f),
                        Mathf.Sin(angle) * radii.z * radial * irregular);
                    vertices.Add(point);
                    uv.Add(new Vector2(u * 4.2f, v * 2.8f));
                    colors.Add(new Color(v, irregular - .94f, excavation + .5f, 1f));
                }
            }

            int row = segments + 1;
            for (int level = 0; level < levels; level++)
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = (segment + .5f) / segments * 360f;
                bool portal = level < levels * .42f && IsPortal(angle, portalAnglesDegrees);
                if (portal) continue;
                int a = level * row + segment;
                int b = a + row;
                // Reverse winding so lighting and shadows describe the inner
                // excavated surface while the camera remains inside.
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
            }

            return Store(key, Finish($"Organic closed chamber shell {variant}",
                vertices, uv, colors, triangles));
        }

        public static Mesh ChamberFloor(int variant, Vector2 radii)
        {
            string key = $"nest-chamber-floor-{variant}-{radii.x:F2}-{radii.y:F2}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int rings = 12;
            const int segments = 56;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (int ring = 0; ring <= rings; ring++)
            {
                float radial = ring / (float)rings;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float angle = u * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * radii.x * radial;
                    float z = Mathf.Sin(angle) * radii.y * radial;
                    float broad = (Mathf.PerlinNoise(
                        x * .72f + variant * 2.1f + 11f,
                        z * .72f + variant * .9f + 27f) - .5f) * .15f;
                    float grains = (Mathf.PerlinNoise(x * 2.7f + 5f, z * 2.7f + 19f) - .5f) * .035f;
                    float worn = -Mathf.Exp(-Mathf.Pow(z / Mathf.Max(.2f, radii.y * .34f), 2f)) * .045f;
                    float rim = Mathf.Pow(radial, 5f) * .11f;
                    vertices.Add(new Vector3(x, broad + grains + worn + rim, z));
                    uv.Add(new Vector2(x / 1.4f, z / 1.4f));
                    colors.Add(new Color(radial, broad + .5f, grains + .5f, 1f));
                }
            }
            int topVertexCount = vertices.Count;
            for (int i = 0; i < topVertexCount; i++)
            {
                vertices.Add(vertices[i] + Vector3.down * .18f);
                uv.Add(uv[i]);
                colors.Add(colors[i]);
            }
            int row = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                int a = ring * row + segment;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);

                int lowerA = topVertexCount + a;
                int lowerB = topVertexCount + b;
                triangles.Add(lowerA); triangles.Add(lowerA + 1); triangles.Add(lowerB);
                triangles.Add(lowerA + 1); triangles.Add(lowerB + 1); triangles.Add(lowerB);
            }

            int outer = rings * row;
            for (int segment = 0; segment < segments; segment++)
            {
                int topA = outer + segment;
                int topB = topA + 1;
                int lowerA = topVertexCount + topA;
                int lowerB = topVertexCount + topB;
                triangles.Add(topA); triangles.Add(lowerA); triangles.Add(topB);
                triangles.Add(topB); triangles.Add(lowerA); triangles.Add(lowerB);
            }
            return Store(key, Finish($"Uneven packed chamber floor {variant}",
                vertices, uv, colors, triangles));
        }

        public static Mesh TunnelShell(
            int variant,
            IReadOnlyList<Vector3> path,
            float radius,
            float height)
        {
            string key = $"nest-tunnel-shell-{variant}-{path.Count}-{radius:F2}-{height:F2}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            const int crossSegments = 18;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            for (int pointIndex = 0; pointIndex < path.Count; pointIndex++)
            {
                Vector3 tangent = PathTangent(path, pointIndex);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float pathT = pointIndex / (float)(path.Count - 1);
                for (int cross = 0; cross <= crossSegments; cross++)
                {
                    float u = cross / (float)crossSegments;
                    float angle = u * Mathf.PI;
                    float irregular = 1f + Mathf.Sin(cross * .91f + pointIndex * .77f + variant) * .045f;
                    Vector3 offset = side * Mathf.Cos(angle) * radius * irregular +
                                     Vector3.up * Mathf.Sin(angle) * height * irregular;
                    float gouge = (Mathf.PerlinNoise(
                        cross * .21f + variant,
                        pointIndex * .43f + 9f) - .5f) * .055f;
                    vertices.Add(path[pointIndex] + offset + tangent * gouge);
                    uv.Add(new Vector2(u * 2.2f, pathT * 4f));
                    colors.Add(new Color(Mathf.Sin(angle), pathT, gouge + .5f, 1f));
                }
            }
            int row = crossSegments + 1;
            for (int pointIndex = 0; pointIndex < path.Count - 1; pointIndex++)
            for (int cross = 0; cross < crossSegments; cross++)
            {
                int a = pointIndex * row + cross;
                int b = a + row;
                // Inward winding: the player and camera travel inside this
                // half-pipe, so front faces point toward the centreline.
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            return Store(key, Finish($"Curved excavated tunnel shell {variant}",
                vertices, uv, colors, triangles));
        }

        public static Mesh TunnelFloor(
            int variant,
            IReadOnlyList<Vector3> path,
            float halfWidth)
        {
            string key = $"nest-tunnel-floor-{variant}-{path.Count}-{halfWidth:F2}";
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            const int widthSegments = 6;
            const float thickness = .16f;
            int row = widthSegments + 1;

            for (int surface = 0; surface < 2; surface++)
            for (int pointIndex = 0; pointIndex < path.Count; pointIndex++)
            {
                Vector3 tangent = PathTangent(path, pointIndex);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                for (int widthIndex = 0; widthIndex <= widthSegments; widthIndex++)
                {
                    float across = widthIndex / (float)widthSegments * 2f - 1f;
                    float irregular = (Mathf.PerlinNoise(
                        pointIndex * .61f + variant,
                        widthIndex * .47f + 3f) - .5f) * .055f;
                    float worn = -Mathf.Exp(-Mathf.Pow(across / .38f, 2f)) * .035f;
                    Vector3 point = path[pointIndex] + side * halfWidth * across +
                                    Vector3.up * (irregular + worn - (surface == 0 ? 0 : thickness));
                    vertices.Add(point);
                    uv.Add(new Vector2(widthIndex / (float)widthSegments,
                        pointIndex / (float)(path.Count - 1) * 3f));
                    colors.Add(new Color(Mathf.Abs(across), irregular + .5f,
                        pointIndex / (float)(path.Count - 1), 1f));
                }
            }
            int surfaceStride = path.Count * row;
            for (int pointIndex = 0; pointIndex < path.Count - 1; pointIndex++)
            for (int widthIndex = 0; widthIndex < widthSegments; widthIndex++)
            {
                int a = pointIndex * row + widthIndex;
                int b = a + row;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                int c = surfaceStride + a;
                int d = c + row;
                triangles.Add(c); triangles.Add(c + 1); triangles.Add(d);
                triangles.Add(c + 1); triangles.Add(d + 1); triangles.Add(d);
            }
            for (int pointIndex = 0; pointIndex < path.Count - 1; pointIndex++)
            {
                AddSide(triangles, pointIndex * row, (pointIndex + 1) * row,
                    surfaceStride + pointIndex * row, surfaceStride + (pointIndex + 1) * row, true);
                int right = pointIndex * row + widthSegments;
                AddSide(triangles, right, right + row,
                    surfaceStride + right, surfaceStride + right + row, false);
            }
            return Store(key, Finish($"Solid worn tunnel floor {variant}",
                vertices, uv, colors, triangles));
        }

        static bool IsPortal(float angle, IReadOnlyList<float> portals)
        {
            if (portals == null) return false;
            for (int i = 0; i < portals.Count; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(angle, portals[i])) < 20f)
                    return true;
            return false;
        }

        static Vector3 PathTangent(IReadOnlyList<Vector3> path, int index)
        {
            Vector3 tangent = index == 0 ? path[1] - path[0] :
                index == path.Count - 1 ? path[index] - path[index - 1] :
                path[index + 1] - path[index - 1];
            tangent.y = 0;
            if (tangent.sqrMagnitude < .001f) tangent = Vector3.forward;
            return tangent.normalized;
        }

        static void AddSide(
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

        static Mesh Finish(
            string name,
            List<Vector3> vertices,
            List<Vector2> uv,
            List<Color> colors,
            List<int> triangles)
        {
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
