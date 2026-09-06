using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] string jsonPath = "";
        [SerializeField] SceneAsset updateTarget;
        string message = "JSONを選択し、新規作成または既存シーンの更新を選んでください。";
        Vector2 scroll;
        [MenuItem("BetoBeto/Stage JSON Importer")]
        public static void Open() => GetWindow<StageImporter>("BetoBeto · Stage Importer");
        void OnEnable()
        {
            minSize = new Vector2(440, 480);
            if (updateTarget == null) SelectOpenStage();
        }
        void SelectOpenStage()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path) && SceneComponents<StageLayout>(scene).Length == 1)
                updateTarget = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
        }
        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("ステージJSON → Unityシーン", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("書き出したJSONを読み込み、配置とステージのルールをUnityに反映します。", MessageType.Info);
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
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            if (playing) EditorGUILayout.HelpBox("シーンの作成・更新は再生を停止してから行ってください。", MessageType.Info);
            using (new EditorGUI.DisabledScope(playing))
            {
                GUILayout.Space(8);
                GUILayout.Label("新規ステージ", EditorStyles.boldLabel);
                if (GUILayout.Button("新しいシーンを生成して保存"))
                {
                    try
                    {
                        string json = File.ReadAllText(jsonPath); StageData.Parse(json);
                        string savePath = EditorUtility.SaveFilePanelInProject("シーンを保存", "Kitchen", "unity", "保存先を指定", "Assets/BetoBeto/Scenes");
                        if (!string.IsNullOrEmpty(savePath))
                        {
                            if (File.Exists(savePath)) throw new IOException("同じシーンへ反映するには、下の「既存シーンを更新して保存」を使用してください。");
                            CreateScene(json, savePath); message = "生成しました: " + savePath;
                        }
                    }
                    catch (Exception e) { message = e.Message; }
                }
                GUILayout.Space(12);
                GUILayout.Label("既存ステージ", EditorStyles.boldLabel);
                updateTarget = (SceneAsset)EditorGUILayout.ObjectField("更新するシーン", updateTarget, typeof(SceneAsset), false);
                if (GUILayout.Button("開いているステージを選択")) SelectOpenStage();
                if (updateTarget != null) EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(updateTarget), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.HelpBox("StageのTiles・Placements内をJSONから作り直して上書き保存します。この中で行った個別の配置調整も置き換わります。カメラ・照明・それ以外のオブジェクトは維持し、ステージ名と選択画面のプレビューも更新します。", MessageType.Info);
                using (new EditorGUI.DisabledScope(updateTarget == null))
                {
                    if (GUILayout.Button("既存シーンを更新して保存"))
                    {
                        try
                        {
                            string path = AssetDatabase.GetAssetPath(updateTarget);
                            UpdateScene(File.ReadAllText(jsonPath), path);
                            message = "更新しました: " + path + "\nステージセレクトから再選択して確認できます。WebGL版への反映には再ビルドが必要です。";
                        }
                        catch (Exception e) { message = e.Message; }
                    }
                }
            }
            if (GUILayout.Button("独立ステージエディタを開く")) Application.OpenURL(new Uri(Path.GetFullPath("Tools/StageEditor/index.html")).AbsoluteUri);
            EditorGUILayout.HelpBox(message, MessageType.None); EditorGUILayout.EndScrollView();
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
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("再生を停止してからシーンを作成してください。");
            if (File.Exists(scenePath)) throw new IOException("既存シーンにはUpdateSceneを使用してください。");
            var data = StageData.Parse(json);
            var assets = PrototypeArt.EnsureAssets();
            // Additive preserves unsaved work in any currently open scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var layout = new GameObject("Stage · " + data.name).AddComponent<StageLayout>();
            layout.sourceJson = json;
            layout.tiles = new GameObject("Tiles").transform; layout.tiles.SetParent(layout.transform);
            layout.placements = new GameObject("Placements · edit prefab instances here").transform; layout.placements.SetParent(layout.transform);
            PopulateLayout(data, assets, layout.tiles, layout.placements);
            var environment = new GameObject("Kitchen presentation").transform;
            var counterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ColorUtility.TryParseHtmlString("#32576B", out var color); counterMaterial.color = color;
            string counterPath = "Assets/BetoBeto/Art/Materials/Countertop.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(counterPath);
            if (existing == null) AssetDatabase.CreateAsset(counterMaterial, counterPath); else { UnityEngine.Object.DestroyImmediate(counterMaterial); counterMaterial = existing; }
            if (assets.countertop != null)
            {
                var counter = (GameObject)PrefabUtility.InstantiatePrefab(assets.countertop, environment);
                counter.name = "Kitchen countertop";
                counter.transform.localPosition = new Vector3(0, -.31f, 0);
                counter.transform.localScale = new Vector3(data.width + 1.3f, .38f, data.height + 1.25f);
            }
            else PrototypeArt.Box(environment, "Kitchen countertop", new Vector3(0, -.31f, 0), new Vector3(data.width + 1.3f, .38f, data.height + 1.25f), counterMaterial);
            var camera = new GameObject("Kitchen Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 6.6f; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .22f, .28f);
            camera.transform.position = new Vector3(0, 18, -10.4f); camera.transform.LookAt(new Vector3(0, .3f, 0));
            camera.gameObject.AddComponent<AudioListener>();
            var game = new GameObject("BetoBeto Game").AddComponent<GameController>();
            game.assets = assets; game.layout = layout; game.gameCamera = camera;
            game.gameObject.AddComponent<BetoBeto.Presentation.KitchenEnvironmentLoader>();
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            RegisterStage(scenePath, json);
            Selection.activeGameObject = layout.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(60, 0, 0), 14);
            Debug.Log("BetoBeto scene created: " + scenePath);
            return scene;
        }
        public static Scene UpdateScene(string json, string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("再生を停止してからシーンを更新してください。");
            var data = StageData.Parse(json);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new ArgumentException("更新する既存のシーンを選択してください。");
            if ((File.GetAttributes(scenePath) & FileAttributes.ReadOnly) != 0)
                throw new IOException("更新先のシーンが読み取り専用です。");

            var scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            GameObject prepared = null;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var layouts = SceneComponents<StageLayout>(scene);
                if (layouts.Length != 1) throw new InvalidOperationException("StageLayoutが1つあるゲーム本編のシーンを選択してください。タイトルやステージセレクトは更新できません。");
                var layout = layouts[0];
                if (layout.tiles == null || layout.placements == null || layout.tiles == layout.placements
                    || !layout.tiles.IsChildOf(layout.transform) || !layout.placements.IsChildOf(layout.transform)
                    || layout.tiles == layout.transform || layout.placements == layout.transform
                    || layout.tiles.IsChildOf(layout.placements) || layout.placements.IsChildOf(layout.tiles))
                    throw new InvalidOperationException("StageLayoutのTiles・Placements参照を確認してください。");
                var game = SceneComponents<GameController>(scene).SingleOrDefault(item => item.layout == layout);
                if (game == null || game.assets == null) throw new InvalidOperationException("StageLayoutを参照するGameControllerとGameAssetsが必要です。");
                if (game.assets.tile == null || data.rows.SelectMany(row => row).Any(symbol => symbol != '.' && PlacementPrefab(game.assets, symbol) == null))
                    throw new InvalidOperationException("JSONで使用する配置物のPrefabがGameAssetsに設定されていません。");

                // Finish instantiating first, so malformed input or missing assets cannot erase the authored layout.
                prepared = new GameObject("Preparing stage import");
                SceneManager.MoveGameObjectToScene(prepared, scene);
                var tiles = new GameObject("Tiles").transform; tiles.SetParent(prepared.transform);
                var placements = new GameObject("Placements").transform; placements.SetParent(prepared.transform);
                PopulateLayout(data, game.assets, tiles, placements);
                ReplaceChildren(layout.tiles, tiles);
                ReplaceChildren(layout.placements, placements);
                UnityEngine.Object.DestroyImmediate(prepared); prepared = null;
                layout.sourceJson = json;
                layout.gameObject.name = "Stage · " + data.name;
                // The generated countertop follows the board dimensions; authored cameras and lighting stay intact.
                var presentation = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Kitchen presentation");
                var counter = presentation != null ? presentation.transform.Find("Kitchen countertop") : null;
                if (counter != null)
                {
                    var size = counter.localScale; size.x = data.width + 1.3f; size.z = data.height + 1.25f;
                    counter.localScale = size;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("シーンを保存できませんでした。保存先を確認してください。");
                RegisterStage(scenePath, json, true);
                SceneManager.SetActiveScene(scene);
                Selection.activeGameObject = layout.gameObject;
                Debug.Log("BetoBeto scene updated: " + scenePath);
                return scene;
            }
            catch
            {
                if (prepared != null) UnityEngine.Object.DestroyImmediate(prepared);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                throw;
            }
        }
        static T[] SceneComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        static void PopulateLayout(StageData data, GameAssets assets, Transform tiles, Transform placements)
        {
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                var cell = new Vector2Int(x, y);
                Instantiate(assets.tile, tiles, data.World(cell), $"Tile {x:00},{y:00}");
                char symbol = data.At(cell);
                var prefab = PlacementPrefab(assets, symbol);
                if (prefab == null) continue;
                var prop = Instantiate(prefab, placements, data.World(cell), $"{prefab.name} [{x},{y}]");
                prop.transform.rotation = PlacementRotation(symbol);
            }
        }
        static void ReplaceChildren(Transform destination, Transform prepared)
        {
            for (int i = destination.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(destination.GetChild(i).gameObject);
            while (prepared.childCount > 0) prepared.GetChild(0).SetParent(destination, true);
        }
        static void RegisterStage(string scenePath, string json, bool updateExisting = false)
        {
            var data = StageData.Parse(json);
            const string catalogPath = "Assets/BetoBeto/Resources/StageCatalog.asset";
            Directory.CreateDirectory("Assets/BetoBeto/Resources");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(catalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<StageCatalog>(); AssetDatabase.CreateAsset(catalog, catalogPath); }
            var entries = new List<StageEntry>(catalog.stages ?? Array.Empty<StageEntry>());
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var entry = entries.Find(item => item.sceneName == sceneName);
            if (entry == null || updateExisting)
            {
                TextAsset source = entry?.layoutJson;
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : null;
                bool shared = source != null && entries.Any(other => other != entry && other.layoutJson == source);
                if (source != null && source.text != json && !shared && sourcePath.StartsWith("Assets/", StringComparison.Ordinal)
                    && sourcePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(sourcePath, json);
                    AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceUpdate);
                    source = AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
                }
                if (source != null && source.text != json) source = null;
                if (entry == null)
                    foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/BetoBeto/Stages" }))
                    {
                        var text = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                        if (text != null && text.text == json) { source = text; break; }
                    }
                if (source == null)
                {
                    sourcePath = AssetDatabase.GenerateUniqueAssetPath("Assets/BetoBeto/Stages/" + sceneName + ".json");
                    File.WriteAllText(sourcePath, json); AssetDatabase.ImportAsset(sourcePath); source = AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
                }
                if (entry == null)
                {
                    entry = new StageEntry { id = sceneName, sceneName = sceneName,
                        description = "驚かせて、よだれへ誘導しよう。" };
                    entries.Add(entry);
                }
                entry.title = data.name; entry.layoutJson = source;
                catalog.stages = entries.ToArray(); EditorUtility.SetDirty(catalog);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
            }
            else AssetDatabase.SaveAssetIfDirty(catalog);
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
            StudioReflectionImporter.ApplyToActiveScene();
            EditorSceneManager.SaveScene(scene, path);
        }
        public static GameObject PlacementPrefab(GameAssets assets, char symbol) => symbol switch
        {
            '#' => assets.wall, 'P' => assets.pipe, 'X' => assets.shredder, 'G' => assets.playerStart,
            'J' => assets.jelly, 'C' => assets.cookie, 'H' => assets.movingShredder, 'V' => assets.movingShredder,
            '1' or '2' or '3' or '4' => assets.scone, 'F' => assets.freezer, 'I' => assets.iceWall, _ => null
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
