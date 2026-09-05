using System;
using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using BetoBeto.Presentation;
using BetoBeto.Stage;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Editor
{
    /// <summary>Generates replaceable, real prefab assets. Existing authored assets are preserved.</summary>
    public static class PrototypeArt
    {
        public const string Root = "Assets/BetoBeto";
        public const string AssetPath = Root + "/Art/GameAssets.asset";
        static Mesh roundedBox;
        static Mesh ring;
        static Mesh triangle;
        static Material cream, dough, biscuitEdge, cocoa, navy, white, blush, leaf, gold, tile, glass, ice, pink, steel, drool, purple;
        static Material jelly, chocolate;

        [MenuItem("BetoBeto/Create Missing Art Assets")]
        public static void CreateMissingArtAssets() => EnsureAssets();

        public static GameAssets EnsureAssets()
        {
            System.IO.Directory.CreateDirectory(Root + "/Art/Materials");
            System.IO.Directory.CreateDirectory(Root + "/Art/Meshes");
            System.IO.Directory.CreateDirectory(Root + "/Prefabs/Stage");
            System.IO.Directory.CreateDirectory(Root + "/Prefabs/Characters");
            System.IO.Directory.CreateDirectory(Root + "/Prefabs/Abilities");
            AssetDatabase.Refresh();
            roundedBox = GetMesh("RoundedBox", CreateRoundedBox);
            ring = GetMesh("Ring", CreateRing);
            triangle = GetMesh("SconeTriangle", CreateTriangle);
            cream = Mat("Vanilla", "FFF1D5", .3f); dough = Mat("Golden biscuit", "E9B876", .22f);
            biscuitEdge = Mat("Baked edges", "B9793F", .19f); cocoa = Mat("Chocolate", "73492E", .25f);
            navy = Mat("Ink", "253544", .4f); white = Mat("Ghost cream", "FFF8F3", .6f);
            blush = Mat("Blush", "F2A9B2", .35f); leaf = Mat("Fresh leaves", "639768", .25f);
            gold = Mat("Brass", "EABB6C", .65f, .5f); tile = Mat("Blue glaze", "679CB7", .63f);
            glass = Mat("Pipe glass", "C7EDF5", .94f, 0, .16f); ice = Mat("Mint ice", "A3E9ED", .7f, 0, .85f);
            pink = Mat("Shredder pink", "E78195", .44f); steel = Mat("Steel", "B2CED6", .7f, .65f);
            drool = Mat("Shiny drool", "63D7C1", .82f); purple = Mat("Apron", "CEAAC4", .4f);
            jelly = Mat("Grape jelly", "BA80DA", .85f, 0, .88f);
            chocolate = Mat("Chocolate coating", "693B27", .86f);
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetPath);
            if (assets == null) { assets = ScriptableObject.CreateInstance<GameAssets>(); AssetDatabase.CreateAsset(assets, AssetPath); }
            assets.tile = Prefab("Stage/BlueTile", Tile);
            assets.wall = Prefab("Stage/CookieWall", Cookie);
            assets.pipe = Prefab("Stage/FruitPipe", Pipe);
            assets.shredder = Prefab("Stage/Shredder", Shredder);
            assets.jelly = Prefab("Stage/Jelly", Jelly);
            assets.cookie = Prefab("Stage/BreakableCookie", BreakableCookie);
            assets.movingShredder = Prefab("Stage/MovingShredder", MovingShredder);
            assets.scone = Prefab("Stage/Scone", Scone);
            MigrateStagePrefab("Freezer", "ChocolateFondue", ChocolateFondue);
            assets.freezer = Prefab("Stage/ChocolateFondue", ChocolateFondue);
            MigrateStagePrefab("Popcorn", "IceWall", IceWall);
            assets.iceWall = Prefab("Stage/IceWall", IceWall);
            assets.jellyMaterial = jelly; assets.cookieMaterial = dough; assets.frostMaterial = chocolate;
            assets.exit = Prefab("Stage/Exit", Exit);
            assets.playerStart = Prefab("Stage/PlayerStart", () => { var go = new GameObject("Player start"); go.AddComponent<StageObject>().kind = StageObjectKind.PlayerStart; return go; });
            assets.ice = Prefab("Abilities/IceBlock", Ice);
            assets.drool = Prefab("Abilities/DroolPuddle", Drool);
            assets.ghost = Prefab("Characters/ApronGhost", Ghost);
            string[] fruitColors = { "E96676", "7687BB", "F0AB4F", "A9BA72" };
            assets.fruits = new GameObject[4]; assets.fruitMaterials = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                var kind = (FruitKind)i;
                var material = Mat(kind.ToString(), fruitColors[i], .43f);
                assets.fruitMaterials[i] = material;
                assets.fruits[i] = Prefab("Characters/" + kind, () => Fruit(kind, material));
            }
            assets.sparkleMaterial = cream;
            assets.droolMaterial = drool;
            string effectPath = Root + "/Art/Materials/Juicy effects.mat";
            assets.effectMaterial = AssetDatabase.LoadAssetAtPath<Material>(effectPath);
            if (assets.effectMaterial == null)
            {
                var effect = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Juicy effects" };
                effect.SetColor("_BaseColor", Color.white);
                effect.SetFloat("_Surface", 1); effect.SetFloat("_ZWrite", 0); effect.SetFloat("_Cull", 0);
                effect.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                effect.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                effect.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                effect.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                effect.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                effect.SetOverrideTag("RenderType", "Transparent"); effect.renderQueue = 3000;
                AssetDatabase.CreateAsset(effect, effectPath);
                assets.effectMaterial = effect;
            }
            string placementPath = Root + "/Art/Materials/Placement preview.mat";
            assets.placementMaterial = AssetDatabase.LoadAssetAtPath<Material>(placementPath);
            if (assets.placementMaterial == null)
            {
                assets.placementMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Placement preview", color = new Color(.48f, .91f, .9f) };
                AssetDatabase.CreateAsset(assets.placementMaterial, placementPath);
            }
            EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
            return assets;
        }
        static GameObject Prefab(string name, Func<GameObject> create)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var go = create();
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return asset;
        }
        static Material Mat(string name, string hex, float smoothness, float metallic = 0, float alpha = 1)
        {
            string path = Root + "/Art/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            ColorUtility.TryParseHtmlString("#" + hex, out var color); color.a = alpha;
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smoothness); mat.SetFloat("_Metallic", metallic);
            if (alpha < 1)
            {
                mat.SetFloat("_Surface", 1); mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(mat, path); return mat;
        }
        static GameObject RootObject(string name, StageObjectKind? kind = null)
        {
            var go = new GameObject(name);
            if (kind.HasValue) go.AddComponent<StageObject>().kind = kind.Value;
            return go;
        }
        static GameObject Tile()
        {
            var go = RootObject("Blue glazed kitchen tile");
            Box(go.transform, "Glazed ceramic", new Vector3(0, -.095f, 0), new Vector3(.964f, .15f, .964f), tile);
            return go;
        }
        static GameObject Cookie()
        {
            var go = RootObject("Sandwich biscuit", StageObjectKind.Wall);
            Box(go.transform, "Bottom biscuit", new Vector3(0, .105f, 0), new Vector3(.91f, .18f, .91f), biscuitEdge);
            Box(go.transform, "Vanilla filling", new Vector3(0, .205f, 0), new Vector3(.87f, .12f, .87f), cream);
            Box(go.transform, "Top biscuit", new Vector3(0, .33f, 0), new Vector3(.94f, .22f, .94f), dough);
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                Sphere(go.transform, "Baked dimple", new Vector3(x * .235f, .44f, z * .235f), new Vector3(.068f, .014f, .068f), biscuitEdge);
            return go;
        }
        static GameObject Pipe()
        {
            var go = RootObject("Food grade glass fruit pipe", StageObjectKind.Pipe);
            Cylinder(go.transform, "Glass chute", new Vector3(0, 1.38f, 0), new Vector3(.74f, .96f, .74f), glass);
            MeshPart(go.transform, "Upper sanitary clamp", ring, new Vector3(0, 2.30f, 0), new Vector3(.94f, 1.6f, .94f), steel);
            MeshPart(go.transform, "Lower sanitary clamp", ring, new Vector3(0, .50f, 0), new Vector3(.94f, 1.6f, .94f), steel);
            Box(go.transform, "Hygienic valve housing", new Vector3(0, .04f, 0), new Vector3(.88f, .12f, .88f), white);
            MeshPart(go.transform, "Upper silicone seal", ring, new Vector3(0, 2.24f, 0), new Vector3(.91f, .8f, .91f), white);
            MeshPart(go.transform, "Lower silicone seal", ring, new Vector3(0, .44f, 0), new Vector3(.91f, .8f, .91f), white);
            Box(go.transform, "Upper clamp latch", new Vector3(.42f, 2.30f, 0), new Vector3(.1f, .12f, .16f), steel);
            Box(go.transform, "Lower clamp latch", new Vector3(.42f, .50f, 0), new Vector3(.1f, .12f, .16f), steel);
            Box(go.transform, "Glass highlight", new Vector3(-.19f, 1.34f, -.29f), new Vector3(.025f, 1.48f, .018f), white);
            Box(go.transform, "Glass glint", new Vector3(.20f, 1.56f, -.285f), new Vector3(.018f, .9f, .018f), white);
            Box(go.transform, "Closed rear guard", new Vector3(0, .28f, .37f), new Vector3(.82f, .46f, .14f), white);
            for (int side = -1; side <= 1; side += 2)
                Box(go.transform, side < 0 ? "Left valve guard" : "Right valve guard", new Vector3(side * .36f, .28f, .01f), new Vector3(.12f, .46f, .72f), white);
            Cylinder(go.transform, "One way flap hinge", new Vector3(0, .52f, -.37f), new Vector3(.09f, .32f, .09f), steel).localRotation = Quaternion.Euler(0, 0, 90);
            Box(go.transform, "Outward opening glass flap", new Vector3(0, .30f, -.465f), new Vector3(.57f, .42f, .035f), glass).localRotation = Quaternion.Euler(25, 0, 0);
            Box(go.transform, "Flap safety edge", new Vector3(0, .11f, -.554f), new Vector3(.61f, .035f, .055f), white).localRotation = Quaternion.Euler(25, 0, 0);
            Box(go.transform, "Feed arrow stem", new Vector3(0, .115f, .045f), new Vector3(.06f, .025f, .23f), leaf);
            for (int side = -1; side <= 1; side += 2)
                Box(go.transform, side < 0 ? "Feed arrow left" : "Feed arrow right", new Vector3(side * .06f, .115f, -.10f), new Vector3(.045f, .025f, .17f), leaf).localRotation = Quaternion.Euler(0, side * 45, 0);
            return go;
        }
        static GameObject Jelly()
        {
            var go = RootObject("Bouncy grape jelly", StageObjectKind.Jelly);
            Cylinder(go.transform, "Dessert plate", new Vector3(0, .03f, 0), new Vector3(.92f, .025f, .92f), cream);
            Box(go.transform, "Jelly body", new Vector3(0, .29f, 0), new Vector3(.8f, .48f, .8f), jelly);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Sphere(go.transform, "Jelly flute", new Vector3(Mathf.Cos(a) * .28f, .28f, Mathf.Sin(a) * .28f), new Vector3(.28f, .48f, .28f), jelly);
            }
            Sphere(go.transform, "Gloss", new Vector3(-.15f, .538f, -.12f), new Vector3(.27f, .025f, .10f), white);
            Sphere(go.transform, "Gloss dot", new Vector3(.14f, .53f, .08f), new Vector3(.09f, .025f, .09f), white);
            return go;
        }
        static GameObject BreakableCookie()
        {
            var go = RootObject("Crumbly chocolate cookie", StageObjectKind.Cookie);
            var solid = new GameObject("Solid").transform; solid.SetParent(go.transform, false);
            Cylinder(solid, "Baked edge", new Vector3(0, .16f, 0), new Vector3(.88f, .16f, .88f), biscuitEdge);
            Cylinder(solid, "Golden cookie", new Vector3(0, .30f, 0), new Vector3(.86f, .10f, .86f), dough);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 2.4f, r = i == 0 ? 0 : .29f;
                Box(solid, "Chocolate chip", new Vector3(Mathf.Cos(a) * r, .405f, Mathf.Sin(a) * r), new Vector3(.09f, .04f, .08f), cocoa).localRotation = Quaternion.Euler(0, i * 37, 0);
            }
            for (int crackIndex = 1; crackIndex <= 2; crackIndex++)
            {
                var crack = new GameObject("Crack " + crackIndex).transform; crack.SetParent(solid, false);
                for (int i = 0; i < 4; i++)
                    Box(crack, "Fissure", new Vector3(i % 2 == 0 ? -.04f : .04f, .414f, -.30f + i * .2f), new Vector3(.033f, .014f, .235f), cocoa).localRotation = Quaternion.Euler(0, i % 2 == 0 ? 24 : -24, 0);
                crack.localRotation = Quaternion.Euler(0, (crackIndex - 1) * 92, 0);
                crack.gameObject.SetActive(false);
            }
            var regrowing = new GameObject("Regrowing").transform; regrowing.SetParent(go.transform, false);
            MeshPart(regrowing, "Regrowth outline", ring, new Vector3(0, .04f, 0), Vector3.one, cream);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 2.4f;
                Box(regrowing, "Crumb", new Vector3(Mathf.Cos(a) * .27f, .035f, Mathf.Sin(a) * .27f), new Vector3(.10f, .05f, .09f), dough);
            }
            regrowing.gameObject.SetActive(false);
            return go;
        }
        static GameObject Scone()
        {
            var go = RootObject("Scone deflector", StageObjectKind.Scone);
            MeshPart(go.transform, "Baked triangle", triangle, Vector3.zero, Vector3.one, biscuitEdge);
            MeshPart(go.transform, "Golden top", triangle, new Vector3(0, .20f, 0), new Vector3(.96f, .35f, .96f), dough);
            Box(go.transform, "Sloped icing edge", new Vector3(.005f, .315f, -.005f), new Vector3(1.18f, .045f, .07f), cream).localRotation = Quaternion.Euler(0, -45, 0);
            Sphere(go.transform, "Raisin", new Vector3(-.24f, .305f, .22f), new Vector3(.10f, .03f, .08f), cocoa);
            Sphere(go.transform, "Raisin", new Vector3(.06f, .305f, .28f), new Vector3(.08f, .03f, .07f), cocoa);
            return go;
        }
        static GameObject ChocolateFondue()
        {
            var go = RootObject("Chocolate fondue", StageObjectKind.Freezer);
            Cylinder(go.transform, "Ceramic dish", new Vector3(0, .07f, 0), new Vector3(.91f, .075f, .91f), cream);
            Cylinder(go.transform, "Chocolate pool", new Vector3(0, .145f, 0), new Vector3(.80f, .025f, .80f), chocolate);
            Cylinder(go.transform, "Fountain stem", new Vector3(0, .25f, 0), new Vector3(.15f, .12f, .15f), chocolate);
            Sphere(go.transform, "Flowing crown", new Vector3(0, .36f, 0), new Vector3(.33f, .14f, .33f), chocolate);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                Sphere(go.transform, "Chocolate stream", new Vector3(Mathf.Cos(a) * .12f, .25f, Mathf.Sin(a) * .12f), new Vector3(.07f, .23f, .07f), chocolate);
            }
            foreach (int sign in new[] { -1, 1 })
                Box(go.transform, "Dish handle", new Vector3(sign * .42f, .115f, 0), new Vector3(.15f, .08f, .25f), gold);
            Sphere(go.transform, "Cream swirl", new Vector3(.23f, .174f, .05f), new Vector3(.12f, .012f, .032f), dough).localRotation = Quaternion.Euler(0, -25, 0);
            return go;
        }
        static GameObject IceWall()
        {
            var go = RootObject("Rising ice wall", StageObjectKind.IceWall);
            var waterMaterial = Mat("Ice wall water", "74CFE5", .94f, 0, .72f);
            var water = new GameObject("Water").transform; water.SetParent(go.transform, false);
            Sphere(water, "Puddle", new Vector3(0, .025f, 0), new Vector3(.84f, .045f, .73f), waterMaterial);
            Sphere(water, "Puddle edge", new Vector3(.25f, .024f, -.18f), new Vector3(.33f, .04f, .32f), waterMaterial);
            MeshPart(water, "Ripple", ring, new Vector3(0, .052f, 0), new Vector3(.45f, .2f, .4f), ice);
            var wall = new GameObject("Wall").transform; wall.SetParent(go.transform, false);
            Box(wall, "Rising ice", new Vector3(0, .53f, 0), new Vector3(.87f, 1.06f, .87f), ice);
            for (int i = 0; i < 3; i++)
            {
                Box(wall, "Frozen crest", new Vector3((i - 1) * .26f, 1.02f, .05f), new Vector3(.20f, .18f + i * .04f, .55f), ice);
                Sphere(wall, "Air bubble", new Vector3((i - 1) * .23f, .30f + i * .2f, -.425f), new Vector3(.08f, .12f, .025f), white);
            }
            Box(wall, "Glint", new Vector3(-.24f, .70f, -.441f), new Vector3(.055f, .42f, .015f), white).localRotation = Quaternion.Euler(0, 0, -22);
            var cracks = new GameObject("Cracks").transform; cracks.SetParent(wall, false);
            for (int i = 0; i < 3; i++)
            {
                Box(cracks, "Front fracture", new Vector3(i % 2 == 0 ? -.035f : .035f, .25f + i * .26f, -.449f), new Vector3(.027f, .30f, .015f), white).localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 27 : -27);
                Box(cracks, "Top fracture", new Vector3((i - 1) * .23f, 1.067f, i % 2 == 0 ? -.12f : -.04f), new Vector3(.29f, .015f, .024f), white).localRotation = Quaternion.Euler(0, i % 2 == 0 ? 25 : -25, 0);
            }
            cracks.gameObject.SetActive(false);
            wall.gameObject.SetActive(false);
            return go;
        }
        static void MigrateStagePrefab(string oldName, string newName, Func<GameObject> create)
        {
            string oldPath = Root + "/Prefabs/Stage/" + oldName + ".prefab";
            string newPath = Root + "/Prefabs/Stage/" + newName + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(oldPath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(newPath) != null) return;
            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            var root = PrefabUtility.LoadPrefabContents(newPath);
            GameObject replacement = null;
            try
            {
                replacement = create();
                while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                while (replacement.transform.childCount > 0) replacement.transform.GetChild(0).SetParent(root.transform, false);
                root.name = replacement.name;
                root.GetComponent<StageObject>().kind = replacement.GetComponent<StageObject>().kind;
                PrefabUtility.SaveAsPrefabAsset(root, newPath);
            }
            finally
            {
                if (replacement != null) UnityEngine.Object.DestroyImmediate(replacement);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        static GameObject MovingShredder()
        {
            var go = Shredder(); go.name = "Rolling berry shredder";
            go.GetComponent<StageObject>().kind = StageObjectKind.MovingShredder;
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Cylinder(go.transform, "Wheel", new Vector3(x * .3f, .1f, z * .43f), new Vector3(.19f, .035f, .19f), navy).localRotation = Quaternion.Euler(90, 0, 0);
            foreach (int sign in new[] { -1, 1 })
                foreach (int side in new[] { -1, 1 })
                    Box(go.transform, "Travel chevron", new Vector3(sign * .37f, .31f, side * .06f), new Vector3(.045f, .035f, .18f), gold).localRotation = Quaternion.Euler(0, sign * side * 45, 0);
            return go;
        }
        static GameObject Shredder()
        {
            var go = RootObject("Berry shredder", StageObjectKind.Shredder);
            Box(go.transform, "Machine body", new Vector3(0, .12f, 0), new Vector3(.93f, .24f, .93f), pink);
            Box(go.transform, "Dark blade well", new Vector3(0, .254f, 0), new Vector3(.75f, .035f, .75f), navy);
            var spin = new GameObject("Blades").transform; spin.SetParent(go.transform, false);
            spin.localPosition = new Vector3(0, .3f, 0);
            spin.gameObject.AddComponent<ActorVisual>().spinning = true;
            Cylinder(spin, "Blade disc", Vector3.zero, new Vector3(.53f, .035f, .53f), steel);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                var tooth = Box(spin, "Tooth", new Vector3(Mathf.Cos(angle) * .265f, 0, Mathf.Sin(angle) * .265f), new Vector3(.16f, .085f, .13f), steel);
                tooth.localRotation = Quaternion.Euler(0, -i * 45 + 20, 0);
            }
            Sphere(spin, "Hub", Vector3.up * .055f, new Vector3(.18f, .09f, .18f), gold);
            for (int i = -1; i <= 1; i += 2) Sphere(go.transform, "Indicator", new Vector3(i * .35f, .26f, -.37f), Vector3.one * .07f, cream);
            return go;
        }
        static GameObject Exit()
        {
            var go = RootObject("Fruit escape chute", StageObjectKind.Exit);
            Box(go.transform, "Escape dark hole", new Vector3(0, .014f, 0), new Vector3(.96f, .055f, .96f), navy);
            for (int i = -1; i <= 1; i++) Box(go.transform, "Grille", new Vector3(i * .26f, .05f, 0), new Vector3(.045f, .05f, .76f), steel);
            var arrow = Box(go.transform, "Exit marker", new Vector3(0, .045f, -.57f), new Vector3(.47f, .055f, .15f), pink);
            return go;
        }
        static GameObject Ice()
        {
            var go = RootObject("Temporary ice block");
            Box(go.transform, "Ice", new Vector3(0, .4f, 0), new Vector3(.86f, .8f, .86f), ice);
            Box(go.transform, "Frozen highlight", new Vector3(-.2f, .55f, -.435f), new Vector3(.095f, .32f, .018f), white).localRotation = Quaternion.Euler(0, 0, -25);
            Sphere(go.transform, "Air bubble", new Vector3(.16f, .5f, -.38f), new Vector3(.12f, .1f, .05f), cream);
            return go;
        }
        static GameObject Drool()
        {
            var go = RootObject("Slippery drool");
            Sphere(go.transform, "Puddle", new Vector3(0, .015f, 0), new Vector3(.91f, .055f, .78f), drool);
            Sphere(go.transform, "Drip", new Vector3(.3f, .014f, .25f), new Vector3(.25f, .045f, .26f), drool);
            Sphere(go.transform, "Gloss", new Vector3(-.2f, .052f, -.12f), new Vector3(.28f, .01f, .085f), white);
            Sphere(go.transform, "Gloss point", new Vector3(.18f, .052f, .1f), new Vector3(.08f, .01f, .08f), white);
            return go;
        }
        static GameObject Ghost()
        {
            var go = RootObject("Apron ghost"); go.AddComponent<GhostController>();
            var visual = new GameObject("Visual").transform; visual.SetParent(go.transform, false);
            visual.gameObject.AddComponent<ActorVisual>().ghost = true;
            Sphere(visual, "Soft ghost body", new Vector3(0, .73f, 0), new Vector3(.76f, .97f, .67f), white);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2 / 5;
                Sphere(visual, "Wavy hem", new Vector3(Mathf.Cos(angle) * .23f, .32f, Mathf.Sin(angle) * .21f), new Vector3(.36f, .29f, .34f), white);
            }
            Sphere(visual, "Apron bib", new Vector3(0, .59f, -.315f), new Vector3(.43f, .39f, .065f), purple);
            Box(visual, "Apron neck band", new Vector3(0, .79f, -.295f), new Vector3(.36f, .07f, .065f), cream);
            Sphere(visual, "Pocket", new Vector3(0, .48f, -.36f), new Vector3(.2f, .12f, .025f), cream);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Sphere(visual, "Eye", new Vector3(sign * .145f, .91f, -.285f), new Vector3(.09f, .16f, .07f), navy);
                Sphere(visual, "Eye shine", new Vector3(sign * .145f - .014f, .952f, -.317f), Vector3.one * .026f, white);
                Sphere(visual, "Pink cheek", new Vector3(sign * .248f, .8f, -.263f), new Vector3(.105f, .055f, .024f), blush);
                Sphere(visual, "Little hand", new Vector3(sign * .365f, .6f, -.05f), new Vector3(.19f, .32f, .22f), white).localRotation = Quaternion.Euler(0, 0, sign * -35);
            }
            Sphere(visual, "Mouth", new Vector3(0, .81f, -.32f), new Vector3(.05f, .035f, .035f), navy);
            return go;
        }
        static GameObject Fruit(FruitKind kind, Material skin)
        {
            var go = RootObject(kind.ToString()); go.AddComponent<FruitAgent>().kind = kind;
            var visual = new GameObject("Visual").transform; visual.SetParent(go.transform, false);
            visual.gameObject.AddComponent<ActorVisual>();
            float scale = kind == FruitKind.Melon ? 1.14f : kind == FruitKind.Blueberry ? .78f : .94f;
            Sphere(visual, "Fruit body", new Vector3(0, .48f, 0), new Vector3(.72f, kind == FruitKind.Strawberry ? .84f : .72f, .68f), skin);
            if (kind == FruitKind.Strawberry)
            {
                Sphere(visual, "Berry tip", new Vector3(0, .25f, 0), new Vector3(.48f, .36f, .46f), skin);
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * 6.283185f / 5;
                    var l = Sphere(visual, "Strawberry leaf", new Vector3(Mathf.Cos(angle) * .14f, .87f, Mathf.Sin(angle) * .14f), new Vector3(.14f, .07f, .31f), leaf);
                    l.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90, 0);
                }
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * 2.399f, y = .27f + (i % 4) * .14f;
                    float radius = .30f * Mathf.Sqrt(Mathf.Max(.3f, 1 - Mathf.Pow((y - .5f) / .48f, 2)));
                    Sphere(visual, "Seed", new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius), new Vector3(.036f, .055f, .034f), cream);
                }
            }
            else if (kind == FruitKind.Blueberry)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 1.2566f;
                    Sphere(visual, "Blueberry crown", new Vector3(Mathf.Cos(a) * .105f, .834f, Mathf.Sin(a) * .105f), new Vector3(.13f, .06f, .13f), navy);
                }
            }
            else
            {
                Cylinder(visual, "Stem", new Vector3(0, .895f, 0), new Vector3(.065f, .12f, .065f), leaf).localRotation = Quaternion.Euler(0, 0, -18);
                Sphere(visual, "Leaf", new Vector3(.13f, .91f, 0), new Vector3(.28f, .06f, .13f), leaf).localRotation = Quaternion.Euler(0, 0, 20);
            }
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Sphere(visual, "Eye white", new Vector3(sign * .133f, .61f, -.305f), new Vector3(.17f, .2f, .07f), white);
                Sphere(visual, "Pupil", new Vector3(sign * .133f, .60f, -.341f), new Vector3(.086f, .112f, .04f), navy);
                Sphere(visual, "Eye sparkle", new Vector3(sign * .133f - .015f, .63f, -.365f), Vector3.one * .029f, white);
                Sphere(visual, "Foot", new Vector3(sign * .18f, .10f, -.06f), new Vector3(.14f, .14f, .2f), cocoa);
                Sphere(visual, "Hand", new Vector3(sign * .36f, .41f, -.03f), new Vector3(.11f, .20f, .12f), skin).localRotation = Quaternion.Euler(0, 0, sign * 32);
            }
            Sphere(visual, "Worried mouth", new Vector3(0, .414f, -.329f), new Vector3(.09f, .10f, .035f), navy);
            if (kind == FruitKind.Melon)
            {
                for (int i = 0; i < 3; i++)
                {
                    var stripe = MeshPart(visual, "Melon rind stripe", ring, new Vector3(0, .48f, 0), new Vector3(.8f, .8f, .8f), cream);
                    stripe.localRotation = Quaternion.Euler(90, i * 60, 0);
                }
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Sphere(visual, "Moustache", new Vector3(sign * .1f, .445f, -.36f), new Vector3(.22f, .10f, .045f), cream).localRotation = Quaternion.Euler(0, 0, sign * 20);
                    Box(visual, "Brow", new Vector3(sign * .14f, .728f, -.313f), new Vector3(.2f, .055f, .045f), leaf);
                }
            }
            visual.localScale = Vector3.one * scale;
            return go;
        }
        internal static Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material) => MeshPart(parent, name, roundedBox, position, scale, material);
        static Transform Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material) => Primitive(parent, name, PrimitiveType.Sphere, position, scale, material);
        static Transform Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material) => Primitive(parent, name, PrimitiveType.Cylinder, position, scale, material);
        static Transform Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }
        static Transform MeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }
        static Mesh GetMesh(string name, Func<Mesh> create)
        {
            string path = Root + "/Art/Meshes/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = create(); mesh.name = name; AssetDatabase.CreateAsset(mesh, path); }
            return mesh;
        }
        static Mesh CreateRoundedBox()
        {
            const int steps = 8;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>();
            Vector3[] normal = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] horizontal = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            for (int face = 0; face < 6; face++)
            {
                var vertical = Vector3.Cross(normal[face], horizontal[face]); int offset = vertices.Count;
                for (int y = 0; y <= steps; y++) for (int x = 0; x <= steps; x++)
                {
                    Vector3 p = normal[face] * .5f + horizontal[face] * ((float)x / steps - .5f) + vertical * ((float)y / steps - .5f);
                    var centre = new Vector3(Mathf.Clamp(p.x, -.36f, .36f), Mathf.Clamp(p.y, -.36f, .36f), Mathf.Clamp(p.z, -.36f, .36f));
                    var n = (p - centre).normalized; vertices.Add(centre + n * .14f); normals.Add(n); uv.Add(new Vector2((float)x / steps, (float)y / steps));
                }
                for (int y = 0; y < steps; y++) for (int x = 0; x < steps; x++)
                {
                    int a = offset + y * (steps + 1) + x, b = a + 1, c = a + steps + 1, d = c + 1;
                    indices.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh CreateRing()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int segments = 32, sides = 8;
            for (int a = 0; a <= segments; a++) for (int b = 0; b <= sides; b++)
            {
                float theta = a * Mathf.PI * 2 / segments, phi = b * Mathf.PI * 2 / sides;
                float r = .42f + Mathf.Cos(phi) * .035f;
                vertices.Add(new Vector3(Mathf.Cos(theta) * r, Mathf.Sin(phi) * .035f, Mathf.Sin(theta) * r));
            }
            for (int a = 0; a < segments; a++) for (int b = 0; b < sides; b++)
            {
                int i = a * (sides + 1) + b, j = i + sides + 1;
                triangles.AddRange(new[] { i, i + 1, j, i + 1, j + 1, j });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh CreateTriangle()
        {
            Vector3[] corners = { new Vector3(-.46f, 0, .46f), new Vector3(.46f, 0, .46f), new Vector3(-.46f, 0, -.46f),
                new Vector3(-.46f, .28f, .46f), new Vector3(.46f, .28f, .46f), new Vector3(-.46f, .28f, -.46f) };
            int[] faces = { 3, 4, 5, 0, 2, 1, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            var vertices = new Vector3[faces.Length]; var indices = new int[faces.Length];
            for (int i = 0; i < faces.Length; i++) { vertices[i] = corners[faces[i]]; indices[i] = i; }
            var mesh = new Mesh { vertices = vertices, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
