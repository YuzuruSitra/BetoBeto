using UnityEditor;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace BetoBeto.Editor
{
    public sealed class StudioReflectionImporter : AssetPostprocessor
    {
        public const string MapPath = "Assets/BetoBeto/Art/Environment/KitchenReflection.exr";
        public const string CookiePath = "Assets/BetoBeto/Art/Environment/LeafSunlightCookie.png";

        void OnPreprocessTexture()
        {
            if (assetPath == CookiePath)
            {
                var cookie = (TextureImporter)assetImporter;
                cookie.textureType = TextureImporterType.Default;
                cookie.sRGBTexture = false;
                cookie.alphaSource = TextureImporterAlphaSource.None;
                cookie.wrapMode = TextureWrapMode.Repeat;
                cookie.mipmapEnabled = true;
                cookie.maxTextureSize = 512;
                cookie.textureCompression = TextureImporterCompression.Uncompressed;
                return;
            }
            if (assetPath != MapPath && assetPath != "Assets/BetoBeto/Art/Environment/StudioReflection.exr"
                && assetPath != "Assets/BetoBeto/Art/Environment/BladeReflection.exr") return;
            var texture = (TextureImporter)assetImporter;
            texture.textureShape = TextureImporterShape.TextureCube;
            texture.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            var settings = new TextureImporterSettings();
            texture.ReadTextureSettings(settings);
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            settings.seamlessCubemap = true;
            texture.SetTextureSettings(settings);
            texture.sRGBTexture = false;
            texture.mipmapEnabled = true;
            texture.maxTextureSize = 256;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.isReadable = false;
            if (assetPath.EndsWith("/BladeReflection.exr", System.StringComparison.Ordinal))
            {
                // This custom shader samples linear radiance directly, not Unity's probe RGBM encoding.
                settings.cubemapConvolution = TextureImporterCubemapConvolution.None;
                texture.SetTextureSettings(settings);
                var platform = texture.GetDefaultPlatformTextureSettings();
                platform.format = TextureImporterFormat.RGBAHalf;
                platform.maxTextureSize = 256;
                platform.textureCompression = TextureImporterCompression.Uncompressed;
                texture.SetPlatformTextureSettings(platform);
            }
        }

        // Scene settings keep the reflection in builds without a realtime reflection probe.
        public static bool ApplyToActiveScene()
        {
            if (SceneManager.GetActiveScene().GetRootGameObjects().Any(r => r.GetComponentInChildren<BetoBeto.Stage.StageLayout>() != null)) return false;
            var map = AssetDatabase.LoadAssetAtPath<Cubemap>(MapPath);
            if (map == null) return false;
            bool changed = RenderSettings.defaultReflectionMode != DefaultReflectionMode.Custom
                || RenderSettings.customReflectionTexture != map || RenderSettings.reflectionIntensity != .8f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = map;
            RenderSettings.reflectionIntensity = .8f;
            var ambient = new Color(.48f, .54f, .61f);
            changed |= RenderSettings.ambientLight != ambient;
            RenderSettings.ambientLight = ambient;
            var cookie = AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
            if (cookie != null)
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (var light in root.GetComponentsInChildren<Light>(true))
                    {
                        if (light.type != LightType.Directional) continue;
                        var tint = new Color(1, .91f, .76f);
                        var data = light.GetComponent<UniversalAdditionalLightData>();
                        if (data == null) { data = light.gameObject.AddComponent<UniversalAdditionalLightData>(); changed = true; }
                        var size = new Vector2(8, 8);
                        changed |= light.cookie != cookie || light.intensity != 2.4f || light.color != tint
                            || data.lightCookieSize != size;
                        light.cookie = cookie; light.intensity = 2.4f; light.color = tint;
                        data.lightCookieSize = size;
                    }
            return changed;
        }

        [MenuItem("BetoBeto/Apply Kitchen Environment Lighting")]
        public static void ApplyAllScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode before applying lighting.");
            var active = SceneManager.GetActiveScene();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BetoBeto/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool open = scene.IsValid() && scene.isLoaded;
                if (!open) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                bool dirty = scene.isDirty;
                SceneManager.SetActiveScene(scene);
                if (ApplyToActiveScene())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!dirty) EditorSceneManager.SaveScene(scene);
                }
                if (!open) EditorSceneManager.CloseScene(scene, true);
            }
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            Debug.Log("Kitchen HDR reflection and dappled sunlight applied.");
        }
    }
}
