using System;
using System.IO;
using System.Linq;
using BetoBeto.Player;
using BetoBeto.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BetoBeto.Editor
{
    public sealed class GhostFbxPostprocessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(GhostModelImporter.Source + "/") || !assetPath.EndsWith(".fbx")) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = true;
            importer.importAnimation = assetPath.Contains("/Animations/");
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importCameras = false;
            importer.importLights = false;
            importer.resampleCurves = true;
            importer.optimizeGameObjects = false;
        }

        Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!assetPath.StartsWith(GhostModelImporter.Source + "/")) return null;
            // Match source material names so slot reordering survives a Blender re-export.
            string path = material.name switch
            {
                "CuteGhost_PBR" => GhostModelImporter.Source + "/Materials/GhostBody.mat",
                "Apron" => GhostModelImporter.Source + "/Materials/Apron.mat",
                _ => null
            };
            return path == null ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(GhostModelImporter.Source + "/Animations/")) return;
            var importer = (ModelImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = name;
                clip.loopTime = name != "YODAREStart";
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            // Always derive ranges from the newly exported take, including length edits.
            importer.clipAnimations = clips;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(GhostModelImporter.Source + "/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.sRGBTexture = assetPath.Contains("BaseColor");
            if (assetPath.Contains("Normal")) importer.textureType = TextureImporterType.NormalMap;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }
    }

    public static class GhostModelImporter
    {
        public const string Source = "Assets/BetoBeto/Art/Characters/CuteGhost";
        public const string PrefabPath = "Assets/BetoBeto/Prefabs/Characters/ApronGhost.prefab";
        public const string ControllerPath = Source + "/Controllers/Ghost.controller";

        [MenuItem("BetoBeto/Apply Ghost Model")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            var apronMaterial = CreateMaterial();
            var bodyMaterial = CreateBodyMaterial(apronMaterial);
            foreach (string file in Directory.GetFiles(Source, "*.fbx", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            var clips = new[] { "Idle", "Move", "YODAREStart", "Yodare", "Spook" }.ToDictionary(n => n, Clip);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            // Keep the controller GUID stable when rerunning setup.
            foreach (var layer in controller.layers)
            {
                var machine = layer.stateMachine;
                foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
                foreach (var state in machine.states) machine.RemoveState(state.state);
            }
            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>())
                UnityEngine.Object.DestroyImmediate(tree, true);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Drool", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Drooling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Spook", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Spooking", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var locomotion = sm.AddState("Locomotion", new Vector3(250, 60));
            var blend = new BlendTree { name = "Idle - Move", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(clips["Idle"], 0);
            blend.AddChild(clips["Move"], 1);
            locomotion.motion = blend;
            sm.defaultState = locomotion;
            var start = sm.AddState("YODAREStart", new Vector3(250, 180));
            start.motion = clips["YODAREStart"];
            var loop = sm.AddState("Yodare", new Vector3(500, 180));
            loop.motion = clips["Yodare"];
            var spook = sm.AddState("Spook", new Vector3(500, 60));
            spook.motion = clips["Spook"];
            var scare = sm.AddAnyStateTransition(spook);
            Configure(scare, .04f);
            scare.canTransitionToSelf = true;
            scare.AddCondition(AnimatorConditionMode.If, 0, "Spook");
            scare.AddCondition(AnimatorConditionMode.IfNot, 0, "Drooling");
            var drool = sm.AddAnyStateTransition(start);
            Configure(drool, .06f);
            drool.canTransitionToSelf = false;
            drool.AddCondition(AnimatorConditionMode.If, 0, "Drool");
            drool.AddCondition(AnimatorConditionMode.IfNot, 0, "Spooking");
            var toLoop = start.AddTransition(loop);
            Configure(toLoop, .025f);
            toLoop.hasExitTime = true;
            toLoop.exitTime = 1;
            toLoop.AddCondition(AnimatorConditionMode.If, 0, "Drooling");
            var cancelStart = start.AddTransition(locomotion);
            Configure(cancelStart, .08f);
            cancelStart.AddCondition(AnimatorConditionMode.IfNot, 0, "Drooling");
            var toMove = loop.AddTransition(locomotion);
            Configure(toMove, .1f);
            toMove.AddCondition(AnimatorConditionMode.IfNot, 0, "Drooling");
            var finishScare = spook.AddTransition(locomotion);
            Configure(finishScare, .1f);
            finishScare.AddCondition(AnimatorConditionMode.IfNot, 0, "Spooking");
            EditorUtility.SetDirty(controller);

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "/Models/CuteGhost_Rig.fbx");
            if (source == null) throw new InvalidOperationException("Export CuteGhost_Rig.fbx first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<GhostController>() == null) throw new InvalidOperationException("GhostController missing");
                var visual = root.transform.Find("Visual");
                if (visual == null) throw new InvalidOperationException("Ghost Visual missing");
                while (visual.childCount > 0) UnityEngine.Object.DestroyImmediate(visual.GetChild(0).gameObject);
                var bob = visual.GetComponent<ActorVisual>();
                if (bob != null) UnityEngine.Object.DestroyImmediate(bob);
                visual.localPosition = new Vector3(0, .15f, 0);
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * 1.5f;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, visual);
                model.name = "Ghost Model";
                // Blender -Y becomes Unity +Z; ActorFacing uses local -Z.
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer.sharedMaterials.Length != renderer.sharedMesh.subMeshCount ||
                        renderer.sharedMaterials.Any(m => m != apronMaterial && m != bodyMaterial))
                        throw new InvalidOperationException("Unknown ghost material slot; expected CuteGhost_PBR or Apron.");
                    renderer.updateWhenOffscreen = true;
                }
                if (visual.GetComponent<GhostModelVisual>() == null) visual.gameObject.AddComponent<GhostModelVisual>();
                var droolVfx = visual.GetComponent<GhostDroolVfx>() ?? visual.gameObject.AddComponent<GhostDroolVfx>();
                droolVfx.origin = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Yodare");
                droolVfx.material = GhostDroolVfxSetup.EnsureMaterial();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Ghost model applied: Idle/Move, YODAREStart -> Yodare, Spook.");
        }

        static void Configure(AnimatorStateTransition transition, float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        }

        public static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(Source + "/Animations/" + name + ".fbx")
            .OfType<AnimationClip>().Single(c => c.name == name);

        static Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit missing");
            string path = Source + "/Materials/Apron.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/CuteGhost_BaseColor.jpg"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/CuteGhost_Normal.png"));
            material.SetFloat("_BumpScale", .65f);
            material.EnableKeyword("_NORMALMAP");
            var rough = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var metal = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                ImageConversion.LoadImage(rough, File.ReadAllBytes(Source + "/Textures/CuteGhost_Roughness.png"));
                ImageConversion.LoadImage(metal, File.ReadAllBytes(Source + "/Textures/CuteGhost_Metallic.png"));
                var pixels = metal.GetPixels32();
                var roughness = rough.GetPixels32();
                if (pixels.Length != roughness.Length) throw new InvalidOperationException("PBR texture dimensions differ");
                for (int i = 0; i < pixels.Length; i++) pixels[i].a = (byte)(255 - roughness[i].r);
                metal.SetPixels32(pixels); metal.Apply();
                string map = Source + "/Textures/CuteGhost_MetallicSmoothness.png";
                File.WriteAllBytes(map, metal.EncodeToPNG());
                AssetDatabase.ImportAsset(map, ImportAssetOptions.ForceSynchronousImport);
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(map));
            }
            finally { UnityEngine.Object.DestroyImmediate(rough); UnityEngine.Object.DestroyImmediate(metal); }
            material.SetFloat("_Metallic", 1);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material CreateBodyMaterial(Material apron)
        {
            var shader = Shader.Find("BetoBeto/Ghost Body Rim");
            if (shader == null) throw new InvalidOperationException("Ghost Body Rim shader missing");
            string path = Source + "/Materials/GhostBody.mat";
            var body = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (body == null)
            {
                body = new Material(shader);
                body.SetColor("_RimColor", new Color(.035f, .18f, 1f, 1));
                body.SetFloat("_RimStrength", 2.5f);
                body.SetFloat("_RimPower", 2.5f);
                AssetDatabase.CreateAsset(body, path);
            }
            body.shader = shader;
            body.SetTexture("_BaseMap", apron.GetTexture("_BaseMap"));
            body.SetColor("_BaseColor", Color.white);
            body.SetTexture("_BumpMap", apron.GetTexture("_BumpMap"));
            body.SetFloat("_BumpScale", .65f);
            body.SetTexture("_MetallicGlossMap", apron.GetTexture("_MetallicGlossMap"));
            EditorUtility.SetDirty(body);
            return body;
        }
    }
}
