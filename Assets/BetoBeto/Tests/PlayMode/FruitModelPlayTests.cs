using System.Collections;
using System.Linq;
using BetoBeto.Core;
using BetoBeto.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BetoBeto.Tests
{
    public sealed class FruitModelPlayTests
    {
        GameController game;
        PadTestInput pad;

        [UnitySetUp] public IEnumerator Setup()
        {
            pad = new PadTestInput();
            yield return SceneManager.LoadSceneAsync("Kitchen");
            yield return null;
            yield return null;
            game = GameController.Instance;
            game.enabled = false;
            game.Board.Walls.Clear(); game.Board.Shredders.Clear(); game.Board.Exits.Clear();
            game.Board.Jellies.Clear(); game.Board.Cookies.Clear(); game.Board.Scones.Clear();
            game.Board.Freezers.Clear(); game.Board.Movers.Clear();
        }

        [TearDown] public void Cleanup() => pad.Dispose();

        [UnityTest] public IEnumerator OnlySupineSlidesSpinAndPauseAndStopRestoreCorrectly()
        {
            foreach (int motion in new[] { 1, 2 })
            {
                var cell = new Vector2Int(5, 4);
                var fruit = game.SpawnFruit(FruitKind.Strawberry, cell);
                fruit.Tick(.7f);
                var visual = fruit.GetComponentInChildren<FruitModelVisual>();
                var rest = visual.transform.localRotation;
                Assert.That(fruit.BeginSlide(Vector2Int.right, 1), Is.True);
                ChooseMotion(visual, motion);
                var heading = fruit.transform.rotation;
                yield return new WaitForSeconds(.15f);
                float angle = Quaternion.Angle(rest, visual.transform.localRotation);
                Assert.That(angle, motion == 1 ? Is.GreaterThan(5f) : Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(heading, fruit.transform.rotation), Is.LessThan(.01f));
                Assert.That(Vector3.Dot(visual.transform.up, Vector3.up), Is.GreaterThan(.999f));

                game.Session.State = GameState.Paused;
                var paused = visual.transform.localRotation;
                yield return new WaitForSeconds(.1f);
                Assert.That(Quaternion.Angle(paused, visual.transform.localRotation), Is.LessThan(.01f));
                game.Session.State = GameState.Playing;
                yield return new WaitForSeconds(.1f);
                float resumed = Quaternion.Angle(paused, visual.transform.localRotation);
                Assert.That(resumed, motion == 1 ? Is.GreaterThan(5f) : Is.LessThan(.01f));

                game.Board.Walls.Add(cell + Vector2Int.right);
                fruit.Tick(.2f);
                Assert.That(fruit.Sliding, Is.False);
                yield return null;
                Assert.That(Quaternion.Angle(rest, visual.transform.localRotation), Is.LessThan(.01f));
                game.Board.Walls.Remove(cell + Vector2Int.right);
                fruit.MarkRemoved();
                yield return null;
            }
        }

        // Seed just the presentation's choice so both modes are covered without flaky random tests.
        static void ChooseMotion(FruitModelVisual visual, int motion)
        {
            var previous = Random.state;
            try
            {
                for (int seed = 0; seed < 100; seed++)
                {
                    Random.InitState(seed);
                    if (Random.Range(1, 3) != motion) continue;
                    Random.InitState(seed);
                    visual.Refresh();
                    Assert.That(visual.GetComponentInChildren<Animator>().GetInteger("Motion"), Is.EqualTo(motion));
                    return;
                }
                Assert.Fail("Could not seed the requested slide motion");
            }
            finally { Random.state = previous; }
        }

        [UnityTest] public IEnumerator AllFruitSwitchFacesDuringSlidesAndRestoreAfterWallImpact()
        {
            for (int i = 0; i < 4; i++)
            {
                var cell = new Vector2Int(5, 4);
                var fruit = game.SpawnFruit((FruitKind)i, cell);
                fruit.Tick(.7f);
                var visual = fruit.GetComponentInChildren<FruitModelVisual>();
                Assert.That(visual, Is.Not.Null, fruit.kind.ToString());
                var parts = fruit.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.That(parts.Length, Is.EqualTo(3));
                var normal = parts.Single(p => p.name == "Expression_Normal");
                var surprised = parts.Single(p => p.name == "Expression_Surprised");
                var limbs = parts.Single(p => p.name == "Limbs");
                var animator = fruit.GetComponentInChildren<Animator>();
                yield return null;
                Assert.That(normal.enabled && !surprised.enabled && limbs.enabled, Is.True);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(animator.GetInteger("Motion"), Is.Zero);
                foreach (var part in parts)
                    Assert.That(part.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));

                Assert.That(fruit.BeginSlide(Vector2Int.right, 1), Is.True);
                yield return null;
                Assert.That(!normal.enabled && surprised.enabled && limbs.enabled, Is.True);
                int slideMotion = animator.GetInteger("Motion");
                Assert.That(slideMotion, Is.InRange(1, 2));
                yield return null;
                yield return null;
                Assert.That(animator.GetInteger("Motion"), Is.EqualTo(slideMotion));
                game.Session.State = GameState.Paused;
                yield return null;
                Assert.That(animator.speed, Is.Zero);
                Assert.That(surprised.enabled, Is.True);
                Assert.That(animator.GetInteger("Motion"), Is.EqualTo(slideMotion));
                game.Session.State = GameState.Playing;

                game.Board.Walls.Add(cell + Vector2Int.right);
                fruit.Tick(.2f);
                Assert.That(fruit.Sliding, Is.False);
                yield return null;
                Assert.That(normal.enabled && !surprised.enabled && limbs.enabled, Is.True);
                Assert.That(animator.GetInteger("Motion"), Is.Zero);
                game.Board.Walls.Remove(cell + Vector2Int.right);
                fruit.Tick(.6f);
                Assert.That(fruit.BeginSlide(Vector2Int.down, 1), Is.True);
                yield return null;
                Assert.That(!normal.enabled && surprised.enabled && limbs.enabled, Is.True);
                Assert.That(animator.GetInteger("Motion"), Is.InRange(1, 2));
                fruit.MarkRemoved();
                yield return null;
            }
        }
    }
}
