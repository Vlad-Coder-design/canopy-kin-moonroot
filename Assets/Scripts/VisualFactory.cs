using System;
using System.Collections.Generic;
using UnityEngine;

namespace CanopyKin
{
    /// <summary>
    /// Small runtime art kit used by the vertical slice. It keeps the project
    /// self-contained while producing a readable, original ant-scale world.
    /// </summary>
    public static class VisualFactory
    {
        static readonly Dictionary<long, Material> Materials = new();

        public static Material Material(Color color, float smoothness = .2f)
        {
            Color32 c = color;
            long key = c.r | ((long)c.g << 8) | ((long)c.b << 16) | ((long)c.a << 24) |
                       ((long)Mathf.RoundToInt(smoothness * 100f) << 32);
            if (Materials.TryGetValue(key, out Material cached)) return cached;

            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = color,
                enableInstancing = true,
                name = $"Moonroot {c.r:X2}{c.g:X2}{c.b:X2}"
            };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            Materials[key] = material;
            return material;
        }

        public static GameObject Primitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            bool keepCollider = false,
            float smoothness = .2f)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.GetComponent<Renderer>().sharedMaterial = Material(color, smoothness);
            if (!keepCollider && item.TryGetComponent(out Collider collider))
                UnityEngine.Object.Destroy(collider);
            return item;
        }

        public static GameObject WorldPrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool keepCollider = false,
            float smoothness = .2f)
        {
            GameObject item = Primitive(type, name, parent, Vector3.zero, scale, color, keepCollider, smoothness);
            item.transform.position = position;
            return item;
        }

        public static GameObject Segment(
            string name,
            Transform parent,
            Vector3 localStart,
            Vector3 localEnd,
            float radius,
            Color color,
            bool keepCollider = false,
            float smoothness = .2f)
        {
            Vector3 direction = localEnd - localStart;
            GameObject segment = Primitive(
                PrimitiveType.Cylinder,
                name,
                parent,
                (localStart + localEnd) * .5f,
                new Vector3(radius, direction.magnitude * .5f, radius),
                color,
                keepCollider,
                smoothness);
            segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return segment;
        }

        public static GameObject WorldSegment(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float radius,
            Color color,
            bool keepCollider = false,
            float smoothness = .2f)
        {
            Vector3 direction = end - start;
            GameObject segment = WorldPrimitive(
                PrimitiveType.Cylinder,
                name,
                parent,
                (start + end) * .5f,
                new Vector3(radius, direction.magnitude * .5f, radius),
                color,
                keepCollider,
                smoothness);
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return segment;
        }

        public static GameObject Terrain(
            string name,
            Transform parent,
            float size,
            int resolution,
            Func<float, float, float> height,
            Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var filter = root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            var collider = root.AddComponent<MeshCollider>();
            var mesh = new Mesh { name = "Moonroot rolling soil" };

            int row = resolution + 1;
            var vertices = new Vector3[row * row];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[resolution * resolution * 6];
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float px = (x / (float)resolution - .5f) * size;
                    float pz = (z / (float)resolution - .5f) * size;
                    int index = z * row + x;
                    vertices[index] = new Vector3(px, height(px, pz), pz);
                    uv[index] = new Vector2(x / (float)resolution, z / (float)resolution);
                }
            }

            int t = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int a = z * row + x;
                    int b = a + 1;
                    int c = a + row;
                    int d = c + 1;
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            collider.sharedMesh = mesh;
            renderer.sharedMaterial = Material(color, .05f);
            return root;
        }

        public static void GrassTuft(Transform parent, Vector3 position, float height, Color color)
        {
            var tuft = new GameObject("Wind grass").transform;
            tuft.SetParent(parent, false);
            tuft.position = position;
            float width = Mathf.Lerp(.025f, .055f, height / 2.8f);
            for (int i = 0; i < 3; i++)
            {
                GameObject blade = Primitive(
                    PrimitiveType.Cube,
                    "Blade",
                    tuft,
                    new Vector3((i - 1) * .07f, height * .5f, (i % 2) * .05f),
                    new Vector3(width, height, width),
                    color,
                    false,
                    .05f);
                blade.transform.localRotation = Quaternion.Euler((i - 1) * 7f, i * 37f, (i - 1) * -6f);
            }
        }

        public static void Flower(Transform parent, Vector3 position, Color petals)
        {
            var flower = new GameObject("Seed flower").transform;
            flower.SetParent(parent, false);
            flower.position = position;
            Primitive(PrimitiveType.Cylinder, "Stem", flower, new Vector3(0, .7f, 0), new Vector3(.035f, .7f, .035f), new Color(.18f, .42f, .11f));
            Primitive(PrimitiveType.Sphere, "Flower heart", flower, new Vector3(0, 1.45f, 0), Vector3.one * .13f, new Color(.9f, .62f, .13f), false, .3f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 offset = new(Mathf.Cos(angle) * .2f, 1.45f, Mathf.Sin(angle) * .2f);
                Primitive(PrimitiveType.Sphere, "Petal", flower, offset, new Vector3(.18f, .055f, .11f), petals, false, .25f)
                    .transform.localRotation = Quaternion.Euler(0, -i * 60f, 0);
            }
        }

        public static void Mushroom(Transform parent, Vector3 position, float scale, Color cap)
        {
            var mushroom = new GameObject("Mooncap mushroom").transform;
            mushroom.SetParent(parent, false);
            mushroom.position = position;
            Primitive(PrimitiveType.Cylinder, "Pale stem", mushroom, new Vector3(0, scale * .35f, 0), new Vector3(scale * .12f, scale * .35f, scale * .12f), new Color(.72f, .64f, .48f));
            Primitive(PrimitiveType.Sphere, "Mooncap", mushroom, new Vector3(0, scale * .73f, 0), new Vector3(scale * .5f, scale * .18f, scale * .5f), cap, false, .35f);
        }
    }
}
