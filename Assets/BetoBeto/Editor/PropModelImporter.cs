using System;
using System.IO;
using System.Linq;
using BetoBeto.Core;
using BetoBeto.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BetoBeto.Editor
{
    public sealed class PropImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(PropModelImporter.Root + "/Models/", StringComparison.Ordinal)) return;
            var model = (ModelImporter)assetImporter;
            model.importAnimation = false;
            model.importBlendShapes = true;
            model.importCameras = false; model.importLights = false;
            model.importNormals = ModelImporterNormals.Import;
            model.importTangents = ModelImporterTangents.CalculateMikk;
            model.importBlendShapeNormals = ModelImporterNormals.Calculate;
            model.meshCompression = ModelImporterMeshCompression.Off;
            model.isReadable = false; model.addCollider = false;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PropModelImporter.Root + "/Textures/", StringComparison.Ordinal)) return;
            var texture = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_Normal.png", StringComparison.Ordinal);
            bool data = normal || assetPath.EndsWith("_Mask.png", StringComparison.Ordinal)
                || assetPath.EndsWith("_Roughness.png", StringComparison.Ordinal) || assetPath.EndsWith("_Metallic.png", StringComparison.Ordinal);
            texture.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            texture.sRGBTexture = !data;
            texture.maxTextureSize = 1024; texture.mipmapEnabled = true;
            texture.textureCompression = TextureImporterCompression.Compressed;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.alphaSource = assetPath.EndsWith("_Mask.png", StringComparison.Ordinal) ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        }
    }

    public static class PropModelImporter
    {
        public const string Root = "Assets/BetoBeto/Art/Props";
        [Serializable] public sealed class Manifest { public string name; public MaterialSpec[] materials; }
        [Serializable] public sealed class MaterialSpec
        {
            public string name, BaseColor, Normal, Roughness, Metallic;
            public float[] color;
            public float roughness, metallic, alpha;
        }

        [MenuItem("BetoBeto/Apply Delivered Prop Models")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before importing props.");
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Root + "/Documentation", "*.json"))
            {
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(file));
                if (manifest == null || manifest.materials == null) continue;
                string modelPath = Root + "/Models/" + manifest.name + ".fbx";
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing prop model: " + modelPath);
                foreach (var spec in manifest.materials)
                {
                    var material = Material(spec);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), spec.name), material);
                }
                importer.SaveAndReimport();
                var root = new GameObject(manifest.name);
                AddModel(manifest.name, root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + manifest.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            Replace("Stage/BlueTile", "BlueTile");
            Replace("Stage/CookieWall", "BiscuitWall");
            Replace("Stage/BreakableCookie", "BreakableCookie", damage: true);
            Replace("Stage/Scone", "Scone", damage: true);
            Replace("Stage/Shredder", "Shredder", mechanism: true);
            Replace("Stage/MovingShredder", "MovingShredder", mechanism: true);
            Replace("Stage/FruitPipe", "FruitPipe", mechanism: true);
            Replace("Stage/Jelly", "Jelly");
            Replace("Stage/ChocolateFondue", "ChocolateFondue");
            Replace("Abilities/DroolPuddle", "DroolPuddle");
            ReplaceIce();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(PrototypeArt.AssetPath);
            assets.countertop = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Countertop.prefab");
            assets.fruitConfetti = CreateConfetti();
            assets.jellyMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Jelly_GrapeGelatin.mat");
            assets.droolMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DroolPuddle_Liquid.mat");
            EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
            UpdateCountertops(assets);
            Debug.Log("Delivered props applied. Original scene layouts and character prefabs preserved.");
        }

        static Material Material(MaterialSpec spec)
        {
            string path = Root + "/Materials/" + spec.name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            bool polished = spec.name.EndsWith("_BladeSteel", StringComparison.Ordinal);
            mat.shader = Shader.Find(spec.alpha < .999f ? "BetoBeto/Prop Transparent Fresnel" : polished ? "BetoBeto/Polished Prop Metal" : "Universal Render Pipeline/Lit");
            var tint = new Color(spec.color[0], spec.color[1], spec.color[2], spec.alpha);
            // Blender's numeric surface colours are linear; Unity's Color property is authored in sRGB.
            if (string.IsNullOrEmpty(spec.BaseColor)) { tint = tint.gamma; tint.a = spec.alpha; }
            if (spec.name == "Shredder_Surface" || spec.name == "MovingShredder_Surface") tint = new Color(1.13f, 1.10f, 1.08f, 1);
            mat.SetColor("_BaseColor", tint);
            if (polished)
            {
                mat.SetFloat("_ReflectionStrength", 2.2f);
                mat.SetTexture("_MetalReflection", AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/BetoBeto/Art/Environment/BladeReflection.exr"));
                mat.SetFloat("_MetalReflectionStrength", .28f);
            }
            mat.SetFloat("_Smoothness", 1 - spec.roughness); mat.SetFloat("_Metallic", polished ? 1 : spec.metallic);
            mat.SetTexture("_BaseMap", Texture(spec.BaseColor));
            var normal = Texture(spec.Normal); mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", 1);
            SetKeyword(mat, "_NORMALMAP", normal != null);
            string maskFile = spec.Roughness == null ? null : spec.Roughness.Replace("_Roughness.png", "_Mask.png");
            var mask = Texture(maskFile);
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", mask);
            SetKeyword(mat, "_METALLICSPECGLOSSMAP", mask != null);
            if (mask != null) { mat.SetFloat("_Metallic", 1); mat.SetFloat("_Smoothness", 1); }
            bool transparent = spec.alpha < .999f;
            if (transparent)
            {
                mat.SetColor("_FresnelColor", new Color(.55f + spec.color[0] * .45f, .55f + spec.color[1] * .45f, .55f + spec.color[2] * .45f, 1));
                mat.SetFloat("_FresnelStrength", 1.5f); mat.SetFloat("_FresnelPower", 2.2f); mat.SetFloat("_EdgeOpacity", .65f);
                bool drool = spec.name == "DroolPuddle_Liquid";
                mat.SetFloat("_FillStrength", spec.name == "Jelly_GrapeGelatin" ? .28f : 0);
                mat.SetFloat("_SparkleStrength", drool ? 2.4f : 0);
                if (drool) { mat.SetFloat("_FresnelStrength", 2.1f); mat.SetFloat("_EdgeOpacity", .42f); }
            }
            if (!transparent && !polished)
            {
                mat.SetFloat("_Surface", 0); mat.SetFloat("_Blend", 0); mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_ZWrite", 1);
                mat.SetFloat("_SrcBlend", (float)BlendMode.One); mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); mat.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            }
            mat.SetFloat("_Cull", (float)CullMode.Back);
            SetKeyword(mat, "_SURFACE_TYPE_TRANSPARENT", transparent);
            mat.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            mat.renderQueue = transparent ? 3000 : 2000;
            mat.SetShaderPassEnabled("ShadowCaster", !transparent);
            EditorUtility.SetDirty(mat); return mat;
        }
        static Texture2D Texture(string file) => string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + file);
        static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled) material.EnableKeyword(keyword); else material.DisableKeyword(keyword);
        }

        static GameObject AddModel(string name, Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing " + name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            // FBX/Unity handedness flips Blender X and Y. Restore authored grid directions.
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            model.transform.localScale = name == "Countertop" ? new Vector3(1, 12.5f, 1) : Vector3.one;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) skin.SetBlendShapeWeight(i, 0);
            return model;
        }
        static void ClearVisuals(GameObject root)
        {
            while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            foreach (var component in root.GetComponents<PropDamageVisual>()) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in root.GetComponents<PropMechanismVisual>()) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in root.GetComponents<JellyBoneWobble>()) UnityEngine.Object.DestroyImmediate(component);
        }
        static void Replace(string prefab, string modelName, bool damage = false, bool mechanism = false)
        {
            string path = PrototypeArt.Root + "/Prefabs/" + prefab + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ClearVisuals(root); var model = AddModel(modelName, root.transform);
                if (damage) root.AddComponent<PropDamageVisual>().fracture = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (modelName == "Jelly")
                {
                    var wobble = root.AddComponent<JellyBoneWobble>();
                    wobble.bones = new[] { Find(model.transform, "JellyLower"), Find(model.transform, "JellyMiddle"), Find(model.transform, "JellyTop") };
                    if (wobble.bones.Any(b => b == null)) throw new InvalidOperationException("Jelly deformation bones missing");
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    { var bounds = skin.localBounds; bounds.Expand(.3f); skin.localBounds = bounds; }
                }
                if (mechanism)
                {
                    var visual = root.AddComponent<PropMechanismVisual>();
                    visual.blade = Find(model.transform, "Blade"); visual.outletFlap = Find(model.transform, "OutletFlap");
                    visual.wheels = model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Wheel_", StringComparison.Ordinal)).ToArray();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        static void ReplaceIce()
        {
            string path = PrototypeArt.Root + "/Prefabs/Stage/IceWall.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ClearVisuals(root);
                var water = new GameObject("Water"); water.transform.SetParent(root.transform, false); AddModel("IceWater", water.transform);
                var wall = new GameObject("Wall"); wall.transform.SetParent(root.transform, false);
                var model = AddModel("IceWall", wall.transform);
                var cracks = new GameObject("Cracks"); cracks.transform.SetParent(wall.transform, false);
                foreach (var piece in model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Crack_", StringComparison.Ordinal)).ToArray())
                    piece.SetParent(cracks.transform, true);
                cracks.SetActive(false);
                wall.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static GameObject[] CreateConfetti()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/FruitConfetti.prefab");
            var renderers = source.GetComponentsInChildren<MeshRenderer>();
            var results = new GameObject[renderers.Length];
            Directory.CreateDirectory(Root + "/Prefabs/Confetti");
            for (int i = 0; i < renderers.Length; i++)
            {
                var root = new GameObject(renderers[i].name);
                var mesh = renderers[i].GetComponent<MeshFilter>().sharedMesh;
                var visual = new GameObject("Mesh"); visual.transform.SetParent(root.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                visual.AddComponent<MeshRenderer>().sharedMaterials = renderers[i].sharedMaterials;
                // Each reusable fragment is centred and has unit maximum dimension.
                float size = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
                visual.transform.localScale = Vector3.one / Mathf.Max(.001f, size);
                visual.transform.localPosition = -mesh.bounds.center / Mathf.Max(.001f, size);
                results[i] = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Confetti/" + root.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            return results;
        }
        static void UpdateCountertops(GameAssets assets)
        {
            var active = SceneManager.GetActiveScene();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BetoBeto/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool alreadyOpen = scene.IsValid() && scene.isLoaded;
                if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                bool wasDirty = scene.isDirty, changed = false;
                SceneManager.SetActiveScene(scene);
                changed |= StudioReflectionImporter.ApplyToActiveScene();
                foreach (var root in scene.GetRootGameObjects())
                {
                    var counter = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Kitchen countertop");
                    if (counter == null) continue;
                    var filter = counter.GetComponent<MeshFilter>(); var renderer = counter.GetComponent<MeshRenderer>();
                    if (filter == null && counter.childCount > 0) continue;
                    if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
                    if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(assets.countertop, counter);
                    model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one; changed = true;
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    // An already dirty user scene stays open and unsaved.
                    if (!wasDirty) EditorSceneManager.SaveScene(scene);
                }
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }
}

