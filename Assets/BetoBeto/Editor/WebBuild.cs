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
            ConfigureCharacterTextures();
            var font = AssetImporter.GetAtPath("Assets/BetoBeto/Resources/Fonts/MPLUSRounded1c-Regular.ttf") as TrueTypeFontImporter;
            if (font != null && !font.includeFontData) { font.includeFontData = true; font.SaveAndReimport(); }
            AssetDatabase.SaveAssets();
        }

        // Large imported PBR maps otherwise expand to hundreds of MB on WebGL startup.
        // Keep the original files and desktop import settings; the board displays small characters.
        static void ConfigureCharacterTextures()
        {
            foreach (string root in new[] { "Assets/BetoBeto/Art/Characters/CuteGhost", "Assets/BetoBeto/Art/Characters/Fruits" })
            {
                if (!AssetDatabase.IsValidFolder(root)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                    if (importer == null) continue;
                    var web = importer.GetPlatformTextureSettings("WebGL");
                    if (web.overridden && web.maxTextureSize <= 1024) continue;
                    web.name = "WebGL";
                    web.overridden = true;
                    web.maxTextureSize = Mathf.Min(importer.maxTextureSize, 1024);
                    importer.SetPlatformTextureSettings(web);
                    importer.SaveAndReimport();
                }
            }
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
