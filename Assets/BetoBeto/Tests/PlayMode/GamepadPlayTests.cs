using System.Collections;
using BetoBeto.Audio;
using BetoBeto.Core;
using BetoBeto.Player;
using BetoBeto.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BetoBeto.Tests
{
    public sealed class GamepadPlayTests
    {
        PadTestInput pad;
        GameController game;
        [UnitySetUp] public IEnumerator Setup()
        {
            pad = new PadTestInput();
            yield return SceneManager.LoadSceneAsync("Kitchen");
            yield return null; yield return null;
            game = GameController.Instance;
        }
        [TearDown] public void Cleanup() { pad.Dispose(); }

        [UnityTest] public IEnumerator RightStickTurnsInPlaceAndScaresInFrontDroolsAtFeet()
        {
            var ghost = Object.FindFirstObjectByType<GhostController>();
            var cell = new Vector2Int(4, 4);
            ghost.transform.position = game.Board.Data.World(cell);
            pad.State(new GamepadState { rightStick = Vector2.right });
            yield return null; yield return null;
            Assert.That(ghost.Cell, Is.EqualTo(cell));
            Assert.That(ghost.Facing, Is.EqualTo(Vector2Int.right));
            Assert.That(Vector3.Dot(ghost.transform.TransformDirection(Vector3.back), Vector3.right), Is.GreaterThan(.99f));
            pad.State(new GamepadState { rightStick = new Vector2(.04f, -.03f) });
            yield return null; yield return null;
            Assert.That(ghost.Facing, Is.EqualTo(Vector2Int.right), "Stick drift must not change facing.");
            yield return pad.Press(GamepadButton.South);
            Assert.That(game.Board.Drool.ContainsKey(cell), Is.True);
            var fruit = game.SpawnFruit(FruitKind.Strawberry, cell + Vector2Int.right);
            fruit.Tick(.7f);
            yield return pad.Press(GamepadButton.West);
            Assert.That(fruit.IsFleeing, Is.True);
            Assert.That(fruit.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(ghost.transform.position, Is.EqualTo(game.Board.Data.World(cell)));
        }
        [UnityTest] public IEnumerator ScaringEmptyFrontTileDoesNotAffectFruitBehindPlayer()
        {
            var ghost = Object.FindFirstObjectByType<GhostController>();
            ghost.transform.position = game.Board.Data.World(new Vector2Int(7, 4));
            pad.State(new GamepadState { rightStick = Vector2.up });
            yield return null; yield return null;
            game.Board.Walls.Add(ghost.ScareTarget);
            Assert.That(game.Board.Walls.Contains(ghost.ScareTarget), Is.True);
            var fruit = game.SpawnFruit(FruitKind.Strawberry, new Vector2Int(7, 5));
            fruit.Tick(.7f);
            yield return pad.Press(GamepadButton.West);
            Assert.That(fruit.IsFleeing, Is.False);
        }
        [UnityTest] public IEnumerator FruitFaceFollowsWalkingAndCollisionSlide()
        {
            var fruit = game.SpawnFruit(FruitKind.Strawberry, new Vector2Int(3, 3));
            fruit.Tick(.7f); fruit.Tick(.03f);
            Assert.That(Vector3.Dot(fruit.transform.TransformDirection(Vector3.back), fruit.Forward), Is.GreaterThan(.99f));
            Assert.That(fruit.BeginSlide(Vector2Int.right, 1), Is.True);
            Assert.That(Vector3.Dot(fruit.transform.TransformDirection(Vector3.back), Vector3.right), Is.GreaterThan(.99f));
            yield return null;
        }
        [UnityTest] public IEnumerator PadMenusSelectSecondStageAdjustVolumePauseResumeAndReturn()
        {
            yield return SceneManager.LoadSceneAsync("Title"); yield return null; yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ステージを選ぶ    →"));
            yield return pad.Press(GamepadButton.South);
            yield return Arrive("StageSelect");
            yield return pad.Press(GamepadButton.DpadRight);
            yield return pad.Press(GamepadButton.South);
            yield return Arrive("Kitchen02");
            game = GameController.Instance;
            Assert.That(GameFlow.SelectedStage, Is.EqualTo(1));
            Assert.That(game.Board.Drool, Is.Empty, "Menu submit must not leak into gameplay.");
            yield return pad.Press(GamepadButton.Start);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Paused));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("キッチンに戻る"));
            yield return pad.Press(GamepadButton.DpadUp);
            var slider = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
            Assert.That(slider, Is.Not.Null);
            float before = slider.value;
            var button = before > .5f ? GamepadButton.DpadLeft : GamepadButton.DpadRight;
            yield return pad.Press(button);
            Assert.That(slider.value, Is.Not.EqualTo(before));
            slider.value = before;
            yield return pad.Press(GamepadButton.DpadDown);
            yield return pad.Press(GamepadButton.South);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Playing));
            Assert.That(game.Board.Drool, Is.Empty, "Resume must not place drool.");
            yield return pad.Press(GamepadButton.Start);
            yield return pad.Press(GamepadButton.East);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Playing));
            for (int i = 0; i < game.Session.EscapeLimit; i++) game.Session.Escape();
            yield return Arrive("Result");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("もう一度つくる"));
            yield return pad.Press(GamepadButton.South);
            yield return Arrive("Kitchen02");
            yield return pad.Press(GamepadButton.Start);
            yield return pad.Press(GamepadButton.DpadDown);
            yield return pad.Press(GamepadButton.South);
            yield return Arrive("StageSelect");
            yield return pad.Press(GamepadButton.East);
            yield return Arrive("Title");
        }
        [UnityTest] public IEnumerator DisconnectPausesAndReconnectRequiresResume()
        {
            InputSystem.DisableDevice(pad.Device);
            yield return null; yield return null;
            Assert.That(game.Session.State, Is.EqualTo(GameState.Paused));
            float elapsed = game.Session.Elapsed;
            yield return new WaitForSeconds(.1f);
            Assert.That(game.Session.Elapsed, Is.EqualTo(elapsed));
            InputSystem.EnableDevice(pad.Device);
            pad.State(new GamepadState());
            yield return null; yield return null;
            Assert.That(game.Session.State, Is.EqualTo(GameState.Paused));
            yield return pad.Press(GamepadButton.East);
            Assert.That(game.Session.State, Is.EqualTo(GameState.Playing));
        }
        static IEnumerator Arrive(string scene)
        {
            float until = Time.realtimeSinceStartup + 12;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < until) yield return null;
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
        }
    }
}
