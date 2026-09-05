using System.Collections;
using BetoBeto.Core;
using BetoBeto.Enemies;
using BetoBeto.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BetoBeto.Tests
{
    public sealed class ScarePlayTests
    {
        GameController game;
        PadTestInput pad;
        [UnitySetUp] public IEnumerator Setup()
        {
            pad = new PadTestInput();
            yield return SceneManager.LoadSceneAsync("Kitchen");
            yield return null; yield return null;
            game = GameController.Instance;
            // Advance enemies explicitly so range and movement assertions do not depend on frame rate.
            game.enabled = false;
            game.Board.Walls.Clear(); game.Board.Shredders.Clear(); game.Board.Exits.Clear();
            game.Board.Jellies.Clear(); game.Board.Cookies.Clear(); game.Board.Scones.Clear();
            game.Board.Freezers.Clear(); game.Board.Movers.Clear();
            game.Player.transform.position = game.Board.Data.World(new Vector2Int(7, 4));
        }
        [TearDown] public void Cleanup() { pad.Dispose(); }

        [Test] public void TapAffectsOwnAndFrontTilesAndCanImmediatelyBeUsedAgain()
        {
            var overlap = Spawn(new Vector2Int(7, 4));
            var front = Spawn(new Vector2Int(8, 4));
            var back = Spawn(new Vector2Int(6, 4));
            var side = Spawn(new Vector2Int(7, 5));
            var far = Spawn(new Vector2Int(9, 4));
            Assert.That(game.TryScare(new Vector2Int(7, 4), Vector2Int.right, .1f), Is.True);
            Assert.That(overlap.IsFleeing && front.IsFleeing, Is.True);
            Assert.That(overlap.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(front.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(back.IsFleeing || side.IsFleeing || far.IsFleeing, Is.False);
            Assert.That(game.TryScare(new Vector2Int(7, 4), Vector2Int.left, .1f), Is.True);
            Assert.That(back.IsFleeing, Is.True);
            Assert.That(back.Direction, Is.EqualTo(Vector2Int.left));
            Assert.That(overlap.Direction, Is.EqualTo(Vector2Int.left));
            Assert.That(side.IsFleeing || far.IsFleeing, Is.False);
        }
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void TapMovesOverlappingAndFrontFruitInPlayerFacingDirection(int x, int y)
        {
            var source = new Vector2Int(7, 4);
            var facing = new Vector2Int(x, y);
            var overlap = Spawn(source);
            var front = Spawn(source + facing);
            var escape = new Vector3(facing.x, 0, -facing.y);
            var playerPosition = game.Board.Data.World(source) + escape * .4f;
            Assert.That(game.TryScare(playerPosition, facing, .1f), Is.True);
            foreach (var fruit in new[] { overlap, front })
            {
                Assert.That(fruit.IsFleeing, Is.True);
                Assert.That(fruit.Direction, Is.EqualTo(facing));
                var before = fruit.transform.position;
                fruit.Tick(.1f);
                Assert.That(Vector3.Dot(fruit.transform.position - before, escape), Is.GreaterThan(0),
                    "Both fruit must follow facing, including overlapping fruit behind the player's actual position.");
            }
        }
        [Test] public void FullChargeIncludesSixTileBoundaryButExcludesBeyondCircle()
        {
            var center = new Vector2Int(8, 4);
            var right = Spawn(new Vector2Int(14, 4));
            var left = Spawn(new Vector2Int(2, 4));
            var diagonal = Spawn(new Vector2Int(12, 8));
            var tooFar = Spawn(new Vector2Int(15, 4));
            var outsideDiagonal = Spawn(new Vector2Int(13, 8));
            Assert.That(game.TryScare(center, Vector2Int.right, 1.5f), Is.True);
            Assert.That(right.IsFleeing && left.IsFleeing && diagonal.IsFleeing, Is.True);
            Assert.That(right.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(left.Direction, Is.EqualTo(Vector2Int.left));
            Assert.That(tooFar.IsFleeing || outsideDiagonal.IsFleeing, Is.False);
        }
        [Test] public void PartialChargeReachesAroundPlayerWithoutFullRadius()
        {
            var near = Spawn(new Vector2Int(5, 4));
            var far = Spawn(new Vector2Int(3, 4));
            game.TryScare(new Vector2Int(7, 4), Vector2Int.right, .8f);
            Assert.That(near.IsFleeing, Is.True);
            Assert.That(far.IsFleeing, Is.False);
        }
        [Test] public void ChargedScareUsesActualPlayerPositionWhenSharingOneTile()
        {
            var cell = new Vector2Int(7, 4);
            var fruit = Spawn(cell);
            fruit.transform.position += Vector3.right * .1f;
            game.Player.transform.position = game.Board.Data.World(cell) + Vector3.right * .4f;
            game.TryScare(game.Player.transform.position, Vector2Int.right, 1.5f);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.left), "Flee from the player's actual position, not the tile center.");
            fruit.Tick(.05f);
            Assert.That(fruit.transform.position.x, Is.LessThan(game.Board.Data.World(cell).x + .1f));
        }
        [TestCase(FruitKind.Strawberry)]
        [TestCase(FruitKind.Blueberry)]
        [TestCase(FruitKind.Orange)]
        [TestCase(FruitKind.Melon)]
        public void AllFruitKindsKeepFleeingInsteadOfImmediatelyTurningBack(FruitKind kind)
        {
            var fruit = Spawn(new Vector2Int(7, 4), kind);
            Vector3 source = game.Board.Data.World(new Vector2Int(6, 4));
            fruit.Scare(source);
            for (int i = 0; i < 20; i++) fruit.Tick(.05f);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(fruit.transform.position.x, Is.GreaterThan(source.x + 2));
            Assert.That(fruit.Sliding, Is.False);
            Assert.That(game.Session.TotalHarvested, Is.Zero);
        }
        [Test] public void ScareReversesMidTileMovementWithoutTeleporting()
        {
            var fruit = Spawn(new Vector2Int(7, 4));
            fruit.Tick(.2f);
            Vector3 before = fruit.transform.position;
            Vector3 toward = fruit.Forward;
            var oldDirection = fruit.Direction;
            fruit.Scare(before + toward * 1.1f);
            Assert.That(fruit.Direction, Is.EqualTo(-oldDirection));
            Assert.That(fruit.transform.position, Is.EqualTo(before));
            fruit.Tick(.05f);
            Assert.That(Vector3.Dot(fruit.transform.position - before, toward), Is.LessThan(0));
            Assert.That(Vector3.Distance(fruit.transform.position, before), Is.LessThanOrEqualTo(fruit.Speed * .051f));
        }
        [TestCase(1, 0, false)]
        [TestCase(-1, 0, false)]
        [TestCase(0, 1, false)]
        [TestCase(0, -1, false)]
        [TestCase(1, 0, true)]
        [TestCase(-1, 0, true)]
        [TestCase(0, 1, true)]
        [TestCase(0, -1, true)]
        public void ScaredFruitDivesIntoAdjacentShredderWithoutDrool(int x, int y, bool tap)
        {
            var cell = new Vector2Int(7, 4);
            var direction = new Vector2Int(x, y);
            var fruit = Spawn(cell);
            game.Board.Shredders.Add(cell + direction);
            game.TryScare(cell - direction, direction, tap ? 0 : 1.5f);
            Assert.That(game.Board.Drool, Is.Empty);
            Assert.That(fruit.Sliding, Is.True);
            Assert.That(fruit.Direction, Is.EqualTo(direction));
            Assert.That(game.Session.TotalHarvested, Is.Zero);
            fruit.Tick(.15f);
            Assert.That(fruit.Removed, Is.True);
            Assert.That(game.Session.TotalHarvested, Is.EqualTo(1));
        }
        [TestCase(2, 0)]
        [TestCase(0, 1)]
        [TestCase(-1, 0)]
        public void ScareDoesNotStartDiveUnlessShredderIsImmediatelyAhead(int x, int y)
        {
            var cell = new Vector2Int(7, 4);
            var fruit = Spawn(cell);
            game.Board.Shredders.Add(cell + new Vector2Int(x, y));
            game.TryScare(cell - Vector2Int.right, Vector2Int.right, 0);
            Assert.That(fruit.IsFleeing, Is.True);
            Assert.That(fruit.Sliding, Is.False);
            for (int i = 0; i < 20; i++) fruit.Tick(.05f);
            Assert.That(fruit.Sliding || fruit.Removed, Is.False);
            Assert.That(game.Session.TotalHarvested, Is.Zero);
        }
        [Test] public void ScareDiveStartsFromCurrentPositionWithoutTeleporting()
        {
            var cell = new Vector2Int(7, 4);
            var fruit = Spawn(cell);
            fruit.Scare(game.Board.Data.World(cell - Vector2Int.right));
            fruit.Tick(.2f);
            var before = fruit.transform.position;
            game.Board.Shredders.Add(cell + Vector2Int.right);
            game.TryScare(cell, Vector2Int.right, 0);
            Assert.That(fruit.Sliding, Is.True);
            Assert.That(fruit.transform.position, Is.EqualTo(before));
            fruit.Tick(.03f);
            Assert.That(fruit.transform.position.x, Is.GreaterThan(before.x));
            Assert.That(Vector3.Distance(fruit.transform.position, before), Is.LessThanOrEqualTo(fruit.SlidingSpeed * .031f));
        }
        [Test] public void ScareGuidesFruitOntoDroolThenChainsIntoShredder()
        {
            var fruit = Spawn(new Vector2Int(5, 4));
            var second = Spawn(new Vector2Int(7, 4), FruitKind.Blueberry);
            game.Board.Shredders.Add(new Vector2Int(9, 4));
            game.TryPlaceDrool(new Vector2Int(6, 4));
            game.TryScare(new Vector2Int(4, 4), Vector2Int.right, 0);
            for (int i = 0; i < 50; i++) { fruit.Tick(.05f); if (second.Sliding) second.Tick(.05f); }
            Assert.That(game.Session.TotalHarvested, Is.EqualTo(2));
            Assert.That(game.Session.BestChain, Is.EqualTo(2));
        }
        [Test] public void ScareRedirectsExistingSlideWithoutLosingSharedCombo()
        {
            var first = Spawn(new Vector2Int(5, 4));
            var second = Spawn(new Vector2Int(6, 4));
            first.BeginSlide(Vector2Int.right, 1);
            Assert.That(second.JoinSlide(first), Is.True);
            second.Scare(game.Board.Data.World(new Vector2Int(7, 4)));
            Assert.That(second.Direction, Is.EqualTo(Vector2Int.left));
            Assert.That(second.Sliding, Is.True);
            Assert.That(first.Chain, Is.EqualTo(2));
            Assert.That(second.Chain, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator PadHoldDoesNotFireUntilReleasedAndCapsAtSixTiles()
        {
            var near = Spawn(new Vector2Int(7, 5));
            var boundary = Spawn(new Vector2Int(1, 4));
            var outside = Spawn(new Vector2Int(14, 4));
            pad.State(new GamepadState().WithButton(GamepadButton.West));
            float deadline = Time.realtimeSinceStartup + 5;
            while (game.Player.Charge01 < 1 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Player.Charge01, Is.EqualTo(1));
            Assert.That(game.Player.ScareRadius, Is.EqualTo(6));
            Assert.That(near.IsFleeing || boundary.IsFleeing, Is.False);
            yield return new WaitForSeconds(.15f);
            Assert.That(game.Player.ChargeSeconds, Is.EqualTo(1.5f));
            pad.State(new GamepadState());
            yield return null; yield return null;
            Assert.That(game.Player.IsCharging, Is.False);
            Assert.That(near.IsFleeing && boundary.IsFleeing, Is.True);
            Assert.That(outside.IsFleeing, Is.False);
        }
        [UnityTest] public IEnumerator PauseAndDisconnectCancelChargeWithoutDelayedShot()
        {
            game.enabled = true;
            var fruit = Spawn(new Vector2Int(7, 5));
            pad.State(new GamepadState().WithButton(GamepadButton.West));
            yield return null; yield return null;
            Assert.That(game.Player.IsCharging, Is.True);
            game.Hud.ShowOptions();
            Assert.That(game.Player.IsCharging, Is.False);
            pad.State(new GamepadState());
            yield return null; yield return null;
            game.Hud.TogglePause();
            yield return null; yield return null;
            Assert.That(fruit.IsFleeing, Is.False);
            pad.State(new GamepadState().WithButton(GamepadButton.West));
            yield return null; yield return null;
            Assert.That(game.Player.IsCharging, Is.True);
            InputSystem.DisableDevice(pad.Device);
            yield return null; yield return null;
            Assert.That(game.Player.IsCharging, Is.False);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Paused));
            InputSystem.EnableDevice(pad.Device);
            pad.State(new GamepadState());
            yield return null; yield return null;
            game.Hud.TogglePause();
            yield return null; yield return null;
            Assert.That(fruit.IsFleeing, Is.False);
        }
        [UnityTest] public IEnumerator PadCanRepeatScareAndStartChargingImmediately()
        {
            var fruit = Spawn(new Vector2Int(7, 5));
            yield return pad.Press(GamepadButton.West);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.up));
            fruit.Scare(game.Board.Data.World(new Vector2Int(7, 6)));
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.down));
            yield return pad.Press(GamepadButton.West);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.up));
            pad.State(new GamepadState().WithButton(GamepadButton.West));
            yield return null; yield return null;
            Assert.That(game.Player.IsCharging, Is.True);
            pad.State(new GamepadState());
            yield return null; yield return null;
        }
        FruitAgent Spawn(Vector2Int cell, FruitKind kind = FruitKind.Strawberry)
        {
            var fruit = game.SpawnFruit(kind, cell); fruit.Tick(.7f); return fruit;
        }
    }
}
