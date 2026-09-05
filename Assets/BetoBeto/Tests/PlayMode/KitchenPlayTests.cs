using System.Collections;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using BetoBeto.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BetoBeto.Tests
{
    public sealed class KitchenPlayTests
    {
        GameController game;
        PadTestInput pad;
        [TearDown] public void CleanupInput() { pad?.Dispose(); }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            pad = new PadTestInput();
            yield return SceneManager.LoadSceneAsync("Kitchen");
            yield return null;
            game = GameController.Instance;
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Playing));
            game.StartGame();
        }
        [Test] public void DroolCannotOverlapWallPipeShredderButCanShareFruitCell()
        {
            Assert.That(game.TryPlaceDrool(new Vector2Int(0, 0)), Is.False);
            Assert.That(game.TryPlaceDrool(game.Board.Pipes[0]), Is.False);
            Assert.That(game.TryPlaceDrool(new Vector2Int(5, 3)), Is.False);
            game.SpawnFruit(FruitKind.Strawberry, new Vector2Int(3, 3));
            Assert.That(game.TryPlaceDrool(new Vector2Int(3, 3)), Is.True);
        }
        [UnityTest] public IEnumerator PausedDroolDoesNotExpireAndResumesCorrectly()
        {
            game.Board.Data.droolLifetime = 1;
            var cell = new Vector2Int(4, 3);
            game.TryPlaceDrool(cell);
            game.Hud.TogglePause();
            Assert.That(game.Session.State, Is.EqualTo(GameState.Paused));
            float before = game.Board.Drool[cell];
            yield return new WaitForSeconds(.2f);
            Assert.That(game.Board.Drool[cell], Is.EqualTo(before));
            game.Hud.TogglePause();
            yield return new WaitForSeconds(1.25f);
            Assert.That(game.Board.Drool.ContainsKey(cell), Is.False);
        }
        [Test] public void DroolStartsASlideAndWallStopsIt()
        {
            game.Board.Walls.Clear(); game.Board.Shredders.Clear();
            game.Board.Walls.Add(new Vector2Int(6, 3));
            var fruit = SpawnReady(FruitKind.Strawberry, new Vector2Int(3, 3));
            fruit.BeginSlide(Vector2Int.right, 1);
            for (int i = 0; i < 8; i++) fruit.Tick(.05f);
            Assert.That(fruit.Cell, Is.EqualTo(new Vector2Int(5, 3)));
            Assert.That(fruit.Sliding, Is.False);
            Assert.That(fruit.Removed, Is.False);
            Assert.That(fruit.IsStunned, Is.True);
            Assert.That(game.Session.TotalHarvested, Is.Zero);
            Vector3 before = fruit.transform.position;
            fruit.Tick(.1f);
            Assert.That(fruit.transform.position.x, Is.LessThan(before.x));
            Assert.That(game.Feedback.HitStopped, Is.True);
        }
        [UnityTest] public IEnumerator DroolCanBePlacedAgainAfterShortCooldown()
        {
            Assert.That(game.TryPlaceDrool(new Vector2Int(4, 3)), Is.True);
            Assert.That(game.DroolCooldown, Is.EqualTo(.3f));
            Assert.That(game.TryPlaceDrool(new Vector2Int(4, 2)), Is.False);
            yield return new WaitForSeconds(.4f);
            Assert.That(game.TryPlaceDrool(new Vector2Int(4, 2)), Is.True);
            Assert.That(game.Board.Drool.Count, Is.EqualTo(2));
        }
        [Test] public void DroolAtFruitCellActuallyTriggersSlide()
        {
            var fruit = SpawnReady(FruitKind.Strawberry, new Vector2Int(4, 3));
            Assert.That(game.TryPlaceDrool(new Vector2Int(4, 3)), Is.True);
            Assert.That(fruit.Sliding, Is.True);
        }
        [Test] public void SlidingFruitPicksUpAnotherFruitAndScoresHarvest()
        {
            game.Board.Walls.Clear(); game.Board.Shredders.Clear();
            game.Board.Shredders.Add(new Vector2Int(6, 3));
            var first = SpawnReady(FruitKind.Strawberry, new Vector2Int(3, 3));
            var second = SpawnReady(FruitKind.Blueberry, new Vector2Int(4, 3));
            first.BeginSlide(Vector2Int.right, 1); first.Tick(.1f);
            Assert.That(second.Sliding, Is.True); Assert.That(second.Chain, Is.EqualTo(2));
            Assert.That(first.Chain, Is.EqualTo(2));
            Assert.That(second.SlidingSpeed, Is.GreaterThan(7.5f));
            for (int i = 0; i < 10 && !second.Removed; i++) second.Tick(.05f);
            Assert.That(second.Removed, Is.True);
            Assert.That(game.Session.Harvested[(int)FruitKind.Blueberry], Is.EqualTo(1));
            Assert.That(game.Session.BestChain, Is.EqualTo(2));
        }
        [Test] public void MelonSurvivesFirstHitButNotSecondAfterImmunity()
        {
            var melon = SpawnReady(FruitKind.Melon, new Vector2Int(5, 2));
            game.TryPlaceDrool(new Vector2Int(5, 2));
            melon.Tick(.15f);
            Assert.That(melon.Health, Is.EqualTo(1)); Assert.That(melon.Removed, Is.False);
            melon.HitShredder(); Assert.That(melon.Health, Is.EqualTo(1));
            Assert.That(melon.BeginSlide(Directions.Down, 1), Is.False);
            melon.Tick(1.2f);
            Assert.That(melon.BeginSlide(Directions.Down, 1), Is.True);
            melon.Tick(.15f);
            Assert.That(melon.Removed, Is.True);
            Assert.That(game.Session.Harvested[(int)FruitKind.Melon], Is.EqualTo(1));
        }
        [TestCase(FruitKind.Strawberry)]
        [TestCase(FruitKind.Blueberry)]
        [TestCase(FruitKind.Orange)]
        [TestCase(FruitKind.Melon)]
        public void WalkingFruitNeverEntersShredder(FruitKind kind)
        {
            // The blade blocks the forward route; the only safe way out is to the right.
            game.Board.Walls.Clear(); game.Board.Shredders.Clear(); game.Board.Exits.Clear();
            game.Board.Walls.Add(new Vector2Int(4, 2));
            game.Board.Walls.Add(new Vector2Int(5, 1));
            game.Board.Shredders.Add(new Vector2Int(5, 3));
            var fruit = SpawnReady(kind, new Vector2Int(5, 2));
            fruit.Tick(.1f);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.right));
            for (int i = 0; i < 300 && !fruit.Removed; i++)
            {
                fruit.Tick(.05f);
                Assert.That(game.Board.Shredders.Contains(fruit.Cell), Is.False);
                Assert.That(game.Board.Shredders.Contains(fruit.TargetCell), Is.False);
            }
            Assert.That(game.Session.TotalHarvested, Is.Zero);
        }
        [Test] public void SurroundedByWallsAndBladeFruitWaitsInsteadOfSuicide()
        {
            game.Board.Walls.Clear(); game.Board.Shredders.Clear();
            game.Board.Shredders.Add(new Vector2Int(5, 3));
            game.Board.Walls.UnionWith(new[] { new Vector2Int(4, 2), new Vector2Int(6, 2), new Vector2Int(5, 1) });
            var fruit = SpawnReady(FruitKind.Strawberry, new Vector2Int(5, 2));
            fruit.Tick(3);
            Assert.That(fruit.Cell, Is.EqualTo(new Vector2Int(5, 2)));
            Assert.That(fruit.Removed, Is.False);
            Assert.That(game.TryPlaceDrool(fruit.Cell), Is.True);
            fruit.Tick(.15f);
            Assert.That(fruit.Removed, Is.True);
            Assert.That(game.Session.TotalHarvested, Is.EqualTo(1));
        }
        [Test] public void WalkingContactCannotHarvestEvenWhenPlacedOnBlade()
        {
            var fruit = SpawnReady(FruitKind.Strawberry, new Vector2Int(5, 3));
            fruit.HitShredder(); game.HarvestFruit(fruit);
            Assert.That(fruit.Health, Is.EqualTo(1));
            Assert.That(fruit.Removed, Is.False);
            Assert.That(game.Session.TotalHarvested, Is.Zero);
        }
        [Test] public void ThreeFruitChainAcceleratesWholeGroupAndHarvestsAllThree()
        {
            game.Board.Walls.Clear(); game.Board.Shredders.Clear();
            game.Board.Shredders.Add(new Vector2Int(7, 3));
            var first = SpawnReady(FruitKind.Strawberry, new Vector2Int(3, 3));
            var second = SpawnReady(FruitKind.Blueberry, new Vector2Int(4, 3));
            var third = SpawnReady(FruitKind.Orange, new Vector2Int(5, 3));
            first.BeginSlide(Vector2Int.right, 1); first.Tick(.05f); second.Tick(.05f);
            Assert.That(new[] { first.Chain, second.Chain, third.Chain }, Is.All.EqualTo(3));
            Assert.That(first.SlidingSpeed, Is.EqualTo(third.SlidingSpeed));
            Assert.That(first.SlidingSpeed, Is.GreaterThan(8));
            for (int i = 0; i < 30; i++) { third.Tick(.05f); second.Tick(.05f); first.Tick(.05f); }
            Assert.That(game.Session.TotalHarvested, Is.EqualTo(3));
            Assert.That(game.Session.Score, Is.EqualTo(900));
            Assert.That(game.Session.BestChain, Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator SixtySecondsWithoutDroolCannotHarvestOnEitherSampleStage()
        {
            foreach (string scene in new[] { "Kitchen", "Kitchen02" })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                game = GameController.Instance;
                game.StartGame();
                foreach (var kind in new[] { FruitKind.Strawberry, FruitKind.Blueberry, FruitKind.Orange, FruitKind.Melon })
                {
                    var fruit = SpawnReady(kind, game.Board.Pipes[0]);
                    for (int tick = 0; tick < 1200 && !fruit.Removed; tick++)
                    {
                        fruit.Tick(.05f);
                        if (!fruit.Removed) Assert.That(game.Board.Shredders.Contains(fruit.Cell), Is.False, scene + ": " + kind);
                    }
                }
                Assert.That(game.Session.TotalHarvested, Is.Zero, scene);
            }
        }
        [UnityTest] public IEnumerator GhostMovesThroughCookieWallWithGamepad()
        {
            var ghost = Object.FindFirstObjectByType<GhostController>();
            var cell = new Vector2Int(7, 3);
            ghost.transform.position = game.Board.Data.World(new Vector2Int(7, 4));
            Assert.That(game.Board.Walls.Contains(cell), Is.True);
            float deadline = Time.realtimeSinceStartup + 1;
            float destination = game.Board.Data.World(cell).z + .05f;
            while (ghost.transform.position.z < destination && Time.realtimeSinceStartup < deadline)
            {
                pad.State(new GamepadState { leftStick = Vector2.up });
                yield return null;
            }
            pad.State(new GamepadState());
            yield return null;
            Assert.That(ghost.transform.position.z, Is.GreaterThan(game.Board.Data.World(cell).z - .3f));
        }
        [Test] public void RetryResetsCountersActorsAndTemporaryObjects()
        {
            game.TryPlaceDrool(new Vector2Int(4, 3));
            game.TryScare(new Vector2Int(4, 3), Vector2Int.right, 0);
            game.Session.Escape(); game.Session.Harvest(FruitKind.Strawberry, 3);
            game.SpawnFruit(FruitKind.Strawberry, new Vector2Int(3, 3));
            game.StartGame();
            Assert.That(game.Session.Escaped + game.Session.Score, Is.Zero);
            Assert.That(game.Board.Drool.Count + game.ActiveFruitCount, Is.Zero);
            Assert.That(game.DroolCooldown, Is.Zero);
            Assert.That(game.Player.IsCharging, Is.False);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Playing));
        }
        FruitAgent SpawnReady(FruitKind kind, Vector2Int cell)
        {
            var fruit = game.SpawnFruit(kind, cell); fruit.Tick(.7f); return fruit;
        }
    }
}
