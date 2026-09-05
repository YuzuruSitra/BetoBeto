using System;
using System.IO;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using BetoBeto.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BetoBeto.Tests
{
    public sealed class StageAndRulesTests
    {
        StageData data;
        [SetUp] public void Setup() => data = StageData.Parse(File.ReadAllText("Assets/BetoBeto/Stages/kitchen-01.json"));
        [Test] public void SampleMatchesPlayableBoardContract()
        {
            Assert.That(data.Validate(), Is.Empty);
            Assert.That(data.Find('P').Count, Is.EqualTo(2));
            Assert.That(data.Find('X').Count, Is.EqualTo(4));
        }
        [Test] public void JsonRoundTripPreservesLayoutAndRecipe()
        {
            var copy = StageData.Parse(JsonUtility.ToJson(data));
            Assert.That(copy.rows, Is.EqualTo(data.rows));
            Assert.That(copy.recipe.Total, Is.EqualTo(13));
            Assert.That(copy.name, Is.EqualTo(data.name));
        }
        [Test] public void GridAndWorldCoordinatesRoundTripEveryCell()
        {
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                var cell = new Vector2Int(x, y);
                Assert.That(data.Cell(data.World(cell)), Is.EqualTo(cell));
            }
        }
        [Test] public void InvalidRowsAreRejectedBeforeSceneGeneration()
        {
            data.rows[0] = "short";
            Assert.Throws<FormatException>(() => StageData.Parse(JsonUtility.ToJson(data)));
        }
        [Test] public void UnreachablePipeIsRejected()
        {
            data.rows[1] = "################";
            Assert.That(data.Validate(), Has.Some.Contains("到達できません"));
        }
        [Test] public void InvalidRecipeAndTimingsAreRejected()
        {
            data.recipe.melon = -1; data.spawnInterval = 0; data.iceLifetime = float.NaN;
            Assert.That(data.Validate().Count, Is.GreaterThanOrEqualTo(3));
        }
        [Test] public void ExtraStrawberriesCannotReplaceMissingMelon()
        {
            var session = new GameSession(data) { State = GameState.Playing };
            for (int i = 0; i < 20; i++) session.Harvest(FruitKind.Strawberry, 1);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(session.RecipeCount, Is.EqualTo(5));
        }
        [Test] public void CompletingEveryIngredientWinsAndLocksCounters()
        {
            var session = new GameSession(data) { State = GameState.Playing };
            for (int i = 0; i < 4; i++) for (int j = 0; j < data.recipe.For((FruitKind)i); j++) session.Harvest((FruitKind)i, 2);
            Assert.That(session.State, Is.EqualTo(GameState.Won));
            Assert.That(session.Score, Is.EqualTo(2600));
            session.Escape(); session.Harvest(FruitKind.Melon, 8);
            Assert.That(session.TotalHarvested, Is.EqualTo(13));
            Assert.That(session.Escaped, Is.Zero);
        }
        [Test] public void EscapeLimitLosesAtExactlyConfiguredCount()
        {
            var session = new GameSession(data) { State = GameState.Playing };
            for (int i = 0; i < data.escapeLimit - 1; i++) session.Escape();
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            session.Escape(); Assert.That(session.State, Is.EqualTo(GameState.Lost));
        }
        [Test] public void PausedSessionDoesNotCountHarvestsOrEscapes()
        {
            var session = new GameSession(data) { State = GameState.Paused };
            session.Escape(); session.Harvest(FruitKind.Strawberry, 1);
            Assert.That(session.Escaped + session.TotalHarvested, Is.Zero);
        }
        [Test] public void BerriesTurnAtOpenCorner()
        {
            var next = FruitNavigation.Choose(FruitKind.Strawberry, Vector2Int.zero, Directions.Down, true, _ => false, _ => 0);
            Assert.That(next, Is.EqualTo(Directions.Left(Directions.Down)));
        }
        [Test] public void FruitReversesOutOfDeadEndAndWaitsIfEnclosed()
        {
            var next = FruitNavigation.Choose(FruitKind.Blueberry, Vector2Int.zero, Directions.Down, true, c => c != -Directions.Down, _ => 0);
            Assert.That(next, Is.EqualTo(-Directions.Down));
            Assert.That(FruitNavigation.Choose(FruitKind.Blueberry, Vector2Int.zero, Directions.Down, true, _ => true, _ => 0), Is.EqualTo(Vector2Int.zero));
        }
        [Test] public void OrangeReturnsToDownAfterHorizontalMovement()
        {
            var next = FruitNavigation.Choose(FruitKind.Orange, Vector2Int.zero, Vector2Int.left, false, _ => false, _ => 0);
            Assert.That(next, Is.EqualTo(Directions.Down));
        }
        [Test] public void AssetCatalogUsesRealPrefabAssets()
        {
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/BetoBeto/Art/GameAssets.asset");
            Assert.That(assets, Is.Not.Null);
            foreach (var go in new[] { assets.ghost, assets.tile, assets.wall, assets.pipe, assets.shredder, assets.ice, assets.drool, assets.exit })
                Assert.That(PrefabUtility.IsPartOfPrefabAsset(go), Is.True, go == null ? "Missing prefab" : go.name);
            Assert.That(assets.fruits.Length, Is.EqualTo(4));
            foreach (var fruit in assets.fruits) Assert.That(PrefabUtility.IsPartOfPrefabAsset(fruit), Is.True);
        }
        [TestCase(.249f, false, 1)]
        [TestCase(.25f, true, 1)]
        [TestCase(.8f, true, 3)]
        [TestCase(1.499f, true, 5)]
        [TestCase(1.5f, true, 6)]
        [TestCase(10f, true, 6)]
        public void ScareChargeHasExplicitTapThresholdAndMaximum(float seconds, bool radial, int radius)
        {
            Assert.That(ScareRules.IsCharged(seconds), Is.EqualTo(radial));
            Assert.That(ScareRules.Radius(seconds), Is.EqualTo(radius));
            Assert.That(ScareRules.Contains(Vector2Int.zero, Vector2Int.right, Vector2Int.left, seconds), Is.EqualTo(radial));
        }
    }
}
