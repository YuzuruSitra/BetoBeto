using BetoBeto.Core;
using BetoBeto.Presentation;
using BetoBeto.Stage;
using BetoBeto.UI;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.Tests
{
    public sealed class KitchenUiTests
    {
        [TestCase(1600, 900)]
        [TestCase(1920, 1200)]
        [TestCase(1024, 768)]
        [TestCase(720, 1280)]
        [TestCase(2560, 1080)]
        public void RecipeClothKeepsItsHudPositionAcrossScreenShapes(int width, int height)
        {
            Rect board = KitchenLayout.Viewport(width, height);
            Rect cloth = KitchenLayout.Viewport(KitchenLayout.RecipeCloth, width, height);
            float scale = board.width * width / KitchenLayout.Board.width;
            Assert.That((cloth.center.x - board.center.x) * width / scale,
                Is.EqualTo(KitchenLayout.RecipeCloth.center.x - KitchenLayout.Board.center.x).Within(.001f));
            Assert.That((cloth.center.y - board.center.y) * height / scale,
                Is.EqualTo(KitchenLayout.RecipeCloth.center.y - KitchenLayout.Board.center.y).Within(.001f));
            Assert.That(cloth.width * width / (cloth.height * height),
                Is.EqualTo(KitchenLayout.RecipeCloth.width / KitchenLayout.RecipeCloth.height).Within(.0001f));
            Assert.That(cloth.xMin, Is.GreaterThan(board.xMax), "The cloth must not intrude on the live board.");
            Assert.That(cloth.xMax, Is.LessThanOrEqualTo(1));
            Assert.That(cloth.yMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(cloth.yMax, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void RecipeClothAlignmentSurvivesCameraChangesWithoutDrifting()
        {
            var root = new GameObject("Recipe cloth alignment test");
            try
            {
                var environment = root.AddComponent<KitchenEnvironment>();
                var stage = new GameObject("Stage camera", typeof(Camera)).GetComponent<Camera>();
                stage.transform.SetParent(root.transform);
                stage.orthographic = true;
                stage.rect = KitchenLayout.Viewport(Screen.width, Screen.height);
                stage.transform.SetPositionAndRotation(new Vector3(0, 20, -10), Quaternion.Euler(60, 0, 0));
                var background = new GameObject("Background camera", typeof(Camera)).GetComponent<Camera>();
                background.transform.SetParent(root.transform);
                background.orthographic = true;
                environment.backgroundCamera = background;
                var cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloth.transform.SetParent(root.transform);
                cloth.transform.localScale = new Vector3(3, .08f, 2);
                cloth.transform.SetPositionAndRotation(new Vector3(8, -.58f, 0), Quaternion.Euler(0, -12, 0));
                environment.recipeCloth = cloth.transform;
                Vector3 proportions = cloth.transform.localScale.normalized;
                var vertices = cloth.GetComponent<MeshFilter>().sharedMesh.vertices;

                foreach (float size in new[] { 6f, 11f, 4f, 6f })
                {
                    stage.orthographicSize = size;
                    environment.SyncCamera(stage);
                    Vector3 position = cloth.transform.position;
                    Vector3 scale = cloth.transform.localScale;
                    for (int frame = 0; frame < 300; frame++) environment.SyncCamera(stage);
                    Assert.That(Vector3.Distance(position, cloth.transform.position), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(scale, cloth.transform.localScale), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(proportions, cloth.transform.localScale.normalized), Is.LessThan(.0001f));
                    Assert.That(cloth.transform.position.y, Is.EqualTo(-.58f).Within(.0001f), "Cloth stays on the table plane.");
                    Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                    foreach (Vector3 vertex in vertices)
                    {
                        Vector2 point = background.WorldToViewportPoint(cloth.transform.TransformPoint(vertex));
                        min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                    }
                    Rect target = KitchenLayout.Viewport(KitchenLayout.RecipeCloth, Screen.width, Screen.height);
                    Assert.That(Vector2.Distance((min + max) * .5f, target.center), Is.LessThan(.0001f));
                    Assert.That(max.x - min.x, Is.LessThanOrEqualTo(target.width + .0001f));
                    Assert.That(max.y - min.y, Is.LessThanOrEqualTo(target.height + .0001f));
                    Assert.That(Mathf.Max((max.x - min.x) / target.width, (max.y - min.y) / target.height),
                        Is.EqualTo(1).Within(.0001f), "The real mesh fills one dimension of its HUD opening.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1600, 900, 134f, 108f, 894f, 650f)]
        [TestCase(1920, 1200, 160.8f, 189.6f, 1072.8f, 780f)]
        [TestCase(1024, 768, 85.76f, 165.12f, 572.16f, 416f)]
        [TestCase(720, 1280, 60.3f, 486.1f, 402.3f, 292.5f)]
        [TestCase(2560, 1080, 480.8f, 129.6f, 1072.8f, 780f)]
        public void CameraOpeningKeepsTheIllustrationPositionAndAspectAcrossScreens(
            int width, int height, float left, float bottom, float boardWidth, float boardHeight)
        {
            Rect viewport = KitchenLayout.Viewport(width, height);
            Assert.That(viewport.x * width, Is.EqualTo(left).Within(.01f));
            Assert.That(viewport.y * height, Is.EqualTo(bottom).Within(.01f));
            Assert.That(viewport.width * width, Is.EqualTo(boardWidth).Within(.01f));
            Assert.That(viewport.height * height, Is.EqualTo(boardHeight).Within(.01f));
            Assert.That(viewport.xMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(viewport.yMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(viewport.xMax, Is.LessThanOrEqualTo(1));
            Assert.That(viewport.yMax, Is.LessThanOrEqualTo(1));
            Assert.That(viewport.width * width / (viewport.height * height),
                Is.EqualTo(894f / 650f).Within(.0001f), "Portrait and letterboxed windows must not stretch the board.");
        }

        [Test]
        public void FeedbackUsesBoardCoordinatesInsteadOfTheWholeScreen()
        {
            var center = KitchenLayout.FeedbackPosition(new Vector3(.5f, .5f, 1));
            Assert.That(center.x, Is.EqualTo(581f).Within(.001f));
            Assert.That(center.y, Is.EqualTo(433f).Within(.001f));
            foreach (var point in new[] { new Vector3(-2, -2, 1), new Vector3(2, 2, 1) })
            {
                Vector2 position = KitchenLayout.FeedbackPosition(point);
                Assert.That(position.x - 180, Is.GreaterThanOrEqualTo(KitchenLayout.Board.xMin - .001f));
                Assert.That(position.x + 180, Is.LessThanOrEqualTo(KitchenLayout.Board.xMax + .001f));
                Assert.That(position.y - 35, Is.GreaterThanOrEqualTo(KitchenLayout.Board.yMin));
                Assert.That(position.y + 35, Is.LessThanOrEqualTo(KitchenLayout.Board.yMax));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BackdropMeshLeavesOnlyTheGameplayCameraOpening(bool gameplay)
        {
            var root = new GameObject("Backdrop mesh test", typeof(RectTransform), typeof(KitchenBackdropGraphic));
            var mesh = new Mesh();
            try
            {
                var graphic = root.GetComponent<KitchenBackdropGraphic>();
                graphic.rectTransform.sizeDelta = KitchenLayout.DesignSize;
                graphic.showBoard = gameplay;
                using (var vertices = new VertexHelper())
                {
                    typeof(KitchenBackdropGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                            null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(graphic, new object[] { vertices });
                    vertices.FillMesh(mesh);
                }
                Vector3 offset = -graphic.rectTransform.rect.min;
                Vector3[] points = mesh.vertices;
                int[] indices = mesh.triangles;
                float area = 0;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = points[indices[i]], b = points[indices[i + 1]], c = points[indices[i + 2]];
                    area += Vector3.Cross(b - a, c - a).magnitude * .5f;
                    if (gameplay)
                        Assert.That(KitchenLayout.Board.Contains((a + b + c) / 3 + offset), Is.False,
                            "An opaque background triangle must never cover the live board.");
                }
                float expected = 1600 * 900 - (gameplay ? KitchenLayout.Board.width * KitchenLayout.Board.height : 0);
                Assert.That(area, Is.EqualTo(expected).Within(.1f));
            }
            finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(root); }
        }

        [TestCase(0, 7, 0)]
        [TestCase(1, 7, 1)]
        [TestCase(3, 7, 1)]
        [TestCase(4, 7, 2)]
        [TestCase(6, 7, 2)]
        [TestCase(7, 7, 3)]
        [TestCase(70, 7, 3)]
        [TestCase(0, 0, 0)]
        [TestCase(12, 0, 0)]
        public void ToppingCollectionHandlesPartialSurplusAndUnneededIngredients(int harvested, int goal, int visible)
        {
            int count = 0;
            for (int slot = 0; slot < 3; slot++)
                if (TartPreview.IsToppingCollected(slot, harvested, goal)) count++;
            Assert.That(count, Is.EqualTo(visible));
        }

        [Test]
        public void LiveTartKeepsIngredientProgressSeparateAndResetsForRetry()
        {
            var root = new GameObject("Tart preview test", typeof(RectTransform), typeof(TartPreview));
            try
            {
                var preview = root.GetComponent<TartPreview>();
                preview.Initialize();
                var stage = preview.Stage;
                Assert.That(stage, Is.Not.Null, "The recipe tart model must load from Resources.");
                var data = new StageData { recipe = new Recipe { strawberry = 7, blueberry = 5, orange = 0, melon = 2 } };
                var session = new GameSession(data) { State = GameState.Playing };
                preview.Refresh(session);
                session.Harvest(FruitKind.Strawberry, 1);
                for (int i = 0; i < 12; i++) session.Harvest(FruitKind.Blueberry, 1);
                preview.Refresh(session);

                Assert.That(stage.IsCollected(0, 0), Is.True);
                Assert.That(stage.IsCollected(0, 1), Is.False);
                for (int slot = 0; slot < 3; slot++)
                {
                    Assert.That(stage.IsCollected(1, slot), Is.True, "Surplus berries must fill their own group.");
                    Assert.That(stage.IsCollected(2, slot), Is.False, "Zero-goal fruit must not leave ghost toppings.");
                    Assert.That(stage.IsCollected(3, slot), Is.False, "Surplus berries must not complete missing melon.");
                    foreach (var piece in stage.Pieces(2, slot))
                        Assert.That(piece.gameObject.activeSelf, Is.False, piece.name);
                    foreach (var piece in stage.Pieces(3, slot))
                        Assert.That(piece.gameObject.activeSelf, Is.False, piece.name);
                }
                Assert.That(stage.IsDropping(0, 0), Is.True, "A newly earned ingredient must fall onto the tart.");
                foreach (var piece in stage.Pieces(0, 0))
                {
                    Assert.That(piece.gameObject.activeSelf, Is.True, piece.name);
                    Assert.That(piece.localPosition.y, Is.GreaterThanOrEqualTo(stage.dropHeight),
                        "A dropping ingredient starts a full drop above its place on the tart.");
                }

                preview.Refresh(new GameSession(data));
                for (int kind = 0; kind < 4; kind++)
                    for (int slot = 0; slot < 3; slot++)
                    {
                        Assert.That(stage.IsCollected(kind, slot), Is.False, "Retry must clear the tart.");
                        Assert.That(stage.IsDropping(kind, slot), Is.False, "Retry must cancel ingredients in mid air.");
                        foreach (var piece in stage.Pieces(kind, slot))
                            Assert.That(piece.gameObject.activeSelf, Is.False, piece.name);
                    }
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False, "Decorative art must not block mouse controls.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CompletedShowcaseRestoresAllToppingsAfterARestrictedRecipe()
        {
            var root = new GameObject("Complete tart test", typeof(RectTransform), typeof(TartPreview));
            try
            {
                var preview = root.GetComponent<TartPreview>();
                preview.Initialize();
                var stage = preview.Stage;
                preview.Refresh(new GameSession(new StageData { recipe = new Recipe { strawberry = 1, blueberry = 0, orange = 0, melon = 0 } }));
                preview.ShowComplete();
                for (int kind = 0; kind < 4; kind++)
                    for (int slot = 0; slot < 3; slot++)
                    {
                        Assert.That(stage.IsCollected(kind, slot), Is.True);
                        Assert.That(stage.IsDropping(kind, slot), Is.False, "A finished showcase must not animate its tart together.");
                        foreach (var piece in stage.Pieces(kind, slot))
                        {
                            Assert.That(piece.gameObject.activeSelf, Is.True, piece.name);
                            Assert.That(piece.localPosition.y, Is.LessThan(stage.dropHeight), piece.name);
                        }
                    }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void HarvestThatFinishesNoGroupStillHopsItsIngredients()
        {
            var root = new GameObject("Tart hop test", typeof(RectTransform), typeof(TartPreview));
            try
            {
                var preview = root.GetComponent<TartPreview>();
                preview.Initialize();
                var stage = preview.Stage;
                var data = new StageData { recipe = new Recipe { strawberry = 8, blueberry = 0, orange = 0, melon = 0 } };
                var session = new GameSession(data) { State = GameState.Playing };
                session.Harvest(FruitKind.Strawberry, 1);
                preview.Refresh(session);
                Assert.That(stage.IsCollected(0, 0), Is.True);
                Assert.That(stage.IsDropping(0, 0), Is.False, "The opening state is placed, not dropped.");
                var placed = new List<float>();
                foreach (var piece in stage.Pieces(0, 0)) placed.Add(piece.localPosition.y);

                session.Harvest(FruitKind.Strawberry, 1);
                preview.Refresh(session);
                Assert.That(stage.IsCollected(0, 1), Is.False, "Two of eight must not finish the next group.");
                Assert.That(stage.IsDropping(0, 0), Is.True, "Every harvest must show on the tart.");
                int index = 0;
                foreach (var piece in stage.Pieces(0, 0))
                {
                    Assert.That(piece.localPosition.y, Is.GreaterThan(placed[index]), piece.name);
                    Assert.That(piece.localPosition.y, Is.LessThan(placed[index] + stage.dropHeight * .5f),
                        "A count that finishes no group only hops its ingredients.");
                    index++;
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void EveryIngredientLandsInsideThePanelAndFallsInFromOutsideIt()
        {
            var root = new GameObject("Tart framing test", typeof(RectTransform), typeof(TartPreview));
            try
            {
                var preview = root.GetComponent<TartPreview>();
                preview.Initialize();
                var camera = preview.PreviewCamera;
                Assert.That(camera, Is.Not.Null);
                preview.ShowComplete();
                // The framing may crop the plate for a closer look, but never an ingredient the recipe can ask for.
                for (int kind = 0; kind < 4; kind++)
                    for (int slot = 0; slot < 3; slot++)
                        foreach (var piece in preview.Stage.Pieces(kind, slot))
                        {
                            Vector3 view = camera.WorldToViewportPoint(piece.position);
                            Assert.That(view.x, Is.InRange(0f, 1f), piece.name + " lands outside the recipe panel.");
                            Assert.That(view.y, Is.InRange(0f, 1f), piece.name + " lands outside the recipe panel.");
                            Assert.That(view.z, Is.GreaterThan(0), piece.name);
                        }
                Bounds bounds = preview.Stage.RestBounds;
                Vector3 entry = camera.WorldToViewportPoint(bounds.center + Vector3.up * preview.Stage.dropHeight);
                Assert.That(entry.y, Is.GreaterThan(1), "Ingredients must start above the frame and fall into it.");
                Assert.That(bounds.size.y, Is.GreaterThan(0), "The tart must contribute real geometry.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("UI/KitchenBackdrop")]
        [TestCase("UI/TartBase")]
        [TestCase("UI/FruitIcons")]
        [TestCase("UI/HudChef")]
        [TestCase("UI/HudControls")]
        [TestCase("UI/HudEscape")]
        [TestCase("UI/HudIngredientCard")]
        [TestCase("UI/HudPause")]
        [TestCase("UI/HudProgressBadge")]
        [TestCase("UI/HudRecipeFrame")]
        [TestCase("UI/HudRecipeLabel")]
        [TestCase("UI/HudScore")]
        [TestCase("UI/HudTime")]
        public void IllustratedAssetsAreAvailableToRuntimeResources(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.GreaterThan(32));
            Assert.That(texture.height, Is.GreaterThan(32));
        }

        [Test]
        public void HudChromeUsesIndependentTexturesInsteadOfAFullScreenAtlas()
        {
            var sprites = new[]
            {
                KitchenArt.HudChef, KitchenArt.HudControls, KitchenArt.HudEscape,
                KitchenArt.HudIngredientCard, KitchenArt.HudPause, KitchenArt.HudProgressBadge,
                KitchenArt.HudRecipeFrame, KitchenArt.HudRecipeLabel, KitchenArt.HudScore, KitchenArt.HudTime
            };
            var textures = new HashSet<Texture>();
            foreach (var sprite in sprites)
            {
                Assert.That(sprite, Is.Not.Null);
                Assert.That(textures.Add(sprite.texture), Is.True, sprite.name + " must be an independent asset.");
            }
        }

        [Test]
        public void HudPngsKeepTransparentCornersAndRecipeOpening()
        {
            string[] names =
            {
                "HudChef", "HudControls", "HudEscape", "HudIngredientCard", "HudPause",
                "HudProgressBadge", "HudRecipeFrame", "HudRecipeLabel", "HudScore", "HudTime"
            };
            foreach (string name in names)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    string path = "Assets/BetoBeto/Resources/UI/" + name + ".png";
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                    Assert.That(texture.GetPixel(0, 0).a, Is.LessThan(.01f), name + " top-left");
                    Assert.That(texture.GetPixel(texture.width - 1, 0).a, Is.LessThan(.01f), name + " top-right");
                    Assert.That(texture.GetPixel(0, texture.height - 1).a, Is.LessThan(.01f), name + " bottom-left");
                    Assert.That(texture.GetPixel(texture.width - 1, texture.height - 1).a, Is.LessThan(.01f),
                        name + " bottom-right");
                    if (name == "HudRecipeFrame")
                        Assert.That(texture.GetPixel(texture.width / 2, texture.height / 2).a,
                            Is.LessThan(.01f), "The existing 3D tart must remain visible through the recipe frame.");
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void FruitAtlasUsesFourDistinctQuadrantsInRecipeOrder()
        {
            var texture = Resources.Load<Texture2D>("UI/FruitIcons");
            Assert.That(texture, Is.Not.Null);
            for (int kind = 0; kind < 4; kind++)
            {
                var sprite = KitchenArt.Fruit(kind);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.texture, Is.SameAs(texture));
                Assert.That(sprite.rect.width, Is.EqualTo(texture.width / 2f));
                Assert.That(sprite.rect.height, Is.EqualTo(texture.height / 2f));
                Assert.That(sprite.rect.x, Is.EqualTo(kind % 2 == 0 ? 0 : texture.width / 2f));
                Assert.That(sprite.rect.y, Is.EqualTo(kind < 2 ? texture.height / 2f : 0));
            }
        }

        [TestCase("Fonts/ZenMaruGothic-Bold", "ごほうびフルーツタルト")]
        [TestCase("Fonts/MPLUSRounded1c-Medium", "おばけのスイーツキッチンよだれ□×")]
        [TestCase("Fonts/MPLUSRounded1c-Bold", "0123456789:/%脱出")]
        public void ProvidedFontsLoadAndContainTheirUiCharacters(string path, string text)
        {
            var font = Resources.Load<Font>(path);
            Assert.That(font, Is.Not.Null, path);
            font.RequestCharactersInTexture(text, 24, FontStyle.Normal);
            foreach (char character in text)
                Assert.That(font.HasCharacter(character), Is.True, path + " is missing " + character);
        }
    }
}
