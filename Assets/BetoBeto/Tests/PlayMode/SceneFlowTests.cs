using System.Collections;
using System.Linq;
using BetoBeto.Audio;
using BetoBeto.Core;
using BetoBeto.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BetoBeto.Tests
{
    public sealed class SceneFlowTests
    {
        [UnityTest]
        public IEnumerator TitleSelectSecondStageWinResultAndRetryUseSeparateScenes()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            Assert.That(GameController.Instance, Is.Null);
            var select = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "ステージを選ぶ    →");
            select.onClick.Invoke();
            yield return Arrive("StageSelect");
            Assert.That(GameController.Instance, Is.Null);
            var stageButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "このキッチンで作る →").OrderBy(b => b.transform.parent.GetSiblingIndex()).ToArray();
            Assert.That(stageButtons.Length, Is.EqualTo(2));
            stageButtons[1].onClick.Invoke();
            yield return Arrive("Kitchen02");
            var game = GameController.Instance;
            Assert.That(game.Board.Data.width, Is.EqualTo(20));
            Assert.That(Object.FindFirstObjectByType<MenuScreen>(), Is.Null);
            for (int i = 0; i < 4; i++) for (int n = 0; n < game.Session.Recipe.For((FruitKind)i); n++) game.Session.Harvest((FruitKind)i, 1);
            yield return Arrive("Result");
            Assert.That(GameController.Instance, Is.Null);
            Assert.That(GameFlow.LastResult.won, Is.True);
            Assert.That(GameFlow.SelectedStage, Is.EqualTo(1));
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "もう一度つくる").onClick.Invoke();
            yield return Arrive("Kitchen02");
            Assert.That(GameController.Instance.Session.Score, Is.Zero);
            GameFlow.StageSelect();
            yield return Arrive("StageSelect");
            Assert.That(Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator EscapeLossReachesResultAndCanReturnToSelection()
        {
            yield return SceneManager.LoadSceneAsync("StageSelect");
            yield return null;
            GameFlow.PlayStage(0);
            yield return Arrive("Kitchen");
            var session = GameController.Instance.Session;
            for (int i = 0; i < session.EscapeLimit; i++) session.Escape();
            yield return Arrive("Result");
            Assert.That(GameFlow.LastResult.won, Is.False);
            Assert.That(GameFlow.LastResult.escaped, Is.EqualTo(session.EscapeLimit));
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "ステージ選択へ").onClick.Invoke();
            yield return Arrive("StageSelect");
        }
        static IEnumerator Arrive(string scene)
        {
            float until = Time.realtimeSinceStartup + 12;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < until) yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
        }
    }
}
