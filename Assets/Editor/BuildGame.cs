using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanopyKin.Editor
{
    public static class BuildGame
    {
        const string ScenePath = "Assets/Scenes/Moonroot.unity";
        const string ProductionAntPath = "Assets/Resources/Models/Ant/CanopyKinProductionAnt.fbx";
        const string ProductionSpiderPath =
            "Assets/Resources/Models/Creatures/CanopyKinFishingSpider.fbx";
        const string SpiderAlbedoPath =
            "Assets/Resources/HighQuality/Sketchfab/FishingSpider/fishing_spider_albedo_8k.jpg";
        const string ProductionBeetlePath =
            "Assets/Resources/Models/Creatures/CanopyKinRhinocerosBeetle.fbx";
        const string BeetleAlbedoPath =
            "Assets/Resources/HighQuality/Sketchfab/RhinocerosBeetle/rhinoceros_beetle_albedo_8k.jpg";
        const string ForestFloorPath = "Assets/Resources/HighQuality/PolyHaven/ForestFloor/forest_floor_diff_8k.jpg";
        const string DeadTreePath =
            "Assets/Resources/HighQuality/PolyHaven/DeadTreeTrunk/dead_tree_trunk_4k.fbx";
        const string RootNetworkPath =
            "Assets/Resources/Models/Environment/CanopyKinRootNetwork.fbx";
        const string ProductVersion = "0.4.0";

        [MenuItem("Canopy Kin/Build Windows")]
        public static void BuildWindows()
        {
            ConfigureShared();
            ConfigureWindows();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.moonroot.canopykin");
            Directory.CreateDirectory("Builds/Windows");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            BuildReport report = BuildPipeline.BuildPlayer(
                EnabledScenes(),
                "Builds/Windows/CanopyKin.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            RequireSuccess(report, "Windows");
            WriteManifest("Builds/Windows", "Windows Full Quality", report);
            const string readmeSource = "Packaging/WINDOWS_README.txt";
            if (File.Exists(readmeSource))
                File.Copy(
                    readmeSource,
                    "Builds/Windows/README.txt",
                    true);
        }

        [MenuItem("Canopy Kin/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureShared();
            ConfigureWebGL();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.WebGL, "com.moonroot.canopykin");
            if (Directory.Exists("Builds/WebGL"))
                Directory.Delete("Builds/WebGL", true);
            Directory.CreateDirectory("Builds/WebGL");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            BuildReport report = BuildPipeline.BuildPlayer(
                EnabledScenes(),
                "Builds/WebGL",
                BuildTarget.WebGL,
                BuildOptions.None);
            RequireSuccess(report, "WebGL");
            File.WriteAllText("Builds/WebGL/.nojekyll", string.Empty);
            WriteManifest("Builds/WebGL", "WebGL Optimized", report);
        }

        static void ConfigureShared()
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Gameplay scene is missing", ScenePath);
            ValidateProductionAssets();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Canopy Kin: Moonroot";
            PlayerSettings.companyName = "Moonroot Studio";
            PlayerSettings.bundleVersion = ProductVersion;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        static void ConfigureWindows()
        {
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Minimal);
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.useFlipModelSwapchain = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
            QualitySettings.SetQualityLevel(5, true);
            QualitySettings.vSyncCount = 1;
        }

        static void ConfigureWebGL()
        {
            bool diagnostics = string.Equals(
                Environment.GetEnvironmentVariable("MOONROOT_WEBGL_DIAGNOSTICS"),
                "1",
                StringComparison.Ordinal);
            PlayerSettings.stripEngineCode = true;
            // High managed stripping produced a release-only WebGL regression:
            // Unity removed runtime-reached Input System/UI code and the player
            // emitted a NullReferenceException every frame. Low still strips
            // unused assemblies while preserving the reflection-driven paths
            // used by the actual game.
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL,
                ManagedStrippingLevel.Low);
            // GitHub Pages cannot supply repository-defined Content-Encoding
            // headers. Gzip plus Unity's JavaScript fallback keeps the payload
            // below Git's single-file limit and still loads when the server
            // returns the compressed bytes as ordinary static content.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = diagnostics
                ? WebGLDebugSymbolMode.Embedded
                : WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = diagnostics
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.emscriptenArgs = string.Empty;
            PlayerSettings.WebGL.template = "PROJECT:CanopyKin";
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            QualitySettings.SetQualityLevel(3, true);
            QualitySettings.vSyncCount = 0;
        }

        static string[] EnabledScenes()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Unexpected build scene: {scene.path}");
            return new[] { ScenePath };
        }

        static void RequireSuccess(BuildReport report, string platform)
        {
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"{platform} build failed: {report.summary.result}");
            Debug.Log($"CANOPY_KIN_{platform.ToUpperInvariant()}_BUILD_OK size={report.summary.totalSize} time={report.summary.totalTime}");
        }

        static void WriteManifest(string directory, string edition, BuildReport report)
        {
            string json =
                "{\n" +
                $"  \"product\": \"Canopy Kin: Moonroot\",\n" +
                $"  \"version\": \"{ProductVersion}\",\n" +
                $"  \"edition\": \"{edition}\",\n" +
                $"  \"unity\": \"{Application.unityVersion}\",\n" +
                $"  \"buildBytes\": {report.summary.totalSize},\n" +
                $"  \"builtUtc\": \"{DateTime.UtcNow:O}\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(directory, "build-manifest.json"), json);
        }

        public static void Validate()
        {
            ConfigureShared();
            Debug.Log("CANOPY_KIN_VALIDATION_OK");
        }

        static void ValidateProductionAssets()
        {
            // Importer policy changes must reach the Library cache before the
            // player is built; otherwise Unity may keep an older non-readable
            // mesh even though the source importer requests Read/Write.
            AssetDatabase.ImportAsset(
                DeadTreePath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(
                RootNetworkPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(
                ProductionSpiderPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(
                ProductionBeetlePath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            GameObject ant = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionAntPath);
            if (!ant) throw new FileNotFoundException("Production ant FBX is missing", ProductionAntPath);
            SkinnedMeshRenderer skin = ant.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!skin || !skin.sharedMesh)
                throw new InvalidOperationException("Production ant must contain one imported skinned mesh.");
            if (skin.sharedMesh.vertexCount < 20000)
                throw new InvalidOperationException($"Production ant mesh is unexpectedly low detail: {skin.sharedMesh.vertexCount} vertices.");

            string[] requiredBones =
            {
                "Root", "Thorax", "Abdomen", "Head", "Mandible_L", "Mandible_R",
                "Antenna_L_1", "Antenna_R_1",
                "Leg_L_Front_Coxa", "Leg_R_Front_Coxa",
                "Leg_L_Middle_Coxa", "Leg_R_Middle_Coxa",
                "Leg_L_Rear_Coxa", "Leg_R_Rear_Coxa"
            };
            var importedBones = skin.bones.Where(bone => bone).Select(bone => bone.name).ToHashSet();
            string missingBone = requiredBones.FirstOrDefault(required => !importedBones.Contains(required));
            if (missingBone != null)
                throw new InvalidOperationException($"Production ant rig is missing bone: {missingBone}");

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ProductionAntPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length < 9)
                throw new InvalidOperationException($"Production ant requires nine animation clips; imported {clips.Length}.");

            GameObject spider = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionSpiderPath);
            if (!spider)
                throw new FileNotFoundException("Production fishing-spider FBX is missing", ProductionSpiderPath);
            SkinnedMeshRenderer[] spiderSkins =
                spider.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int spiderTriangles = spiderSkins
                .Where(renderer => renderer.sharedMesh)
                .Sum(renderer => renderer.sharedMesh.triangles.Length / 3);
            if (spiderSkins.Length < 2 || spiderTriangles < 135000)
                throw new InvalidOperationException(
                    $"Production spider requires two useful skinned LODs; " +
                    $"renderers={spiderSkins.Length} triangles={spiderTriangles}.");
            SkinnedMeshRenderer spiderHigh = spiderSkins
                .Where(renderer => renderer.sharedMesh)
                .OrderByDescending(renderer => renderer.sharedMesh.triangles.Length)
                .FirstOrDefault();
            string[] requiredSpiderBones =
            {
                "Root", "Thorax", "Abdomen", "Head",
                "Leg_L_Front_Coxa", "Leg_R_Front_Coxa",
                "Leg_L_Rear_Coxa", "Leg_R_Rear_Coxa"
            };
            var spiderBones = spiderHigh.bones
                .Where(bone => bone)
                .Select(bone => bone.name)
                .ToHashSet();
            string missingSpiderBone =
                requiredSpiderBones.FirstOrDefault(required => !spiderBones.Contains(required));
            if (missingSpiderBone != null)
                throw new InvalidOperationException(
                    $"Production spider rig is missing bone: {missingSpiderBone}");
            AnimationClip[] spiderClips = AssetDatabase.LoadAllAssetsAtPath(ProductionSpiderPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (spiderClips.Length < 8)
                throw new InvalidOperationException(
                    $"Production spider requires eight animation clips; imported {spiderClips.Length}.");

            var spiderTextureImporter = AssetImporter.GetAtPath(SpiderAlbedoPath) as TextureImporter;
            if (spiderTextureImporter == null)
                throw new FileNotFoundException(
                    "Production spider 8K scan texture is missing", SpiderAlbedoPath);
            TextureImporterPlatformSettings spiderStandalone =
                spiderTextureImporter.GetPlatformTextureSettings("Standalone");
            TextureImporterPlatformSettings spiderWeb =
                spiderTextureImporter.GetPlatformTextureSettings("WebGL");
            if (!spiderStandalone.overridden || spiderStandalone.maxTextureSize < 8192)
                throw new InvalidOperationException(
                    "Windows spider import must retain the 8K scan texture.");
            if (!spiderWeb.overridden || spiderWeb.maxTextureSize > 2048)
                throw new InvalidOperationException(
                    "WebGL spider import must use the independent 2K override.");

            GameObject beetle = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionBeetlePath);
            if (!beetle)
                throw new FileNotFoundException(
                    "Production rhinoceros-beetle FBX is missing", ProductionBeetlePath);
            SkinnedMeshRenderer[] beetleSkins =
                beetle.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int beetleTriangles = beetleSkins
                .Where(renderer => renderer.sharedMesh)
                .Sum(renderer => renderer.sharedMesh.triangles.Length / 3);
            if (beetleSkins.Length < 2 || beetleTriangles < 110000)
                throw new InvalidOperationException(
                    $"Production beetle requires two useful skinned LODs; " +
                    $"renderers={beetleSkins.Length} triangles={beetleTriangles}.");
            SkinnedMeshRenderer beetleHigh = beetleSkins
                .Where(renderer => renderer.sharedMesh)
                .OrderByDescending(renderer => renderer.sharedMesh.triangles.Length)
                .FirstOrDefault();
            string[] requiredBeetleBones =
            {
                "Root", "Thorax", "Abdomen", "Head", "Horn",
                "Leg_L_Front_Coxa", "Leg_R_Front_Coxa",
                "Leg_L_Rear_Coxa", "Leg_R_Rear_Coxa"
            };
            var beetleBones = beetleHigh.bones
                .Where(bone => bone)
                .Select(bone => bone.name)
                .ToHashSet();
            string missingBeetleBone =
                requiredBeetleBones.FirstOrDefault(required => !beetleBones.Contains(required));
            if (missingBeetleBone != null)
                throw new InvalidOperationException(
                    $"Production beetle rig is missing bone: {missingBeetleBone}");
            AnimationClip[] beetleClips = AssetDatabase.LoadAllAssetsAtPath(ProductionBeetlePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (beetleClips.Length < 8)
                throw new InvalidOperationException(
                    $"Production beetle requires eight animation clips; imported {beetleClips.Length}.");

            var beetleTextureImporter = AssetImporter.GetAtPath(BeetleAlbedoPath) as TextureImporter;
            if (beetleTextureImporter == null)
                throw new FileNotFoundException(
                    "Production beetle scan texture is missing", BeetleAlbedoPath);
            TextureImporterPlatformSettings beetleStandalone =
                beetleTextureImporter.GetPlatformTextureSettings("Standalone");
            TextureImporterPlatformSettings beetleWeb =
                beetleTextureImporter.GetPlatformTextureSettings("WebGL");
            if (!beetleStandalone.overridden || beetleStandalone.maxTextureSize < 8192)
                throw new InvalidOperationException(
                    "Windows beetle import must retain the 8K scan texture.");
            if (!beetleWeb.overridden || beetleWeb.maxTextureSize > 2048)
                throw new InvalidOperationException(
                    "WebGL beetle import must use the independent 2K override.");

            var floorImporter = AssetImporter.GetAtPath(ForestFloorPath) as TextureImporter;
            if (floorImporter == null)
                throw new FileNotFoundException("Production forest-floor texture is missing", ForestFloorPath);
            TextureImporterPlatformSettings standalone = floorImporter.GetPlatformTextureSettings("Standalone");
            TextureImporterPlatformSettings web = floorImporter.GetPlatformTextureSettings("WebGL");
            if (!standalone.overridden || standalone.maxTextureSize < 8192)
                throw new InvalidOperationException("Windows forest-floor import must retain 8K source resolution.");
            if (!web.overridden || web.maxTextureSize > 2048)
                throw new InvalidOperationException("WebGL forest-floor import must use its independent optimized override.");

            GameObject deadTree = AssetDatabase.LoadAssetAtPath<GameObject>(DeadTreePath);
            if (!deadTree) throw new FileNotFoundException("Production dead-tree landmark is missing", DeadTreePath);
            MeshFilter[] deadTreeMeshes = deadTree.GetComponentsInChildren<MeshFilter>(true);
            int deadTreeTriangles = deadTreeMeshes
                .Where(filter => filter.sharedMesh)
                .Sum(filter => filter.sharedMesh.triangles.Length / 3);
            Mesh largestDeadTreeMesh = deadTreeMeshes
                .Where(filter => filter.sharedMesh)
                .Select(filter => filter.sharedMesh)
                .OrderByDescending(mesh => mesh.triangles.Length)
                .FirstOrDefault();
            if (deadTreeTriangles < 90000)
                throw new InvalidOperationException(
                    $"Dead-tree landmark import is unexpectedly low detail: {deadTreeTriangles} triangles.");
            if (!largestDeadTreeMesh)
                throw new InvalidOperationException("Dead-tree landmark contains no usable mesh.");

            GameObject rootNetwork = AssetDatabase.LoadAssetAtPath<GameObject>(RootNetworkPath);
            if (!rootNetwork)
                throw new FileNotFoundException("Production root-network landmark is missing", RootNetworkPath);
            MeshFilter[] rootMeshes = rootNetwork.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter rootHigh = rootMeshes.FirstOrDefault(filter =>
                filter.sharedMesh && filter.name.Contains("LOD0"));
            MeshFilter rootLow = rootMeshes.FirstOrDefault(filter =>
                filter.sharedMesh && filter.name.Contains("LOD1"));
            if (!rootHigh || !rootLow)
                throw new InvalidOperationException(
                    "Production root network requires named close and distant LOD meshes.");
            int rootHighTriangles = rootHigh.sharedMesh.triangles.Length / 3;
            int rootLowTriangles = rootLow.sharedMesh.triangles.Length / 3;
            if (rootHighTriangles < 15000 || rootLowTriangles < 2000)
                throw new InvalidOperationException(
                    $"Production root network lost authored detail: high={rootHighTriangles}, low={rootLowTriangles}.");

            Debug.Log(
                $"CANOPY_KIN_PRODUCTION_ASSETS_OK antVertices={skin.sharedMesh.vertexCount} " +
                $"antTriangles={skin.sharedMesh.triangles.Length / 3} clips={clips.Length} " +
                $"windowsTexture={standalone.maxTextureSize} webTexture={web.maxTextureSize} " +
                $"spiderLods={spiderSkins.Length} spiderTriangles={spiderTriangles} " +
                $"spiderClips={spiderClips.Length} spiderWindowsTexture={spiderStandalone.maxTextureSize} " +
                $"spiderWebTexture={spiderWeb.maxTextureSize} " +
                $"beetleLods={beetleSkins.Length} beetleTriangles={beetleTriangles} " +
                $"beetleClips={beetleClips.Length} beetleWindowsTexture={beetleStandalone.maxTextureSize} " +
                $"beetleWebTexture={beetleWeb.maxTextureSize} " +
                $"deadTreeMeshes={deadTreeMeshes.Length} deadTreeTriangles={deadTreeTriangles} " +
                $"deadTreeBounds={largestDeadTreeMesh.bounds.size} " +
                $"rootLods={rootMeshes.Length} rootTriangles={rootHighTriangles + rootLowTriangles}");
        }
    }
}
