using System.Collections;
using BetoBeto.Core;
using BetoBeto.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BetoBeto.Tests
{
    public sealed class GhostModelPlayTests
    {
        GameController game;
        PadTestInput pad;
        Animator animator;
        SkinnedMeshRenderer mesh;

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
            animator = game.Player.GetComponentInChildren<Animator>();
            mesh = game.Player.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(mesh, Is.Not.Null);
        }

        [TearDown] public void Cleanup() => pad.Dispose();

        [UnityTest] public IEnumerator SpeedBlendsAndStopsAtBoardEdge()
        {
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(mesh.sharedMesh.subMeshCount, Is.EqualTo(2));
            Assert.That(mesh.sharedMaterials.Length, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { "BetoBeto/Ghost Body Rim", "Universal Render Pipeline/Lit" },
                System.Array.ConvertAll(mesh.sharedMaterials, material => material.shader.name));
            foreach (var material in mesh.sharedMaterials) Assert.That(material.shader.isSupported, Is.True);
            Assert.That(mesh.sharedMesh.blendShapeCount, Is.GreaterThan(0));
            Assert.That(game.Player.GetComponentInChildren<ActorVisual>(), Is.Null);
            pad.State(new GamepadState { leftStick = new Vector2(.5f, 0) });
            yield return new WaitForSeconds(.3f);
            Assert.That(animator.GetFloat("Speed"), Is.InRange(.3f, .7f));
            pad.State(new GamepadState { leftStick = Vector2.right });
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.GetFloat("Speed"), Is.GreaterThan(.8f));
            game.Player.transform.position = new Vector3((game.Board.Data.width - 1) * .5f, 0, 0);
            yield return new WaitForSeconds(.5f);
            Assert.That(game.Player.MoveSpeed01, Is.Zero);
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(.05f));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
        }

        [UnityTest] public IEnumerator DroolStartsLoopsSmilesPausesAndReturns()
        {
            pad.State(new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSeconds(.15f);
            Assert.That(game.Board.Drool.ContainsKey(game.Player.Cell), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("YODAREStart"), Is.True);
            yield return new WaitForSeconds(.75f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Yodare"), Is.True);
            int smile = mesh.sharedMesh.GetBlendShapeIndex("Smile");
            if (smile < 0) smile = 0;
            Assert.That(mesh.GetBlendShapeWeight(smile), Is.GreaterThan(55));
            game.Session.State = GameState.Paused;
            yield return null;
            yield return null;
            float stopped = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(.15f);
            Assert.That(animator.speed, Is.Zero);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(stopped).Within(.001f));
            game.Session.State = GameState.Playing;
            pad.State(new GamepadState());
            yield return new WaitForSeconds(.35f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            Assert.That(mesh.GetBlendShapeWeight(smile), Is.LessThan(.1f));
        }

        [UnityTest] public IEnumerator ShortDroolCancelsOnReleaseAndScareStillPlaysForConfiguredDuration()
        {
            pad.State(new GamepadState().WithButton(GamepadButton.South));
            yield return null; yield return null;
            pad.State(new GamepadState());
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            var visual = game.Player.GetComponentInChildren<GhostModelVisual>();
            pad.State(new GamepadState().WithButton(GamepadButton.West));
            yield return null; yield return null;
            pad.State(new GamepadState());
            yield return new WaitForSeconds(.15f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Spook"), Is.True);
            yield return new WaitForSeconds(visual.spookSeconds + .15f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
        }

        [UnityTest] public IEnumerator HeldDroolFacesCameraDespiteMovementAndEmitsFromBoneToGround()
        {
            var visual = game.Player.GetComponentInChildren<GhostModelVisual>();
            var vfx = visual.GetComponent<GhostDroolVfx>();
            Assert.That(vfx.origin.name, Is.EqualTo("Yodare"));
            Assert.That(vfx.material.shader.isSupported, Is.True);
            // The animation must still work on a cell where gameplay cannot place a puddle.
            game.Board.Walls.Add(game.Player.Cell);
            pad.State(new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSeconds(1.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Yodare"), Is.True);
            Assert.That(vfx.IsEmitting, Is.True);
            Assert.That(vfx.ActiveDropCount, Is.GreaterThan(20));
            Assert.That(vfx.ImpactEvents, Is.GreaterThan(0));
            var lines = vfx.GetComponentsInChildren<LineRenderer>();
            Assert.That(lines.Length, Is.EqualTo(vfx.streamCount + vfx.dropletCount + 20));
            int poolSize = lines.Length;
            pad.State(new GamepadState { leftStick = Vector2.right, rightStick = Vector2.left }.WithButton(GamepadButton.South));
            yield return new WaitForSeconds(.25f);
            Vector3 towardCamera = -game.GameCamera.transform.forward;
            towardCamera.y = 0;
            Assert.That(Vector3.Dot(-visual.transform.forward, towardCamera.normalized), Is.GreaterThan(.999f));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Yodare"), Is.True);
            foreach (var line in lines)
            {
                if (!line.enabled) continue;
                for (int i = 0; i < line.positionCount; i++)
                    Assert.That(line.GetPosition(i).y, Is.GreaterThanOrEqualTo(vfx.groundHeight - .001f));
            }
            game.Session.State = GameState.Paused;
            yield return null; yield return null;
            var stream = lines[0];
            Vector3 frozen = stream.GetPosition(0);
            yield return new WaitForSeconds(.15f);
            Assert.That(stream.GetPosition(0), Is.EqualTo(frozen));
            game.Session.State = GameState.Playing;
            pad.State(new GamepadState());
            yield return new WaitForSeconds(.2f);
            Assert.That(vfx.IsEmitting, Is.False);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            Assert.That(Quaternion.Angle(visual.transform.localRotation, Quaternion.identity), Is.LessThan(.01f));
            yield return new WaitForSeconds(.7f);
            Assert.That(vfx.ActiveDropCount, Is.Zero);
            Assert.That(vfx.GetComponentsInChildren<LineRenderer>().Length, Is.EqualTo(poolSize));
        }
    }
}
