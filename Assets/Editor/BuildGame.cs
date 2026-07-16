using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace CanopyKin.Editor
{
    public static class BuildGame
    {
        [MenuItem("Canopy Kin/Build Windows")]
        public static void BuildWindows(){Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,"Assets/Scenes/Moonroot.unity");PlayerSettings.productName="Canopy Kin: Moonroot";PlayerSettings.companyName="Moonroot Studio";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.moonroot.canopykin");EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64);var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Moonroot.unity"},"Builds/Windows/CanopyKin.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);}
        [MenuItem("Canopy Kin/Build WebGL")]
        public static void BuildWebGL()
        {
            const string scenePath="Assets/Scenes/Moonroot.unity";
            if(!File.Exists(scenePath)) throw new FileNotFoundException("Gameplay scene is missing",scenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)};
            PlayerSettings.productName="Canopy Kin: Moonroot";
            PlayerSettings.companyName="Moonroot Studio";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.WebGL,"com.moonroot.canopykin");
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching=false;
            PlayerSettings.WebGL.emscriptenArgs="-s ALLOW_MEMORY_GROWTH=1";
            PlayerSettings.WebGL.template="PROJECT:CanopyKin";
            Directory.CreateDirectory("Builds/WebGL");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,BuildTarget.WebGL);
            BuildReport report=BuildPipeline.BuildPlayer(new[]{scenePath},"Builds/WebGL",BuildTarget.WebGL,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("WebGL build failed: "+report.summary.result);
            File.WriteAllText("Builds/WebGL/.nojekyll",string.Empty);
        }
        public static void Validate(){Debug.Log("CANOPY_KIN_VALIDATION_OK");}
    }
}
