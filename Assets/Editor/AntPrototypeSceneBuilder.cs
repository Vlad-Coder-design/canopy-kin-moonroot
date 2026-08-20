using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CanopyKin.Editor
{
    public static class AntPrototypeSceneBuilder
    {
        const string ModelPath =
            "Assets/Resources/Models/Ant/Prototype/CanopyKin_FormicaRufa_Player_Prototype.fbx";
        const string ScenePath = "Assets/Scenes/AntPrototype.unity";
        const string ControllerPath = "Assets/Animation/AntPrototype.controller";
        const string GroundMeshPath = "Assets/Art/AntPrototype/AntPrototypeGround.asset";
        const string GroundMaterialPath = "Assets/Art/AntPrototype/AntPrototypeGround.mat";

        [MenuItem("Canopy Kin/Ant Prototype/Build Scene and Validate")]
        public static void BuildAndValidate()
        {
            EnsureDirectory("Assets/Animation");
            EnsureDirectory("Assets/Art/AntPrototype");
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model) throw new FileNotFoundException("Formica-rufa prototype FBX is missing", ModelPath);
            SkinnedMeshRenderer[] importedSkins =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (importedSkins.Length != 1 || !importedSkins[0].sharedMesh)
                throw new InvalidOperationException(
                    $"Prototype requires one real SkinnedMeshRenderer; imported {importedSkins.Length}.");
            SkinnedMeshRenderer importedSkin = importedSkins[0];
            int triangles = importedSkin.sharedMesh.triangles.Length / 3;
            if (triangles < 50000)
                throw new InvalidOperationException(
                    $"Prototype topology is unexpectedly small: {triangles} triangles.");

            string[] requiredBones =
            {
                "Root", "Head", "Thorax", "Petiole", "Abdomen",
                "Mandible_L", "Mandible_R",
                "Antenna_L_1", "Antenna_L_2", "Antenna_L_3",
                "Antenna_R_1", "Antenna_R_2", "Antenna_R_3",
                "Leg_L_Front_Coxa", "Leg_L_Front_Femur", "Leg_L_Front_Tibia",
                "Leg_L_Front_Tarsus", "Leg_L_Front_TarsusTip",
                "Leg_R_Rear_Coxa", "Leg_R_Rear_Femur", "Leg_R_Rear_Tibia",
                "Leg_R_Rear_Tarsus", "Leg_R_Rear_TarsusTip",
                "Leg_L_Front_Claw_Inner", "Leg_L_Front_Claw_Outer",
                "Leg_R_Rear_Claw_Inner", "Leg_R_Rear_Claw_Outer"
            };
            var bones = importedSkin.bones
                .Where(item => item)
                .Select(item => item.name)
                .ToHashSet(StringComparer.Ordinal);
            string missingBone = requiredBones.FirstOrDefault(required => !bones.Contains(required));
            if (missingBone != null)
                throw new InvalidOperationException($"Prototype missing anatomical bone: {missingBone}");

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
            if (clips.Length < 24)
                throw new InvalidOperationException(
                    $"Prototype requires 24 imported clips; imported {clips.Length}: " +
                    string.Join(", ", clips.Select(item => item.name)));
            foreach (string required in new[]
            {
                "ANT_CalmIdle", "ANT_NormalWalk", "ANT_FastRun",
                "ANT_TurnLeft", "ANT_TurnRight", "ANT_Attack_Primary"
            })
                if (clips.All(clip => !string.Equals(clip.name, required, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Prototype missing genuine clip: {required}");

            AnimatorController controller = CreateController(clips);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject stage = new("Formica rufa maximum-quality visual prototype");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            instance.name = "ACTUAL IMPORTED Formica rufa skinned player prototype";
            instance.transform.SetParent(stage.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 1.36f;
            Animator animator = instance.GetComponent<Animator>();
            if (!animator) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            GameObject ground = CreateGround();
            ground.transform.SetParent(stage.transform, false);
            ground.transform.localPosition = new Vector3(0, -.014f, 0);

            GameObject cameraObject = new("Prototype Game View Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 46f;
            camera.nearClipPlane = .015f;
            camera.farClipPlane = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.007f, .011f, .009f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();

            Light key = CreateLight(
                "Warm macro sunlight",
                LightType.Directional,
                new Color(1f, .78f, .57f),
                1.55f,
                new Vector3(46f, -34f, 0));
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .92f;
            key.shadowBias = .012f;
            key.shadowNormalBias = .08f;
            CreateLight(
                "Cool sky fill",
                LightType.Directional,
                new Color(.37f, .57f, .78f),
                .54f,
                new Vector3(118f, 42f, -24f));
            Light rim = CreateLight(
                "Chitin rim light",
                LightType.Point,
                new Color(.48f, .72f, 1f),
                8.5f,
                Vector3.zero);
            rim.transform.position = new Vector3(-1.4f, 1.1f, 1.25f);
            rim.range = 5f;
            rim.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.18f, .24f, .31f);
            RenderSettings.ambientEquatorColor = new Color(.09f, .075f, .055f);
            RenderSettings.ambientGroundColor = new Color(.025f, .018f, .012f);
            RenderSettings.ambientIntensity = .72f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.012f, .018f, .015f);
            RenderSettings.fogDensity = .012f;

            AntPrototypeShowcase showcase = stage.AddComponent<AntPrototypeShowcase>();
            showcase.Initialize(instance.transform, animator, camera, key);
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"CANOPY_KIN_FORMICA_PROTOTYPE_SCENE_OK species=Formica_rufa " +
                $"triangles={triangles} vertices={importedSkin.sharedMesh.vertexCount} " +
                $"bones={bones.Count} clips={clips.Length} skinnedRenderers={importedSkins.Length} " +
                $"scene={ScenePath} model={ModelPath} " +
                $"clipNames={string.Join("|", clips.Select(item => item.name))}");
        }

        [MenuItem("Canopy Kin/Ant Prototype/Build Windows Evidence Player")]
        public static void BuildPrototypeWindows()
        {
            BuildAndValidate();
            Directory.CreateDirectory("Builds/AntPrototype");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/AntPrototype/CanopyKinAntPrototype.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Prototype Windows evidence build failed: {report.summary.result}");
            Debug.Log(
                $"CANOPY_KIN_FORMICA_PROTOTYPE_BUILD_OK " +
                $"bytes={report.summary.totalSize} time={report.summary.totalTime}");
        }

        static AnimatorController CreateController(AnimationClip[] clips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath))
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (AnimationClip clip in clips)
            {
                AnimatorState state = stateMachine.AddState(clip.name);
                state.motion = clip;
                state.speed = 1f;
                if (string.Equals(clip.name, "ANT_CalmIdle", StringComparison.Ordinal))
                    stateMachine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static GameObject CreateGround()
        {
            Mesh mesh = BuildGroundMesh();
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath);
            if (existing) AssetDatabase.DeleteAsset(GroundMeshPath);
            AssetDatabase.CreateAsset(mesh, GroundMeshPath);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (!material)
            {
                Shader shader = Resources.Load<Shader>("CanopyKinLit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "Ant prototype soil" };
                AssetDatabase.CreateAsset(material, GroundMaterialPath);
            }
            material.color = new Color(.115f, .072f, .036f);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", material.color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", .08f);
            Texture2D albedo = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/ForestFloor/forest_floor_diff_8k");
            Texture2D normal = Resources.Load<Texture2D>(
                "HighQuality/PolyHaven/ForestFloor/forest_floor_nor_dx_8k");
            if (albedo) material.SetTexture("_MainTex", albedo);
            if (normal) material.SetTexture("_BumpMap", normal);
            material.SetTextureScale("_MainTex", new Vector2(1.4f, 2.8f));
            material.SetTextureScale("_BumpMap", new Vector2(1.4f, 2.8f));
            EditorUtility.SetDirty(material);

            GameObject ground = new("Uneven ant-scale test terrain");
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = ground.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
            return ground;
        }

        static Mesh BuildGroundMesh()
        {
            const int width = 41;
            const int depth = 61;
            var vertices = new Vector3[width * depth];
            var uv = new Vector2[vertices.Length];
            int index = 0;
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float nz = z / (float)(depth - 1);
                float worldX = Mathf.Lerp(-3.4f, 3.4f, nx);
                float worldZ = Mathf.Lerp(-4.2f, 5.8f, nz);
                float elevation =
                    Mathf.Sin(worldX * 1.7f + worldZ * .55f) * .028f +
                    Mathf.Cos(worldZ * 1.1f - worldX * .4f) * .019f;
                elevation += Mathf.Exp(-Mathf.Pow(worldX - 1.25f, 2) * 2.2f) *
                             Mathf.Exp(-Mathf.Pow(worldZ - 1.65f, 2) * .9f) * .18f;
                elevation -= Mathf.Exp(-Mathf.Pow(worldX + 1.3f, 2) * 2.5f) *
                             Mathf.Exp(-Mathf.Pow(worldZ - 2.4f, 2) * 1.4f) * .08f;
                vertices[index] = new Vector3(worldX, elevation, worldZ);
                uv[index] = new Vector2(nx, nz);
                index++;
            }
            var triangles = new int[(width - 1) * (depth - 1) * 6];
            index = 0;
            for (int z = 0; z < depth - 1; z++)
            for (int x = 0; x < width - 1; x++)
            {
                int a = z * width + x;
                int b = a + 1;
                int c = a + width;
                int d = c + 1;
                triangles[index++] = a;
                triangles[index++] = c;
                triangles[index++] = b;
                triangles[index++] = b;
                triangles[index++] = c;
                triangles[index++] = d;
            }
            Mesh mesh = new()
            {
                name = "Deliberate uneven ant-scale prototype terrain",
                vertices = vertices,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Light CreateLight(
            string name,
            LightType type,
            Color color,
            float intensity,
            Vector3 euler)
        {
            GameObject gameObject = new(name);
            Light light = gameObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.renderMode = LightRenderMode.ForcePixel;
            gameObject.transform.rotation = Quaternion.Euler(euler);
            return light;
        }

        static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }
    }
}
