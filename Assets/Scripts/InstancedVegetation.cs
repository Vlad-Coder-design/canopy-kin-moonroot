using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin
{
    /// <summary>
    /// Builds spatially combined vegetation chunks with authored high/low LODs.
    /// This keeps every placed blade, its wind vertex data and material variation,
    /// while avoiding a Renderer and LODGroup per individual tuft.
    /// </summary>
    public sealed class InstancedVegetation : MonoBehaviour
    {
        sealed class Instance
        {
            public Mesh High;
            public Mesh Low;
            public Matrix4x4 Matrix;
        }

        sealed class Batch
        {
            public Material Material;
            public readonly List<Instance> Instances = new();
        }

        const float ChunkSize = 12f;
        readonly Dictionary<string, Batch> batches = new();
        readonly List<Mesh> runtimeMeshes = new();
        bool completed;

        public void Add(
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            if (completed) return;

            Material material = VisualFactory.HeroVegetationMaterial(color);
            int cellX = Mathf.FloorToInt(position.x / ChunkSize);
            int cellZ = Mathf.FloorToInt(position.z / ChunkSize);
            string key = $"{cellX}:{cellZ}:{material.GetInstanceID()}";
            if (!batches.TryGetValue(key, out Batch batch))
            {
                batch = new Batch { Material = material };
                batches.Add(key, batch);
            }

            batch.Instances.Add(new Instance
            {
                High = VolumetricVegetationMeshFactory.GrassCluster(variant % 11),
                Low = VolumetricVegetationMeshFactory.GrassCluster(variant % 11, true),
                Matrix = Matrix4x4.TRS(position, rotation, scale)
            });
        }

        public void Complete()
        {
            if (completed) return;
            completed = true;

            int tuftCount = 0;
            int highTriangles = 0;
            int lowTriangles = 0;
            foreach (KeyValuePair<string, Batch> entry in batches)
            {
                Batch batch = entry.Value;
                if (batch.Instances.Count == 0) continue;
                tuftCount += batch.Instances.Count;

                var chunk = new GameObject($"Grass chunk {entry.Key}");
                chunk.transform.SetParent(transform, false);

                MeshRenderer high = CreateCombinedLevel(
                    chunk.transform, "Close foliage", batch, false,
                    ShadowCastingMode.On, out int closeTriangles);
                MeshRenderer low = CreateCombinedLevel(
                    chunk.transform, "Distant foliage", batch, true,
                    ShadowCastingMode.Off, out int distantTriangles);
                highTriangles += closeTriangles;
                lowTriangles += distantTriangles;

                var lodGroup = chunk.AddComponent<LODGroup>();
                lodGroup.fadeMode = LODFadeMode.CrossFade;
                lodGroup.animateCrossFading = true;
                lodGroup.SetLODs(new[]
                {
                    new LOD(.19f, new Renderer[] { high }),
                    new LOD(.035f, new Renderer[] { low })
                });
                lodGroup.RecalculateBounds();
            }

            Debug.Log(
                $"MOONROOT_VEGETATION_CHUNKS_READY chunks={batches.Count} tufts={tuftCount} " +
                $"highTriangles={highTriangles} lowTriangles={lowTriangles}");
            batches.Clear();
        }

        MeshRenderer CreateCombinedLevel(
            Transform parent,
            string name,
            Batch batch,
            bool lowDetail,
            ShadowCastingMode shadows,
            out int triangleCount)
        {
            var instances = new CombineInstance[batch.Instances.Count];
            triangleCount = 0;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 0; i < batch.Instances.Count; i++)
            {
                Instance instance = batch.Instances[i];
                Mesh source = lowDetail ? instance.Low : instance.High;
                instances[i] = new CombineInstance
                {
                    mesh = source,
                    subMeshIndex = 0,
                    transform = worldToLocal * instance.Matrix
                };
                triangleCount += (int)source.GetIndexCount(0) / 3;
            }

            var level = new GameObject(name);
            level.transform.SetParent(parent, false);
            var filter = level.AddComponent<MeshFilter>();
            var renderer = level.AddComponent<MeshRenderer>();
            var combined = new Mesh
            {
                name = $"{name} combined mesh",
                indexFormat = IndexFormat.UInt32
            };
            combined.CombineMeshes(instances, true, true, false);
            combined.RecalculateBounds();
            filter.sharedMesh = combined;
            renderer.sharedMaterial = batch.Material;
            renderer.shadowCastingMode = shadows;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            runtimeMeshes.Add(combined);
            return renderer;
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in runtimeMeshes)
                if (mesh) Destroy(mesh);
        }
    }
}
