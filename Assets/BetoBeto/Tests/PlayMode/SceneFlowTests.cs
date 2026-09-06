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
        public IEnumerator StagePagesCanSelectEveryCatalogEntryAndReturnToPreviousPage()
        {
            var catalog = StageCatalog.Load();
            Assert.That(catalog.stages.Length, Is.GreaterThanOrEqualTo(5));
            for (int stage = 0; stage < catalog.stages.Length; stage++)
            {
                yield return SceneManager.LoadSceneAsync("StageSelect");
                yield return null;
                for (int page = 0; page < stage / 2; page++)
                {
                    Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                        .Single(b => b.name == "次へ →").onClick.Invoke();
                    yield return null;
                }
                var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Where(b => b.name == "このキッチンで作る →")
                    .OrderBy(b => b.transform.parent.GetSiblingIndex()).ToArray();
                Assert.That(buttons.Length, Is.EqualTo(Mathf.Min(2, catalog.stages.Length - stage / 2 * 2)));
                Assert.That(buttons[stage % 2].transform.parent.name, Is.EqualTo(catalog.stages[stage].title));
                buttons[stage % 2].onClick.Invoke();
                yield return Arrive(catalog.stages[stage].sceneName);
                Assert.That(GameFlow.SelectedStage, Is.EqualTo(stage));
                Assert.That(GameController.Instance.Board.Data.name,
                    Is.EqualTo(BetoBeto.Stage.StageData.Parse(catalog.stages[stage].layoutJson.text).name));
            }
            GameFlow.StageSelect();
            yield return Arrive("StageSelect");
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "次へ →").onClick.Invoke();
            yield return null;
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "← 前へ").onClick.Invoke();
            yield return null;
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Count(b => b.name == "← 前へ"), Is.Zero);
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.name == "このキッチンで作る →" && b.transform.parent.name == catalog.stages[0].title), Is.True);
        }

        [UnityTest]
        public IEnumerator PcQualityCanRepeatedlySelectAndLeaveBothStages()
        {
            int previousQuality = QualitySettings.GetQualityLevel();
            int pcQuality = System.Array.IndexOf(QualitySettings.names, "PC");
            Assert.That(pcQuality, Is.GreaterThanOrEqualTo(0));
            try
            {
                QualitySettings.SetQualityLevel(pcQuality, true);
                Debug.Log("Stage transition regression quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
                for (int pass = 0; pass < 3; pass++)
                {
                    yield return SceneManager.LoadSceneAsync("Title");
                    yield return null;
                    Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                        .Single(b => b.name == "ステージを選ぶ    →").onClick.Invoke();
                    yield return Arrive("StageSelect");
                    var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                        .Where(b => b.name == "このキッチンで作る →")
                        .OrderBy(b => b.transform.parent.GetSiblingIndex()).ToArray();
                    Assert.That(buttons.Length, Is.EqualTo(2));
                    int stage = pass % 2;
                    buttons[stage].onClick.Invoke();
                    yield return Arrive(stage == 0 ? "Kitchen" : "Kitchen02");
                    Assert.That(GameController.Instance, Is.Not.Null);
                    Assert.That(GameFlow.SelectedStage, Is.EqualTo(stage));
                    GameFlow.StageSelect();
                    yield return Arrive("StageSelect");
                    // Let rendering and object-change jobs run after scene asset unloading.
                    for (int frame = 0; frame < 10; frame++) yield return null;
                }
            }
            finally { QualitySettings.SetQualityLevel(previousQuality, true); }
        }

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
            var backdrop = Object.FindFirstObjectByType<KitchenBackdropGraphic>();
            Assert.That(backdrop, Is.Not.Null, "Every destination scene must create its illustrated UI.");
            Assert.That(backdrop.canvasRenderer, Is.Not.Null);
            Assert.That(backdrop.mainTexture.name, Is.EqualTo("KitchenBackdrop"),
                "Scene unloading must not release the new screen's background artwork.");
        }
    }
}
