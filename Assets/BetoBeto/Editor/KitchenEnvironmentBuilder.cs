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
            RefreshBackgroundProps();
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
        [MenuItem("BetoBeto/Refresh Background Cookie And Cloth Materials")]
        public static void RefreshBackgroundProps()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before updating background art.");
            string texturePath=Root+"/Textures/GinghamCloth_Albedo.png";
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            textureImporter.sRGBTexture=true;textureImporter.mipmapEnabled=true;
            textureImporter.wrapMode=TextureWrapMode.Clamp;textureImporter.anisoLevel=4;textureImporter.maxTextureSize=1024;
            textureImporter.SaveAndReimport();
            string materialPath=Root+"/Materials/Decor_GinghamCloth.mat";
            var cloth=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(cloth==null){cloth=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(cloth,materialPath);}
            cloth.SetColor("_BaseColor",Color.white);cloth.SetFloat("_Metallic",0);cloth.SetFloat("_Smoothness",.1f);
            cloth.SetFloat("_Cull",(float)CullMode.Off);
            cloth.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(cloth);
            foreach(var name in new[]{"CookieBakingTray","GinghamTowel"})
            {
                string path=Root+"/Models/"+name+".fbx";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.importBlendShapes=false;
                importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Decor_GinghamCloth"),cloth);
                foreach(var matName in new[]{"BreakableCookie_Surface","BreakableCookie_Crumb"})
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),matName),AssetDatabase.LoadAssetAtPath<Material>("Assets/BetoBeto/Art/Props/Materials/"+matName+".mat"));
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
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
            foreach(var name in new[]{"Shredder_BladeSteel","MovingShredder_BladeSteel"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/BetoBeto/Art/Props/Materials/"+name+".mat");
                ConfigureBladeMaterial(material);
            }
        }

        public static void ConfigureDroolReflection(Material material)
        {
            material.SetTexture("_CeilingMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/KitchenReflection_Source.png"));
            material.SetFloat("_CeilingStrength",.32f);
            material.SetFloat("_CeilingScale",.24f);
            material.SetFloat("_CeilingEyeWarp",.18f);
            material.SetFloat("_LiquidNormalStrength",.18f);
            material.SetFloat("_LiquidWaveScale",2.6f);
            material.SetFloat("_LiquidWaveSpeed",.7f);
            EditorUtility.SetDirty(material);
        }

        public static void ConfigureBladeMaterial(Material material)
        {
            material.SetTexture("_BladePlanarMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/BladePlanarReflection.png"));
            material.SetFloat("_PlanarStrength",.65f);
            material.SetFloat("_PlanarScale",1.3f);
            material.SetFloat("_EyeWarp",.35f);
            material.SetFloat("_OrthoReflectionFov",45f);
            material.SetFloat("_ReflectionStrength",.8f);
            material.SetFloat("_Metallic",1);
            EditorUtility.SetDirty(material);
        }

        [MenuItem("BetoBeto/Rebuild Blade Planar Reflection")]
        public static void BuildBladePlanarMap()
        {
            // Deliberately authored bands, not an environment capture or a normal-based lookup.
            const int size=512;
            var texture=new Texture2D(size,size,TextureFormat.RGB24,false,true);
            var stops=new[]{0f,.08f,.16f,.20f,.225f,.26f,.39f,.48f,.52f,.57f,.61f,.635f,.67f,.81f,.91f,1f};
            var levels=new[]{.42f,.58f,.13f,.065f,.065f,.98f,.70f,.44f,.25f,.11f,.10f,.92f,.96f,.60f,.30f,.42f};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size,v=(y+.5f)/size;
                float t=Mathf.Repeat(u+v+.035f*Mathf.Sin(v*Mathf.PI*2),1);
                int s=0;while(s<stops.Length-2&&t>stops[s+1])s++;
                float k=Mathf.SmoothStep(0,1,Mathf.InverseLerp(stops[s],stops[s+1],t));
                float value=Mathf.Lerp(levels[s],levels[s+1],k);
                pixels[y*size+x]=new Color(value*.91f,value*.96f,value,1);
            }
            texture.SetPixels(pixels);texture.Apply();
            string path=Root+"/BladePlanarReflection.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=size;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            ApplyBladeMap();AssetDatabase.SaveAssets();
        }
    }
}
