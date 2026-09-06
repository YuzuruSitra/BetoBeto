using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Stage;
using BetoBeto.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BetoBeto.Editor
{
    public static class KitchenEnvironmentBuilder
    {
        const string Root = "Assets/BetoBeto/Art/Environment";
        [Serializable] class Specs { public Spec[] items; }
        [Serializable] class Spec { public string name; public float[] color; public float roughness,metallic; }

        [MenuItem("BetoBeto/Create Shared Kitchen Environment")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before editing the environment.");
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Prefabs");
            var specs=JsonUtility.FromJson<Specs>("{\"items\":"+File.ReadAllText(Root+"/DecorMaterials.json")+"}").items;
            var materials=new Dictionary<string,Material>();
            foreach(var spec in specs)
            {
                var path=Root+"/Materials/"+spec.name+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                m.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2]).gamma);
                m.SetFloat("_Smoothness",1-spec.roughness);m.SetFloat("_Metallic",spec.metallic);
                EditorUtility.SetDirty(m);materials[spec.name]=m;
            }
            foreach(var name in new[]{"PottedPlant","MixingBowlWhisk","RollingPin","FlourCanister","CookieBakingTray","GinghamTowel"})
            {
                string path=Root+"/Models/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
                foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
                importer.SaveAndReimport();
                var wrapper=new GameObject(name);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),wrapper.transform);
                model.transform.localRotation=Quaternion.Euler(0,180,0);
                PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            }
            var active=SceneManager.GetActiveScene();
            // Existing shared scenes are edited by artists; rebuild is intentionally only for first creation.
            if(File.Exists(KitchenEnvironmentLoader.ScenePath))throw new InvalidOperationException("Shared environment already exists. Edit that scene directly; use Migrate Stage Lighting to update stages.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root=new GameObject("Shared kitchen · lighting and background");
            var environment=root.AddComponent<KitchenEnvironment>();
            environment.reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(StudioReflectionImporter.MapPath);
            var table=GameObject.CreatePrimitive(PrimitiveType.Cube);table.name="Honey maple kitchen table";
            UnityEngine.Object.DestroyImmediate(table.GetComponent<Collider>());
            table.transform.SetParent(root.transform,false);table.transform.localPosition=new Vector3(0,-.85f,0);table.transform.localScale=new Vector3(60,.5f,44);
            var wood=new Material(Shader.Find("BetoBeto/Kitchen Table Wood"));
            AssetDatabase.CreateAsset(wood,Root+"/Materials/KitchenTableWood.mat");table.GetComponent<Renderer>().sharedMaterial=wood;
            SetLayer(table,2);
            var decorations=new List<Transform>();var anchors=new List<Vector2>();var offsets=new List<Vector3>();
            void Place(string model,Vector2 anchor,Vector3 offset,float scale,float yaw)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+model+".prefab"),root.transform);
                go.transform.localScale=Vector3.one*scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
                SetLayer(go,2);decorations.Add(go.transform);anchors.Add(anchor);offsets.Add(offset);
            }
            Place("PottedPlant",new Vector2(-1,1),new Vector3(-2.5f,-.6f,1.8f),1.35f,15);
            Place("PottedPlant",new Vector2(1,1),new Vector3(6,-.6f,2.3f),1.1f,160);
            Place("FlourCanister",new Vector2(-1,1),new Vector3(-.1f,-.6f,2),1,5);
            Place("FlourCanister",new Vector2(-1,1),new Vector3(1.1f,-.6f,2.2f),.8f,-10);
            Place("GinghamTowel",new Vector2(-1,-1),new Vector3(-1.7f,-.58f,-1.9f),1.25f,20);
            Place("MixingBowlWhisk",new Vector2(-1,-1),new Vector3(-1.5f,-.52f,-1.8f),.92f,-30);
            Place("RollingPin",new Vector2(0,-1),new Vector3(-2,-.6f,-1.55f),1,12);
            Place("CookieBakingTray",new Vector2(1,-1),new Vector3(3.2f,-.6f,-.6f),1.1f,-20);
            Place("GinghamTowel",new Vector2(1,0),new Vector3(4,-.58f,0),1.8f,-12);
            environment.recipeCloth=decorations[decorations.Count-1];
            environment.decorations=decorations.ToArray();environment.boardAnchors=anchors.ToArray();environment.offsets=offsets.ToArray();environment.Layout(16,10);
            var sun=new GameObject("Window sunlight · leaf cookie").AddComponent<Light>();sun.transform.SetParent(root.transform,false);
            sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-34,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.58f;
            StudioReflectionImporter.ApplyToActiveScene();
            var camera=new GameObject("Full screen kitchen background").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);
            camera.orthographic=true;camera.depth=-20;camera.cullingMask=1<<2;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.43f,.24f,.12f);
            camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.orthographicSize=10;camera.transform.position=new Vector3(4,18,-10);camera.transform.rotation=Quaternion.Euler(60,0,0);
            environment.backgroundCamera=camera;
            EditorSceneManager.SaveScene(scene,KitchenEnvironmentLoader.ScenePath);
            var build=EditorBuildSettings.scenes.ToList();if(!build.Any(s=>s.path==KitchenEnvironmentLoader.ScenePath))build.Add(new EditorBuildSettingsScene(KitchenEnvironmentLoader.ScenePath,true));EditorBuildSettings.scenes=build.ToArray();
            if(active.IsValid())SceneManager.SetActiveScene(active);
            MigrateStages();
            ApplyBladeMap();AssetDatabase.SaveAssets();
            Debug.Log("Shared kitchen environment created; stage lights migrated.");
        }
        static void SetLayer(GameObject root,int layer){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}

        [MenuItem("BetoBeto/Open Shared Kitchen Environment")]
        public static void OpenEnvironment()
        {
            var scene=SceneManager.GetSceneByPath(KitchenEnvironmentLoader.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(KitchenEnvironmentLoader.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
        }

        [MenuItem("BetoBeto/Migrate Stage Lighting To Shared Scene")]
        public static void MigrateStages()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            var active=SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/BetoBeto/Scenes"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);if(path==KitchenEnvironmentLoader.ScenePath)continue;
                var scene=SceneManager.GetSceneByPath(path);bool open=scene.IsValid()&&scene.isLoaded;
                if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                bool dirty=scene.isDirty;
                var roots=scene.GetRootGameObjects();
                var game=roots.SelectMany(r=>r.GetComponentsInChildren<GameController>(true)).FirstOrDefault();
                if(game!=null)
                {
                    if(game.GetComponent<KitchenEnvironmentLoader>()==null)game.gameObject.AddComponent<KitchenEnvironmentLoader>();
                    foreach(var light in roots.SelectMany(r=>r.GetComponentsInChildren<Light>(true)))
                    {
                        var extra=light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                        if(extra!=null)UnityEngine.Object.DestroyImmediate(extra);
                        UnityEngine.Object.DestroyImmediate(light);
                    }
                    foreach(var camera in roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Where(c=>c.name=="Backdrop Camera"))UnityEngine.Object.DestroyImmediate(camera.gameObject);
                    foreach(var old in roots.Where(r=>r!=null).SelectMany(r=>r.GetComponentsInChildren<BetoBeto.UI.KitchenCameraBackdrop>(true)).ToArray())UnityEngine.Object.DestroyImmediate(old);
                    SceneManager.SetActiveScene(scene);
                    RenderSettings.customReflectionTexture=null;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                if(!open)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        }
        public static void ApplyBladeMap()
        {
            var map=AssetDatabase.LoadAssetAtPath<Cubemap>(Root+"/BladeReflection.exr");
            foreach(var name in new[]{"Shredder_BladeSteel","MovingShredder_BladeSteel"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/BetoBeto/Art/Props/Materials/"+name+".mat");
                material.SetTexture("_MetalReflection",map);material.SetFloat("_MetalReflectionStrength",.28f);material.SetFloat("_ReflectionStrength",2.2f);material.SetFloat("_Metallic",1);EditorUtility.SetDirty(material);
            }
        }
    }
}
