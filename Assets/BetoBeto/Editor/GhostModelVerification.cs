using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetoBeto.Editor
{
    public static class GhostModelVerification
    {
        [MenuItem("BetoBeto/Verify and Preview Ghost Model")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before previewing.");
            string[] names = { "Idle", "Move", "YODAREStart", "Yodare", "Spook" };
            float[] times = { .5f, .5f, .66f, .065f, .06f };
            string[] references = new string[names.Length];
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Texture2D image = null;
            Camera camera = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                for (int i = 0; i < names.Length; i++)
                {
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GhostModelImporter.PrefabPath), scene);
                    root.transform.position = new Vector3((i - 2) * 1.3f, 0, 0);
                    var animator = root.GetComponentInChildren<Animator>();
                    animator.enabled = false;
                    var clip = GhostModelImporter.Clip(names[i]);
                    clip.SampleAnimation(animator.gameObject, times[i]);
                    var renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (renderer.sharedMesh.blendShapeCount == 0) throw new InvalidOperationException("Smile blend shape is missing");
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var shape = bindings.FirstOrDefault(b => b.propertyName.Contains("blendShape"));
                    if (string.IsNullOrEmpty(shape.propertyName)) throw new InvalidOperationException(names[i] + " has no baked smile curve");
                    var curve = AnimationUtility.GetEditorCurve(clip, shape);
                    references[i] = $"{names[i]}: {clip.length:F4}s, loop={clip.isLooping}, smile={curve.keys.Min(k => k.value):F1}..{curve.keys.Max(k => k.value):F1}, bones={renderer.bones.Length}";
                    if ((names[i] == "Yodare" || names[i] == "YODAREStart") && curve.keys.Max(k => k.value) < 99)
                        throw new InvalidOperationException("Smile did not bake: " + names[i]);
                }
                var cameraObject = new GameObject("Ghost QA camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 2.6f, -7);
                camera.transform.LookAt(new Vector3(0, 1.2f, 0));
                camera.orthographic = true;
                camera.orthographicSize = 1.35f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.19f, .24f, .29f);
                var lightObject = new GameObject("Ghost QA key");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1;
                light.transform.rotation = Quaternion.Euler(35, -25, 0);
                target = new RenderTexture(1600, 550, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(1600, 550, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 550), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs/QA");
                File.WriteAllBytes("Logs/QA/GhostModels-preview.png", image.EncodeToPNG());
                File.WriteAllLines("Logs/QA/GhostModels-import.txt", references);
                Debug.Log(string.Join("\n", references));
            }
            finally
            {
                RenderTexture.active = previous;
                if (camera != null) camera.targetTexture = null;
                if (target != null) UnityEngine.Object.DestroyImmediate(target);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
