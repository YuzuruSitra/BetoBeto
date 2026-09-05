using System;
using System.IO;
using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Stage;
using BetoBeto.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BetoBeto.Editor
{
    public sealed class StageImporter : EditorWindow
    {
        string jsonPath = "";
        string message = "JSONを選択して検証すると、新しい編集可能なシーンを作成できます。";
        Vector2 scroll;
        [MenuItem("BetoBeto/Stage JSON Importer")]
        public static void Open() => GetWindow<StageImporter>("BetoBeto · Stage Importer");
        void OnGUI()
        {
            GUILayout.Label("ステージJSON → Unityシーン", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("壁・パイプ・シュレッダーなどをPrefabとして配置します。新しいシーンに生成し、既存のシーンは保持します。", MessageType.Info);
            jsonPath = EditorGUILayout.TextField("JSON", jsonPath);
            if (GUILayout.Button("JSONを選択"))
            {
                string selected = EditorUtility.OpenFilePanel("BetoBeto stage", Application.dataPath + "/BetoBeto/Stages", "json");
                if (!string.IsNullOrEmpty(selected)) jsonPath = selected;
            }
            if (GUILayout.Button("検証"))
            {
                try { var data = StageData.Parse(File.ReadAllText(jsonPath)); message = $"OK: {data.name}  ({data.width} × {data.height})"; }
                catch (Exception e) { message = e.Message; }
            }
            if (GUILayout.Button("新しいシーンを生成して保存"))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath); StageData.Parse(json);
                    string savePath = EditorUtility.SaveFilePanelInProject("シーンを保存", "Kitchen", "unity", "保存先を指定", "Assets/BetoBeto/Scenes");
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        if (File.Exists(savePath)) throw new IOException("既存シーンの上書きを避けるため、別のファイル名で保存してください。");
                        CreateScene(json, savePath); message = "生成しました: " + savePath;
                    }
                }
                catch (Exception e) { message = e.Message; }
            }
            if (GUILayout.Button("独立ステージエディタを開く")) Application.OpenURL(new Uri(Path.GetFullPath("Tools/StageEditor/index.html")).AbsoluteUri);
            scroll = EditorGUILayout.BeginScrollView(scroll); EditorGUILayout.HelpBox(message, MessageType.None); EditorGUILayout.EndScrollView();
        }
        [MenuItem("BetoBeto/Create Initial Prototype")]
        public static void CreateInitialPrototype()
        {
            const string scenePath = "Assets/BetoBeto/Scenes/Kitchen.unity";
            string json = File.ReadAllText("Assets/BetoBeto/Stages/kitchen-01.json");
            if (!File.Exists(scenePath)) CreateScene(json, scenePath);
            else RegisterStage(scenePath, json);
            const string secondPath = "Assets/BetoBeto/Scenes/Kitchen02.unity";
            string secondJson = File.ReadAllText("Assets/BetoBeto/Stages/kitchen-02.json");
            if (!File.Exists(secondPath)) CreateScene(secondJson, secondPath);
            else RegisterStage(secondPath, secondJson);
            EnsureMenuScene(MenuKind.Title);
            EnsureMenuScene(MenuKind.StageSelect);
            EnsureMenuScene(MenuKind.Result);
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string name in new[] { "Title", "StageSelect", "Kitchen", "Kitchen02", "Result" })
                scenes.Add(new EditorBuildSettingsScene("Assets/BetoBeto/Scenes/" + name + ".unity", true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (!scenes.Exists(s => s.path == existing.path) && existing.path != "Assets/Scenes/SampleScene.unity") scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.productName = "BetoBeto";
            PlayerSettings.companyName = "BetoBeto Kitchen";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            WebBuild.Configure();
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            AssetDatabase.SaveAssets();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/BetoBeto/Scenes/Title.unity");
        }
        public static Scene CreateScene(string json, string scenePath)
        {
            var data = StageData.Parse(json);
            var assets = PrototypeArt.EnsureAssets();
            // Additive preserves unsaved work in any currently open scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var layout = new GameObject("Stage · " + data.name).AddComponent<StageLayout>();
            layout.sourceJson = json;
            layout.tiles = new GameObject("Tiles").transform; layout.tiles.SetParent(layout.transform);
            layout.placements = new GameObject("Placements · edit prefab instances here").transform; layout.placements.SetParent(layout.transform);
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                var cell = new Vector2Int(x, y);
                Instantiate(assets.tile, layout.tiles, data.World(cell), $"Tile {x:00},{y:00}");
                char symbol = data.At(cell);
                var prefab = PlacementPrefab(assets, symbol);
                if (prefab != null)
                {
                    var prop = Instantiate(prefab, layout.placements, data.World(cell), $"{prefab.name} [{x},{y}]");
                    prop.transform.rotation = PlacementRotation(symbol);
                }
            }
            var environment = new GameObject("Kitchen presentation").transform;
            var counterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ColorUtility.TryParseHtmlString("#32576B", out var color); counterMaterial.color = color;
            string counterPath = "Assets/BetoBeto/Art/Materials/Countertop.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(counterPath);
            if (existing == null) AssetDatabase.CreateAsset(counterMaterial, counterPath); else { UnityEngine.Object.DestroyImmediate(counterMaterial); counterMaterial = existing; }
            PrototypeArt.Box(environment, "Kitchen countertop", new Vector3(0, -.31f, 0), new Vector3(data.width + 1.3f, .38f, data.height + 1.25f), counterMaterial);
            var light = new GameObject("Soft kitchen light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.8f; light.color = new Color(1, .94f, .84f); light.shadows = LightShadows.Soft;
            light.shadowStrength = .58f; light.transform.rotation = Quaternion.Euler(48, -34, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.67f, .76f, .82f);
            RenderSettings.skybox = null;
            var camera = new GameObject("Kitchen Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 6.6f; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .22f, .28f);
            camera.transform.position = new Vector3(0, 18, -10.4f); camera.transform.LookAt(new Vector3(0, .3f, 0));
            camera.gameObject.AddComponent<AudioListener>();
            // A separate clear camera fills the letterbox/sidebar area behind the gameplay viewport.
            var backdrop = new GameObject("Backdrop Camera").AddComponent<Camera>();
            backdrop.clearFlags = CameraClearFlags.SolidColor; backdrop.backgroundColor = camera.backgroundColor;
            backdrop.cullingMask = 0; backdrop.depth = -10;
            var game = new GameObject("BetoBeto Game").AddComponent<GameController>();
            game.assets = assets; game.layout = layout; game.gameCamera = camera;
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            RegisterStage(scenePath, json);
            Selection.activeGameObject = layout.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(60, 0, 0), 14);
            Debug.Log("BetoBeto scene created: " + scenePath);
            return scene;
        }
        static void RegisterStage(string scenePath, string json)
        {
            var data = StageData.Parse(json);
            const string catalogPath = "Assets/BetoBeto/Resources/StageCatalog.asset";
            Directory.CreateDirectory("Assets/BetoBeto/Resources");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(catalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<StageCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            var entries = new List<StageEntry>(catalog.stages ?? Array.Empty<StageEntry>());
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (!entries.Exists(entry => entry.sceneName == sceneName))
            {
                TextAsset source = null;
                foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/BetoBeto/Stages" }))
                {
                    var text = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                    if (text != null && text.text == json) { source = text; break; }
                }
                if (source == null)
                {
                    string sourcePath = AssetDatabase.GenerateUniqueAssetPath("Assets/BetoBeto/Stages/" + sceneName + ".json");
                    File.WriteAllText(sourcePath, json); AssetDatabase.ImportAsset(sourcePath); source = AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
                }
                entries.Add(new StageEntry { id = sceneName, title = data.name, sceneName = sceneName, layoutJson = source,
                    description = data.width == 16 ? "2本のパイプと4つの罠。驚かせて、よだれへ誘導しよう。" : "3本のパイプから大にぎわい。長い通路で連鎖をねらおう。" });
                catalog.stages = entries.ToArray(); EditorUtility.SetDirty(catalog);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath)) { scenes.Add(new EditorBuildSettingsScene(scenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            AssetDatabase.SaveAssets();
        }
        static void EnsureMenuScene(MenuKind kind)
        {
            string path = "Assets/BetoBeto/Scenes/" + kind + ".unity";
            if (File.Exists(path)) return;
            var assets = PrototypeArt.EnsureAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var screen = new GameObject(kind + " Screen").AddComponent<MenuScreen>(); screen.screen = kind;
            var camera = new GameObject("Menu Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .22f, .28f);
            camera.orthographic = true; camera.orthographicSize = kind == MenuKind.Title ? 3.2f : 4.5f;
            camera.transform.position = new Vector3(0, 12, -13); camera.transform.LookAt(Vector3.zero);
            camera.gameObject.AddComponent<AudioListener>();
            if (kind == MenuKind.Title)
            {
                camera.rect = new Rect(.45f, .18f, .53f, .66f);
                var background = new GameObject("Menu backdrop").AddComponent<Camera>();
                background.depth = -10; background.cullingMask = 0; background.clearFlags = CameraClearFlags.SolidColor; background.backgroundColor = camera.backgroundColor;
                var light = new GameObject("Menu key light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.8f; light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(48, -30, 0);
                var diorama = new GameObject("Kitchen diorama · prefab models").transform;
                for (int y = 0; y < 5; y++) for (int x = 0; x < 6; x++)
                    Instantiate(assets.tile, diorama, new Vector3(x - 2.5f, -.15f, y - 2), "Ceramic tile");
                foreach (var pos in new[] { new Vector3(-2.5f, 0, 1), new Vector3(-2.5f, 0, 2), new Vector3(-1.5f, 0, 2), new Vector3(2.5f, 0, -2), new Vector3(2.5f, 0, -1), new Vector3(1.5f, 0, -2) })
                    Instantiate(assets.wall, diorama, pos, "Cookie");
                var ghost = (GameObject)PrefabUtility.InstantiatePrefab(assets.ghost, diorama); ghost.transform.localScale = Vector3.one * 1.9f; ghost.transform.position = new Vector3(.2f, .08f, .45f);
                Vector3[] fruits = { new Vector3(-1.8f, 0, -.7f), new Vector3(.4f, 0, -1.4f), new Vector3(2f, 0, .6f), new Vector3(-.8f, 0, 1.75f) };
                for (int i = 0; i < 4; i++) Instantiate(assets.fruits[i], diorama, fruits[i], "Fruit friend");
                Instantiate(assets.drool, diorama, new Vector3(-.8f, -.02f, -.9f), "Drool puddle");
            }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.67f, .76f, .82f); RenderSettings.skybox = null;
            EditorSceneManager.SaveScene(scene, path);
        }
        public static GameObject PlacementPrefab(GameAssets assets, char symbol) => symbol switch
        {
            '#' => assets.wall, 'P' => assets.pipe, 'X' => assets.shredder, 'E' => assets.exit, 'G' => assets.playerStart,
            'J' => assets.jelly, 'C' => assets.cookie, 'H' => assets.movingShredder, 'V' => assets.movingShredder,
            '1' or '2' or '3' or '4' => assets.scone, 'F' => assets.freezer, _ => null
        };
        public static Quaternion PlacementRotation(char symbol) => Quaternion.Euler(0, GimmickRules.IsScone(symbol) ? (symbol - '1') * 90 : symbol == 'V' ? 90 : 0, 0);
        static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 position, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name; go.transform.position = position;
            return go;
        }
    }
}
