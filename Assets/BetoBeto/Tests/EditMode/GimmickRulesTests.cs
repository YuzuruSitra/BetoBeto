using System.IO;
using System.Text.RegularExpressions;
using BetoBeto.Core;
using BetoBeto.Editor;
using BetoBeto.Enemies;
using BetoBeto.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BetoBeto.Tests
{
    public sealed class GimmickRulesTests
    {
        StageLayout layout;
        StageBoard board;
        [SetUp] public void Setup()
        {
            layout = new GameObject("Gimmick rules fixture").AddComponent<StageLayout>();
            layout.sourceJson = File.ReadAllText("Assets/BetoBeto/Stages/kitchen-01.json");
            var data = layout.Read();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(PrototypeArt.AssetPath);
            foreach (var cell in new[] { new Vector2Int(1, 2), new Vector2Int(8, 2), new Vector2Int(11, 2), new Vector2Int(11, 4), new Vector2Int(7, 7) })
            {
                char symbol = data.At(cell);
                var prefab = StageImporter.PlacementPrefab(assets, symbol);
                Assert.That(prefab, Is.Not.Null, symbol.ToString());
                var prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layout.transform);
                prop.transform.position = data.World(cell);
                prop.transform.rotation = StageImporter.PlacementRotation(symbol);
            }
            board = new StageBoard(layout);
        }
        [TearDown] public void Cleanup() { if (layout != null) Object.DestroyImmediate(layout.gameObject); }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TriangleFrontTurnsNinetyDegreesAndFlatBackReverses(int turns)
        {
            Vector2Int Rotate(Vector2Int direction) => GimmickRules.Rotate(direction, turns);
            Assert.That(GimmickRules.SconeReflection(Rotate(Vector2Int.left), turns), Is.EqualTo(Rotate(Directions.Down)));
            Assert.That(GimmickRules.SconeReflection(Rotate(-Directions.Down), turns), Is.EqualTo(Rotate(Vector2Int.right)));
            Assert.That(GimmickRules.SconeReflection(Rotate(Vector2Int.right), turns), Is.EqualTo(Rotate(Vector2Int.left)));
            Assert.That(GimmickRules.SconeReflection(Rotate(Directions.Down), turns), Is.EqualTo(Rotate(-Directions.Down)));
        }
        [Test] public void CookieOpensOnThirdImpactThenWaitsForOccupiedCellToClear()
        {
            var cell = new Vector2Int(8, 2); var cookie = board.Cookies[cell];
            Assert.That(cookie.Hit(), Is.False); Assert.That(cookie.HitsLeft, Is.EqualTo(2));
            Assert.That(cookie.Hit(), Is.False); Assert.That(board.Blocked(cell), Is.True);
            Assert.That(cookie.Hit(), Is.True); Assert.That(board.Blocked(cell), Is.False);
            Assert.That(board.CanPlace(cell), Is.True);
            Assert.That(cookie.Tick(4.9f, false), Is.False);
            Assert.That(cookie.Tick(.2f, true), Is.False);
            Assert.That(cookie.Tick(0, false), Is.False, "Pause must not restore a cookie.");
            Assert.That(cookie.Tick(.01f, false), Is.True);
            Assert.That(cookie.HitsLeft, Is.EqualTo(3)); Assert.That(board.Blocked(cell), Is.True);
        }
        [Test] public void FurtherContactWithBrokenCookieDoesNotRestartItsTimer()
        {
            var cookie = new CookieState(1, 5);
            Assert.That(cookie.Hit(), Is.True); cookie.Tick(2, false);
            Assert.That(cookie.Hit(), Is.False); Assert.That(cookie.Remaining, Is.EqualTo(3));
            Assert.That(cookie.Tick(3, false), Is.True);
        }
        [Test] public void WalkersAvoidDeflectorsButFreezerIsTraversable()
        {
            Assert.That(board.BlocksWalking(new Vector2Int(1, 2)), Is.True);
            Assert.That(board.BlocksWalking(new Vector2Int(11, 2)), Is.True);
            Assert.That(board.BlocksSliding(new Vector2Int(11, 2)), Is.False, "Sliding fruit reaches the slope.");
            Assert.That(board.BlocksWalking(new Vector2Int(11, 4)), Is.False);
            Assert.That(board.CanPlace(new Vector2Int(11, 4)), Is.True);
        }
        [Test] public void MovingBladeReversesAtWallWithoutChangingAxis()
        {
            var mover = new MovingShredderState(new Vector2Int(3, 4), Vector2Int.right);
            Assert.That(mover.Tick(2.5f, 1, c => c.x >= 5 || c.x <= 0), Is.True);
            Assert.That(mover.Direction, Is.EqualTo(Vector2Int.left));
            Assert.That(mover.Position.x, Is.EqualTo(2.5f).Within(.0001f));
            Assert.That(mover.Position.y, Is.EqualTo(4));
        }
        [Test] public void VerticalMovingBladeWaitsWhenBothEndsAreBlockedAndPausesMidSegment()
        {
            var mover = new MovingShredderState(new Vector2Int(3, 4), Directions.Down);
            mover.Tick(10, 1, _ => true);
            Assert.That(mover.Position, Is.EqualTo(new Vector2(3, 4)));
            mover.Tick(.4f, 1, _ => false);
            Vector2 before = mover.Position;
            mover.Tick(0, 1, _ => false);
            Assert.That(mover.Position, Is.EqualTo(before)); Assert.That(before.x, Is.EqualTo(3));
        }
        [Test] public void WalkingFruitAvoidsBothEndsOfMovingBladeSegment()
        {
            var mover = board.Movers[0]; mover.Tick(.7f, 1, _ => false);
            Assert.That(board.BlocksWalking(new Vector2Int(7, 7)), Is.True);
            Assert.That(board.BlocksWalking(new Vector2Int(8, 7)), Is.True);
            var direction = FruitNavigation.Choose(FruitKind.Strawberry, new Vector2Int(8, 6), Directions.Down, true, board.BlocksWalking, _ => 0);
            Assert.That(direction, Is.Not.EqualTo(Directions.Down));
            Assert.That(board.HasShredder(new Vector2Int(8, 7)), Is.True);
            Assert.That(board.HasShredder(new Vector2Int(7, 7)), Is.False);
        }
        [Test] public void FastSlideDetectsBladeBetweenEndpointsAndDoesNotHitParallelLane()
        {
            Assert.That(GimmickRules.SweepCircle(new Vector2(-2, 0), new Vector2(2, 0), Vector2.zero, .62f, out float time), Is.True);
            Assert.That(time, Is.EqualTo(.345f).Within(.0001f));
            Assert.That(GimmickRules.SweepCircle(new Vector2(-2, 1), new Vector2(2, 1), Vector2.zero, .62f, out _), Is.False);
            Assert.That(GimmickRules.SweepCircle(Vector2.zero, Vector2.zero, Vector2.zero, .62f, out _), Is.True);
            Assert.That(GimmickRules.SweepCircle(Vector2.one, Vector2.one, Vector2.zero, .62f, out _), Is.False);
        }
        [Test] public void OldJsonLoadsDefaultGimmickTimings()
        {
            string old = Regex.Replace(layout.sourceJson, ",\\s*\"(?:cookieHits|cookieRespawnSeconds|movingShredderSpeed|freezerSeconds|frozenSpeedMultiplier)\"\\s*:\\s*[0-9.]+", "");
            var data = StageData.Parse(old);
            Assert.That(data.cookieHits, Is.EqualTo(3)); Assert.That(data.cookieRespawnSeconds, Is.EqualTo(5));
            Assert.That(data.movingShredderSpeed, Is.EqualTo(1)); Assert.That(data.freezerSeconds, Is.EqualTo(3));
            Assert.That(data.frozenSpeedMultiplier, Is.EqualTo(.35f));
        }
        [Test] public void InvalidGimmickParametersAreRejected()
        {
            var data = layout.Read(); data.cookieHits = 0; data.cookieRespawnSeconds = float.NaN;
            data.movingShredderSpeed = -1; data.freezerSeconds = 0; data.frozenSpeedMultiplier = 1;
            Assert.That(data.Validate().Count, Is.EqualTo(5));
        }
        [Test] public void BothExampleRoutesHaveAnUnbrokenPathFromSlopeToBlade()
        {
            var first = layout.Read();
            Assert.That(GimmickRules.SconeReflection(Vector2Int.right, first.At(new Vector2Int(11, 2)) - '1'), Is.EqualTo(Directions.Down));
            Assert.That(first.At(new Vector2Int(11, 4)), Is.EqualTo('F'));
            Assert.That(first.At(new Vector2Int(11, 6)), Is.EqualTo('X'));
            for (int y = 3; y < 6; y++) Assert.That(GimmickRules.BlocksConnectivity(first.At(new Vector2Int(11, y))), Is.False);
            var second = StageData.Parse(File.ReadAllText("Assets/BetoBeto/Stages/kitchen-02.json"));
            Assert.That(GimmickRules.SconeReflection(Directions.Down, second.At(new Vector2Int(14, 7)) - '1'), Is.EqualTo(Vector2Int.left));
            for (int x = 5; x < 14; x++) Assert.That(GimmickRules.BlocksConnectivity(second.At(new Vector2Int(x, 7))), Is.False);
            Assert.That(second.At(new Vector2Int(4, 7)), Is.EqualTo('X'));
        }
        [Test] public void SceneEditsDetermineSconeOrientationAndMovingBladeStart()
        {
            var scone = board.Objects[new Vector2Int(11, 2)]; scone.transform.rotation = Quaternion.Euler(0, 270, 0);
            var mover = board.Objects[new Vector2Int(7, 7)]; mover.transform.position = board.Data.World(new Vector2Int(8, 5));
            mover.transform.rotation = Quaternion.Euler(0, 90, 0);
            var edited = new StageBoard(layout);
            Assert.That(edited.Scones[new Vector2Int(11, 2)], Is.EqualTo(3));
            Assert.That(edited.Movers[0].Start, Is.EqualTo(new Vector2Int(8, 5)));
            Assert.That(edited.Movers[0].Direction, Is.EqualTo(Directions.Down));
        }
    }
}
