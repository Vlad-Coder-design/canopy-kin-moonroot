using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CanopyKin.Editor
{
    public static class BuildGame
    {
        const string ScenePath = "Assets/Scenes/Moonroot.unity";

        [MenuItem("Canopy Kin/Build Windows")]
        public static void BuildWindows()
        {
            ConfigureShared();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.moonroot.canopykin");
            Directory.CreateDirectory("Builds/Windows");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            BuildReport report = BuildPipeline.BuildPlayer(
                EnabledScenes(),
                "Builds/Windows/CanopyKin.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            RequireSuccess(report, "Windows");
        }

        [MenuItem("Canopy Kin/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureShared();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.WebGL, "com.moonroot.canopykin");
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.emscriptenArgs = string.Empty;
            PlayerSettings.WebGL.template = "PROJECT:CanopyKin";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            Directory.CreateDirectory("Builds/WebGL");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            BuildReport report = BuildPipeline.BuildPlayer(
                EnabledScenes(),
                "Builds/WebGL",
                BuildTarget.WebGL,
                BuildOptions.None);
            RequireSuccess(report, "WebGL");
            File.WriteAllText("Builds/WebGL/.nojekyll", string.Empty);
        }

        static void ConfigureShared()
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Gameplay scene is missing", ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Canopy Kin: Moonroot";
            PlayerSettings.companyName = "Moonroot Studio";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.stripEngineCode = true;
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

        public static void Validate()
        {
            ConfigureShared();
            Debug.Log("CANOPY_KIN_VALIDATION_OK");
        }
    }
}
