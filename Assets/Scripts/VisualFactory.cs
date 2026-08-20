using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CanopyKin
{
    /// <summary>
    /// Runtime art kit for the vertical slice. All visible geometry is built from
    /// original organic meshes; Unity primitive renderers are never exposed.
    /// </summary>
    public static class VisualFactory
    {
        static readonly Dictionary<string, Material> Materials = new();
        static Mesh unitBox;
        static Mesh unitTube;

        public static Material Material(Color color, float smoothness = .2f)
            => PbrMaterial(null, color, smoothness, 0);

        public static Material PbrMaterial(
            string textureFolder,
            Color tint,
            float smoothness = .18f,
            float normalStrength = 1f,
            Vector2? tiling = null)
        {
            // Every PBR material created by this path is a solid surface.
            // Forcing alpha here prevents a source FBX material or texture alpha
            // channel from silently switching an ant or environment mesh to a
            // transparent render state.
            tint.a = 1f;
            Vector2 tile = tiling ?? Vector2.one;
            Color32 c = tint;
            string key = $"{textureFolder}|{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}|{smoothness:F2}|{normalStrength:F2}|{tile.x:F1},{tile.y:F1}";
            if (Materials.TryGetValue(key, out Material cached)) return cached;

            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = tint,
                enableInstancing = true,
                name = $"Moonroot PBR {textureFolder ?? "shell"}"
            };
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", normalStrength);
            ConfigureOpaque(material);
            if (!string.IsNullOrEmpty(textureFolder))
            {
                // Forest-floor scans belong on the outdoor terrain only. Reusing
                // leaf-litter albedo inside the nest made the chamber orange and
                // visually destroyed the distinction between packed earth and the
                // surface biome.
                bool highQualityGround = textureFolder == "ForestFloor";
                bool antExoskeleton = textureFolder == "AntExoskeleton";
                string highQualityRoot = highQualityGround
                    ? "HighQuality/PolyHaven/ForestFloor/forest_floor"
                    : antExoskeleton
                        ? "HighQuality/Original/Ant/ant_exoskeleton"
                        : null;
                string resolution = highQualityGround ? "8k" : "4k";
                Texture2D albedo = highQualityRoot != null
                    ? Resources.Load<Texture2D>($"{highQualityRoot}_diff_{resolution}")
                    : null;
                Texture2D normal = highQualityRoot != null
                    ? Resources.Load<Texture2D>($"{highQualityRoot}_nor_dx_{resolution}")
                    : null;
                Texture2D roughness = highQualityRoot != null
                    ? Resources.Load<Texture2D>($"{highQualityRoot}_rough_{resolution}")
                    : null;
                Texture2D occlusion = highQualityRoot != null
                    ? Resources.Load<Texture2D>($"{highQualityRoot}_ao_{resolution}")
                    : null;
                Texture2D height = highQualityGround
                    ? Resources.Load<Texture2D>($"{highQualityRoot}_disp_8k")
                    : null;
                albedo = albedo ? albedo : Resources.Load<Texture2D>($"Textures/{textureFolder}/albedo");
                normal = normal ? normal : Resources.Load<Texture2D>($"Textures/{textureFolder}/normal");
                roughness = roughness ? roughness : Resources.Load<Texture2D>($"Textures/{textureFolder}/roughness");
                occlusion = occlusion ? occlusion : Resources.Load<Texture2D>($"Textures/{textureFolder}/ao");
                ConfigureTexture(albedo);
                ConfigureTexture(normal);
                ConfigureTexture(roughness);
                ConfigureTexture(occlusion);
                ConfigureTexture(height);
                if (albedo) material.SetTexture("_MainTex", albedo);
                if (normal) material.SetTexture("_BumpMap", normal);
                if (roughness) material.SetTexture("_RoughnessMap", roughness);
                if (occlusion) material.SetTexture("_OcclusionMap", occlusion);
                if (height) material.SetTexture("_HeightMap", height);
                if (material.HasProperty("_Parallax"))
                    material.SetFloat("_Parallax", highQualityGround
                        ? RuntimeQualityProfile.IsFullQuality ? .035f : .012f
                        : 0f);
                material.SetTextureScale("_MainTex", tile);
                material.SetTextureScale("_BumpMap", tile);
                material.SetTextureScale("_RoughnessMap", tile);
                material.SetTextureScale("_OcclusionMap", tile);
                material.SetTextureScale("_HeightMap", tile);
            }
            Materials[key] = material;
            return material;
        }

        public static void ConfigureOpaque(Material material)
        {
            if (!material) return;
            Color color = material.color;
            color.a = 1f;
            material.color = color;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 0);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1);
            if (material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
        }

        public static Material VegetationMaterial(Color color)
        {
            Color32 c = color;
            // Procedural placement used to create hundreds of almost-identical
            // materials, which prevented the shared blade meshes from instancing.
            // A restrained shared palette preserves natural variation while
            // allowing the renderer to batch dense vegetation.
            c.r = (byte)Mathf.Min(255, Mathf.RoundToInt(c.r / 64f) * 64);
            c.g = (byte)Mathf.Min(255, Mathf.RoundToInt(c.g / 64f) * 64);
            c.b = (byte)Mathf.Min(255, Mathf.RoundToInt(c.b / 64f) * 64);
            c.a = 255;
            color = c;
            string key = $"vegetation-{c.r:X2}{c.g:X2}{c.b:X2}";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinVegetation") ?? Shader.Find("Diffuse");
            var material = new Material(shader)
            {
                name = "Moonroot living foliage",
                color = color,
                enableInstancing = true
            };
            material.SetColor("_Color", color);
            Texture2D albedo = Resources.Load<Texture2D>("Textures/Moss/albedo");
            ConfigureTexture(albedo);
            if (albedo) material.SetTexture("_MainTex", albedo);
            Materials[key] = material;
            return material;
        }

        public static Material HeroVegetationMaterial(Color color)
        {
            Color32 c = color;
            c.r = (byte)Mathf.Min(255, Mathf.RoundToInt(c.r / 32f) * 32);
            c.g = (byte)Mathf.Min(255, Mathf.RoundToInt(c.g / 32f) * 32);
            c.b = (byte)Mathf.Min(255, Mathf.RoundToInt(c.b / 32f) * 32);
            c.a = 255;
            string key = $"hero-vegetation-{c.r:X2}{c.g:X2}{c.b:X2}";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinHeroVegetation") ??
                            Shader.Find("Transparent/Cutout/Diffuse");
            var material = new Material(shader)
            {
                name = "Moonroot close-up veined grass",
                color = c,
                enableInstancing = true
            };
            material.SetColor("_Color", c);
            Texture2D atlas = Resources.Load<Texture2D>(
                "HighQuality/Original/Vegetation/moonroot_grass_atlas_v1");
            if (atlas)
            {
                atlas.wrapMode = TextureWrapMode.Clamp;
                atlas.filterMode = FilterMode.Trilinear;
                atlas.anisoLevel = RuntimeQualityProfile.IsFullQuality ? 16 : 4;
                material.SetTexture("_MainTex", atlas);
            }
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", .3f);
            if (material.HasProperty("_WindStrength"))
                material.SetFloat("_WindStrength", RuntimeQualityProfile.IsFullQuality ? .075f : .045f);
            Materials[key] = material;
            return material;
        }

        static Material HeroGroundMaterial()
        {
            const string key = "hero-ground-blend-v1";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinHeroGround") ??
                            Resources.Load<Shader>("CanopyKinLit") ??
                            Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Moonroot layered microhabitat ground",
                color = Color.white
            };
            Texture2D soilAlbedo = Resources.Load<Texture2D>("Textures/Soil/albedo");
            Texture2D soilNormal = Resources.Load<Texture2D>("Textures/Soil/normal");
            Texture2D soilRoughness = Resources.Load<Texture2D>("Textures/Soil/roughness");
            Texture2D soilAo = Texture2D.whiteTexture;
            Texture2D mossAlbedo = Resources.Load<Texture2D>("Textures/Moss/albedo");
            Texture2D mossNormal = Resources.Load<Texture2D>("Textures/Moss/normal");
            Texture2D leafAlbedo = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/ForestFloor/forest_floor_diff_8k");
            Texture2D leafNormal = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/ForestFloor/forest_floor_nor_dx_8k");
            Texture2D[] textures =
            {
                soilAlbedo, soilNormal, soilRoughness, soilAo,
                mossAlbedo, mossNormal, leafAlbedo, leafNormal
            };
            foreach (Texture2D texture in textures) ConfigureTexture(texture);
            if (soilAlbedo) material.SetTexture("_SoilAlbedo", soilAlbedo);
            if (soilNormal) material.SetTexture("_SoilNormal", soilNormal);
            if (soilRoughness) material.SetTexture("_SoilRoughness", soilRoughness);
            if (soilAo) material.SetTexture("_SoilAO", soilAo);
            if (mossAlbedo) material.SetTexture("_MossAlbedo", mossAlbedo);
            if (mossNormal) material.SetTexture("_MossNormal", mossNormal);
            if (leafAlbedo) material.SetTexture("_LeafAlbedo", leafAlbedo);
            if (leafNormal) material.SetTexture("_LeafNormal", leafNormal);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", 1.24f);
            Materials[key] = material;
            return material;
        }

        public static Material WaterMaterial()
        {
            const string key = "water";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinWater") ?? Shader.Find("Transparent/Diffuse");
            var material = new Material(shader) { name = "Rainwater" };
            material.SetColor("_Color", new Color(.035f, .19f, .18f, .72f));
            Materials[key] = material;
            return material;
        }

        static Material HeroWaterMaterial()
        {
            const string key = "hero-water-v1";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinHeroWater") ??
                            Resources.Load<Shader>("CanopyKinWater") ??
                            Shader.Find("Transparent/Diffuse");
            var material = new Material(shader) { name = "Surface-tension rainwater" };
            material.SetColor("_Color", new Color(.045f, .18f, .17f, .8f));
            if (material.HasProperty("_EdgeColor"))
                material.SetColor("_EdgeColor", new Color(.48f, .7f, .58f, .92f));
            Materials[key] = material;
            return material;
        }

        static Material HeroLeafMaterial()
        {
            const string key = "hero-dead-leaf-atlas-v1";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinHeroLeaf") ??
                            Shader.Find("Transparent/Cutout/Diffuse");
            var material = new Material(shader)
            {
                name = "Moonroot photoreal dead leaves",
                color = Color.white,
                enableInstancing = true
            };
            Texture2D atlas = Resources.Load<Texture2D>(
                "HighQuality/Original/Vegetation/moonroot_dead_leaf_atlas_v1");
            Texture2D normal = Resources.Load<Texture2D>("Textures/LeafLitter/normal");
            Texture2D roughness = Resources.Load<Texture2D>("Textures/LeafLitter/roughness");
            if (atlas)
            {
                atlas.wrapMode = TextureWrapMode.Clamp;
                atlas.filterMode = FilterMode.Trilinear;
                atlas.anisoLevel = RuntimeQualityProfile.IsFullQuality ? 16 : 4;
                material.SetTexture("_MainTex", atlas);
            }
            ConfigureTexture(normal);
            ConfigureTexture(roughness);
            if (normal) material.SetTexture("_BumpMap", normal);
            if (roughness) material.SetTexture("_RoughnessMap", roughness);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", .22f);
            Materials[key] = material;
            return material;
        }

        static void ConfigureTexture(Texture2D texture)
        {
            if (!texture) return;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = RuntimeQualityProfile.IsFullQuality ? 16 : 4;
        }

        static Material ProductionBarkMaterial()
        {
            const string materialKey = "polyhaven-dead-tree-trunk";
            if (Materials.TryGetValue(materialKey, out Material cached)) return cached;

            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Dead Tree Trunk 4K PBR",
                color = Color.white,
                enableInstancing = true
            };
            Texture2D diffuse = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_diff_4k");
            Texture2D normal = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_nor_dx_4k");
            Texture2D arm = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_arm_4k");
            ConfigureTexture(diffuse);
            ConfigureTexture(normal);
            ConfigureTexture(arm);
            if (diffuse) material.SetTexture("_MainTex", diffuse);
            if (normal) material.SetTexture("_BumpMap", normal);
            if (arm) material.SetTexture("_PackedArm", arm);
            if (material.HasProperty("_UsePackedArm")) material.SetFloat("_UsePackedArm", 1f);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", 1.08f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
            if (material.HasProperty("_Occlusion")) material.SetFloat("_Occlusion", .95f);
            Materials[materialKey] = material;
            return material;
        }

        public static GameObject ProductionDeadTree(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject prefab = Resources.Load<GameObject>(
                "HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_4k");
            if (!prefab) return null;

            GameObject root = UnityEngine.Object.Instantiate(prefab, parent, false);
            root.name = "Poly Haven dead tree trunk landmark";
            root.transform.position = position;
            root.transform.rotation = rotation;
            root.transform.localScale = scale;

            Material material = ProductionBarkMaterial();

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                if (slots.Length == 0) slots = new Material[1];
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            MeshFilter collisionSource = root.GetComponentsInChildren<MeshFilter>(true)
                .OrderByDescending(filter =>
                    filter.sharedMesh ? filter.sharedMesh.triangles.Length : 0)
                .FirstOrDefault();
            if (collisionSource && collisionSource.sharedMesh)
            {
                MeshCollider collider = collisionSource.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = collisionSource.sharedMesh;
            }
            return root;
        }

        public static GameObject ProductionRootNetwork(
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            bool collider = true)
        {
            GameObject prefab = Resources.Load<GameObject>(
                "Models/Environment/CanopyKinRootNetwork");
            if (!prefab) return null;

            GameObject root = UnityEngine.Object.Instantiate(prefab, parent, false);
            root.name = "Authored branching root network";
            root.transform.position = position;
            root.transform.rotation = rotation;
            root.transform.localScale = scale;

            Material material = ProductionBarkMaterial();
            MeshRenderer high = null;
            MeshRenderer low = null;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer.name.Contains("LOD0")) high = renderer;
                else if (renderer.name.Contains("LOD1")) low = renderer;
            }

            if (high && low)
            {
                LODGroup group = root.GetComponent<LODGroup>();
                if (!group) group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(new[]
                {
                    new LOD(.32f, new Renderer[] { high }),
                    new LOD(.055f, new Renderer[] { low })
                });
                group.RecalculateBounds();

                if (collider)
                {
                    MeshFilter lowFilter = low.GetComponent<MeshFilter>();
                    if (lowFilter && lowFilter.sharedMesh)
                    {
                        MeshCollider meshCollider = low.gameObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = lowFilter.sharedMesh;
                    }
                }
            }
            return root;
        }

        public static GameObject MeshObject(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider = false)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (collider)
                item.AddComponent<MeshCollider>().sharedMesh = mesh;
            return item;
        }

        // Compatibility entry point. It now substitutes bespoke meshes for every
        // old visible primitive so no default Unity sphere/cylinder survives.
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
            Mesh mesh = type switch
            {
                PrimitiveType.Cube => UnitBox(),
                PrimitiveType.Cylinder or PrimitiveType.Capsule => UnitTube(),
                PrimitiveType.Quad or PrimitiveType.Plane => UnitBox(),
                _ => OrganicMeshFactory.Body(OrganicMeshFactory.BodyShape.SpiderBody)
            };
            return MeshObject(name, parent, mesh, localPosition, localScale, Material(color, smoothness), keepCollider);
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
            Vector3 delta = localEnd - localStart;
            Vector3 lateral = Vector3.Cross(delta.normalized, Vector3.up);
            if (lateral.sqrMagnitude < .01f) lateral = Vector3.right;
            lateral.Normalize();
            Vector3 bow = lateral * Mathf.Min(.08f, delta.magnitude * .06f);
            var path = new[] { localStart, Vector3.Lerp(localStart, localEnd, .5f) + bow, localEnd };
            var radii = new[] { radius, radius * 1.04f, radius * .72f };
            Mesh mesh = OrganicMeshFactory.Tube(path, radii, 7);
            return MeshObject(name, parent, mesh, Vector3.zero, Vector3.one, Material(color, smoothness), keepCollider);
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
            Vector3 localStart = parent ? parent.InverseTransformPoint(start) : start;
            Vector3 localEnd = parent ? parent.InverseTransformPoint(end) : end;
            GameObject segment = Segment(name, parent, localStart, localEnd, radius, color, keepCollider, smoothness);
            return segment;
        }

        public static GameObject TexturedRoot(
            string name,
            Transform parent,
            IReadOnlyList<Vector3> path,
            IReadOnlyList<float> radii,
            bool collider = true)
        {
            Mesh mesh = OrganicMeshFactory.Tube(path, radii, 12);
            return MeshObject(
                name,
                parent,
                mesh,
                Vector3.zero,
                Vector3.one,
                PbrMaterial("Bark", new Color(.68f, .61f, .54f), .12f, 1.2f, new Vector2(2.2f, 5.5f)),
                collider);
        }

        public static GameObject OrganicPart(
            string name,
            Transform parent,
            OrganicMeshFactory.BodyShape shape,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            float smoothness = .35f,
            bool collider = false)
            => MeshObject(name, parent, OrganicMeshFactory.Body(shape), localPosition, localScale, Material(color, smoothness), collider);

        public static GameObject Stone(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int variant,
            bool collider = true,
            bool moss = true)
        {
            Material material = moss
                ? PbrMaterial("Moss", new Color(.83f, .9f, .78f), .12f, 1.15f, new Vector2(1.7f, 1.7f))
                : PbrMaterial("Soil", new Color(.55f, .55f, .56f), .08f, .8f);
            GameObject stone = MeshObject(name, parent, OrganicMeshFactory.Stone(variant % 7), position, scale, material, collider);
            stone.transform.localRotation = Quaternion.Euler(variant * 13f, variant * 47f, variant * 7f);
            return stone;
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
            var mesh = new Mesh { name = "Moonroot sculpted soil" };
            int row = resolution + 1;
            var vertices = new Vector3[row * row];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[resolution * resolution * 6];
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float px = (x / (float)resolution - .5f) * size;
                    float pz = (z / (float)resolution - .5f) * size;
                    int index = z * row + x;
                    vertices[index] = new Vector3(px, height(px, pz), pz);
                    uv[index] = new Vector2(px / 3.5f, pz / 3.5f);
                    float wet = Mathf.PerlinNoise((px + 22) * .12f, (pz + 31) * .12f);
                    colors[index] = new Color(wet, wet, wet, 1);
                }
            }
            int t = 0;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int a = z * row + x;
                int b = a + 1;
                int c = a + row;
                int d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PbrMaterial("ForestFloor", color, .05f, 1.15f, Vector2.one);
            renderer.receiveShadows = true;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            return root;
        }

        public static GameObject HeroMicroTerrain(
            Transform parent,
            Vector2 center,
            Vector2 size,
            int xSegments,
            int zSegments,
            Func<float, float, float> height)
        {
            GameObject ground = MeshObject(
                "High-density layered hero ground",
                parent,
                EnvironmentMeshFactory.MicroTerrain(
                    center,
                    size,
                    xSegments,
                    zSegments,
                    height),
                Vector3.zero,
                Vector3.one,
                HeroGroundMaterial(),
                true);
            ground.GetComponent<Renderer>().receiveShadows = true;
            ground.AddComponent<MovementSurface>().Initialize("Layered forest soil", .97f);
            return ground;
        }

        public static GameObject HeroGrassTuft(
            Transform parent,
            Vector3 position,
            float height,
            Color color,
            int variant)
        {
            var root = new GameObject("Veined reactive woodland grass");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = new Vector3(.82f, height, .82f);
            var high = MeshObject(
                "Close leaf geometry",
                root.transform,
                EnvironmentMeshFactory.HeroGrassCluster(variant),
                Vector3.zero,
                Vector3.one,
                HeroVegetationMaterial(color));
            var low = MeshObject(
                "Distant leaf geometry",
                root.transform,
                EnvironmentMeshFactory.HeroGrassCluster(variant, true),
                Vector3.zero,
                Vector3.one,
                HeroVegetationMaterial(color));
            low.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            var lod = root.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.CrossFade;
            lod.animateCrossFading = true;
            lod.SetLODs(new[]
            {
                new LOD(RuntimeQualityProfile.IsFullQuality ? .09f : .14f,
                    new Renderer[] { high.GetComponent<Renderer>() }),
                new LOD(.025f, new Renderer[] { low.GetComponent<Renderer>() })
            });
            lod.RecalculateBounds();
            return root;
        }

        public static GameObject HeroStone(
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int variant,
            bool moss)
        {
            Material material = HeroStoneMaterial(moss);
            GameObject stone = MeshObject(
                moss ? "Partly mossed fractured stone" : "Partly buried fractured stone",
                parent,
                EnvironmentMeshFactory.HeroStone(variant),
                position,
                scale,
                material,
                true);
            stone.transform.localRotation = Quaternion.Euler(
                variant * 17f % 24f - 12f,
                variant * 53f,
                variant * 11f % 18f - 9f);
            return stone;
        }

        static Material HeroStoneMaterial(bool moss)
        {
            string key = moss ? "hero-stone-mossed" : "hero-stone-dry";
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
            Color tint = moss
                ? new Color(.42f, .47f, .35f)
                : new Color(.43f, .4f, .36f);
            var material = new Material(shader)
            {
                name = moss ? "Lichen-dark fractured stone" : "Dry fractured stone",
                color = tint,
                enableInstancing = true
            };
            material.SetColor("_Color", tint);
            Texture2D normal = Resources.Load<Texture2D>("Textures/Soil/normal");
            Texture2D roughness = Resources.Load<Texture2D>("Textures/Soil/roughness");
            ConfigureTexture(normal);
            ConfigureTexture(roughness);
            if (normal) material.SetTexture("_BumpMap", normal);
            if (roughness) material.SetTexture("_RoughnessMap", roughness);
            if (material.HasProperty("_NormalStrength")) material.SetFloat("_NormalStrength", 1.38f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", moss ? .12f : .04f);
            Materials[key] = material;
            return material;
        }

        public static GameObject HeroFallenLeaf(
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int variant)
        {
            GameObject leaf = MeshObject(
                "Detailed curled and damaged leaf",
                parent,
                EnvironmentMeshFactory.HeroFallenLeaf(variant),
                position,
                scale,
                HeroLeafMaterial());
            leaf.transform.localRotation = Quaternion.Euler(
                -3f + variant % 4 * 1.5f,
                variant * 61f,
                (variant % 5 - 2) * 3.5f);
            return leaf;
        }

        public static GameObject MossCushion(
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int variant)
        {
            GameObject moss = MeshObject(
                "Layered velvet moss cushion",
                parent,
                EnvironmentMeshFactory.MossCushion(variant),
                position,
                scale,
                PbrMaterial("Moss", new Color(.98f, 1.16f, .72f), .08f, 1.32f,
                    new Vector2(2.4f, 2.4f)));
            moss.transform.localRotation = Quaternion.Euler(0, variant * 73f, 0);
            return moss;
        }

        public static GameObject HeroTexturedRoot(
            string name,
            Transform parent,
            IReadOnlyList<Vector3> path,
            IReadOnlyList<float> radii,
            bool collider = true)
        {
            Mesh mesh = OrganicMeshFactory.Tube(path, radii, 16);
            return MeshObject(
                name,
                parent,
                mesh,
                Vector3.zero,
                Vector3.one,
                ProductionBarkMaterial(),
                collider);
        }

        public static GameObject HeroPuddle(
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int variant)
        {
            GameObject puddle = MeshObject(
                "Irregular shallow reflective rain puddle",
                parent,
                EnvironmentMeshFactory.IrregularPuddle(variant),
                position,
                scale,
                HeroWaterMaterial());
            puddle.AddComponent<MeshCollider>().sharedMesh =
                puddle.GetComponent<MeshFilter>().sharedMesh;
            puddle.AddComponent<MovementSurface>().Initialize("Shallow water", .7f);
            return puddle;
        }

        public static GameObject GrassTuft(Transform parent, Vector3 position, float height, Color color, int variant = 0)
        {
            var root = new GameObject("Broad wind-bent grass");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = new Vector3(.82f, height, .82f);

            var high = MeshObject("Detailed leaves", root.transform, OrganicMeshFactory.BladeCluster(variant % 8), Vector3.zero, Vector3.one, VegetationMaterial(color));
            var low = MeshObject("Distant leaves", root.transform, OrganicMeshFactory.BladeCluster(variant % 8, true), Vector3.zero, Vector3.one, VegetationMaterial(color));
            low.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            var lod = root.AddComponent<LODGroup>();
            lod.SetLODs(new[]
            {
                new LOD(RuntimeQualityProfile.IsFullQuality ? .15f : .12f, new Renderer[] { high.GetComponent<Renderer>() }),
                new LOD(RuntimeQualityProfile.IsFullQuality ? .03f : .022f, new Renderer[] { low.GetComponent<Renderer>() })
            });
            lod.RecalculateBounds();
            return root;
        }

        public static void Flower(Transform parent, Vector3 position, Color petals)
        {
            var flower = new GameObject("Woodland flower").transform;
            flower.SetParent(parent, false);
            flower.position = position;
            Segment("Flexible stem", flower, Vector3.zero, new Vector3(0, 1.18f, .08f), .032f, new Color(.18f, .38f, .08f));
            OrganicPart("Pollen heart", flower, OrganicMeshFactory.BodyShape.Brood, new Vector3(0, 1.25f, .08f), Vector3.one * .24f, new Color(.88f, .53f, .08f), .22f);
            for (int i = 0; i < 7; i++)
            {
                float angle = i / 7f * Mathf.PI * 2f;
                var petal = MeshObject(
                    "Veined petal",
                    flower,
                    OrganicMeshFactory.FallenLeaf(i),
                    new Vector3(Mathf.Cos(angle) * .2f, 1.22f, Mathf.Sin(angle) * .2f + .08f),
                    new Vector3(.42f, .42f, .36f),
                    Material(petals, .18f));
                petal.transform.localRotation = Quaternion.Euler(-12f, -angle * Mathf.Rad2Deg, 0);
            }
        }

        public static void Mushroom(Transform parent, Vector3 position, float scale, Color cap)
        {
            var mushroom = new GameObject("Ribbed mooncap").transform;
            mushroom.SetParent(parent, false);
            mushroom.position = position;
            Segment("Tapered stem", mushroom, Vector3.zero, new Vector3(0, scale * .72f, 0), scale * .12f, new Color(.62f, .52f, .38f), false, .12f);
            GameObject crown = OrganicPart(
                "Undulating cap",
                mushroom,
                OrganicMeshFactory.BodyShape.BeetleShell,
                new Vector3(0, scale * .79f, 0),
                new Vector3(scale, scale * .36f, scale),
                cap,
                .24f);
            crown.transform.localRotation = Quaternion.Euler(0, scale * 41f, 0);
        }

        public static GameObject FallenLeaf(Transform parent, Vector3 position, Vector3 scale, int variant)
        {
            GameObject leaf = MeshObject(
                "Curled rain-dark leaf",
                parent,
                OrganicMeshFactory.FallenLeaf(variant),
                position,
                scale,
                PbrMaterial("LeafLitter", new Color(.82f, .73f, .58f), .08f, .65f, new Vector2(.8f, 1.2f)));
            leaf.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            leaf.transform.localRotation = Quaternion.Euler(0, variant * 47f, (variant % 5 - 2) * 4f);
            return leaf;
        }

        public static GameObject Water(Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject water = MeshObject("Reflective rain pool", parent, UnitBox(), position, scale, WaterMaterial());
            return water;
        }

        static Mesh UnitTube()
        {
            if (unitTube) return unitTube;
            unitTube = OrganicMeshFactory.Tube(
                new[] { new Vector3(0, -1, 0), Vector3.zero, new Vector3(0, 1, 0) },
                new[] { .5f, .52f, .48f },
                10);
            unitTube.name = "Bespoke unit tube";
            return unitTube;
        }

        static Mesh UnitBox()
        {
            if (unitBox) return unitBox;
            Vector3[] vertices =
            {
                new(-.5f,-.5f,-.5f), new(.5f,-.5f,-.5f), new(.5f,.5f,-.5f), new(-.5f,.5f,-.5f),
                new(-.5f,-.5f,.5f), new(.5f,-.5f,.5f), new(.5f,.5f,.5f), new(-.5f,.5f,.5f)
            };
            int[] triangles =
            {
                0,2,1, 0,3,2, 1,2,6, 1,6,5, 5,6,7, 5,7,4,
                4,7,3, 4,3,0, 3,7,6, 3,6,2, 4,0,1, 4,1,5
            };
            unitBox = new Mesh { name = "Bespoke unit box", vertices = vertices, triangles = triangles };
            unitBox.RecalculateNormals();
            unitBox.RecalculateTangents();
            unitBox.RecalculateBounds();
            return unitBox;
        }
    }
}
