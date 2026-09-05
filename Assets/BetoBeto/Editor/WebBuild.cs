using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BetoBeto.Editor
{
    public static class WebBuild
    {
        [MenuItem("BetoBeto/Build/Configure WebGL")]
        public static void Configure()
        {
            PlayerSettings.WebGL.template = "PROJECT:BetoBeto";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.defaultWebScreenWidth = 1600;
            PlayerSettings.defaultWebScreenHeight = 900;
            var font = AssetImporter.GetAtPath("Assets/BetoBeto/Resources/Fonts/MPLUSRounded1c-Regular.ttf") as TrueTypeFontImporter;
            if (font != null && !font.includeFontData) { font.includeFontData = true; font.SaveAndReimport(); }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("BetoBeto/Build/WebGL")]
        public static void Build()
        {
            Configure();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || !scenes[0].EndsWith("/Title.unity")) throw new BuildFailedException("Title must be the first enabled build scene.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Could not switch to WebGL. Install the Unity Web build module.");
            Directory.CreateDirectory("Builds/WebGL");
            var report = BuildPipeline.BuildPlayer(scenes, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("WebGL build failed; see the Console.");
            Debug.Log("WebGL ready: Builds/WebGL. Serve over HTTP; see Tools/serve_webgl.py.");
        }
    }
}
