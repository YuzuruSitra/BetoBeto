using System;
using System.Linq;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BetoBeto.Editor
{
    public static class FruitModelImporter
    {
        public const string Source = "Assets/BetoBeto/Art/Characters/Fruits";

        [MenuItem("BetoBeto/Apply Fruit Models")]
        public static void Apply()
        {
            ConvertMaterials();
            ApplyScaredRunAnimations();
            // Strawberry, Blueberry, Orange, Melon: retain a clear size progression.
            float[] scales = { .94f, .64f, 1.04f, 1.14f };
            for (int i = 0; i < 4; i++)
            {
                var kind = (FruitKind)i;
                string path = PrototypeArt.Root + "/Prefabs/Characters/" + kind + ".prefab";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "/Prefabs/" + kind + ".prefab");
                if (source == null) throw new InvalidOperationException("Missing fruit model: " + kind);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var visual = root.transform.Find("Visual");
                    if (visual == null) throw new InvalidOperationException("Missing Visual: " + path);
                    // Keep the gameplay root and prefab GUID, and set the per-kind visual scale.
                    visual.localScale = Vector3.one * scales[i];
                    while (visual.childCount > 0) UnityEngine.Object.DestroyImmediate(visual.GetChild(0).gameObject);
                    var bob = visual.GetComponent<ActorVisual>();
                    if (bob != null) UnityEngine.Object.DestroyImmediate(bob);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, visual);
                    model.name = "Fruit Model";
                    model.transform.localPosition = Vector3.zero;
                    // Imported models face +Z; ActorFacing expects -Z.
                    model.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    model.transform.localScale = Vector3.one * .44f;
                    if (visual.GetComponent<FruitModelVisual>() == null) visual.gameObject.AddComponent<FruitModelVisual>();
                    if (root.GetComponent<FruitAgent>().kind != kind) throw new InvalidOperationException("Fruit kind mismatch");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("BetoBeto/Update Fruit Scared Run Animations")]
        public static void ApplyScaredRunAnimations()
        {
            for (int i = 0; i < 4; i++)
            {
                string kind = ((FruitKind)i).ToString();
                string path = Source + "/Animations/" + kind + "_ScaredRun.fbx";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .SingleOrDefault(c => c.name == "ScaredRun");
                if (clip == null) throw new InvalidOperationException("Missing ScaredRun clip: " + kind);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Source + "/Controllers/" + kind + ".controller");
                if (controller == null) throw new InvalidOperationException("Missing fruit controller: " + kind);
                var machine = controller.layers[0].stateMachine;
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "ScaredRun")
                    ?? machine.AddState("ScaredRun", new Vector3(270, 220, 0));
                state.motion = clip;
                state.writeDefaultValues = true;
                var transition = machine.anyStateTransitions.FirstOrDefault(t => t.destinationState == state)
                    ?? machine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = .1f;
                transition.canTransitionToSelf = false;
                transition.conditions = new[] { new AnimatorCondition { mode = AnimatorConditionMode.Equals, parameter = "Motion", threshold = 3 } };
                EditorUtility.SetDirty(controller);
            }
            AssetDatabase.SaveAssets();
        }

        static void ConvertMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Source }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader == shader) continue;
                var baseMap = material.GetTexture("_MainTex");
                var normal = material.GetTexture("_BumpMap");
                var metallic = material.GetTexture("_MetallicGlossMap");
                var color = material.GetColor("_Color");
                material.shader = shader;
                material.SetTexture("_BaseMap", baseMap);
                material.SetColor("_BaseColor", color);
                material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", metallic);
                material.SetFloat("_Metallic", 1);
                material.SetFloat("_Smoothness", 1);
                material.SetFloat("_SmoothnessTextureChannel", 0);
                material.DisableKeyword("_METALLICGLOSSMAP");
                if (normal != null) material.EnableKeyword("_NORMALMAP");
                if (metallic != null) material.EnableKeyword("_METALLICSPECGLOSSMAP");
                EditorUtility.SetDirty(material);
            }
        }
    }
}
