using System.Collections;
using BetoBeto.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BetoBeto.Tests
{
    public sealed class TitleOpeningPlayTests
    {
        // Every other menu test drives the title directly, so the movie is only armed here.
        [SetUp] public void ArmTheOpening() { TitleOpening.Enabled = true; TitleOpening.Rewind(); }
        [TearDown] public void DisarmTheOpening() { TitleOpening.Enabled = false; }

        [UnityTest]
        public IEnumerator OpeningHoldsTheTitleUntilItIsSkippedAndDoesNotPlayAgain()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var opening = Object.FindFirstObjectByType<TitleOpening>();
            Assert.That(opening, Is.Not.Null, "タイトルはオープニング映像から始まる。");
            Assert.That(TitleOpening.IsPlaying, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null,
                "映像が出ている間はメニューにフォーカスを渡さない。");
            var canvas = opening.GetComponentInChildren<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.GreaterThan(0), "映像はタイトルUIより手前に出す。");
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null,
                "背後のタイトルボタンがクリックされないよう入力を受け止める。");

            opening.Skip();
            for (float waited = 0; TitleOpening.IsPlaying && waited < 5; waited += Time.unscaledDeltaTime)
                yield return null;
            yield return null;
            Assert.That(TitleOpening.IsPlaying, Is.False);
            Assert.That(Object.FindFirstObjectByType<TitleOpening>(), Is.Null, "映像は再生後に片付ける。");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ステージを選ぶ    →"),
                "映像が終わったらタイトルの操作を戻す。");

            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            Assert.That(Object.FindFirstObjectByType<TitleOpening>(), Is.Null,
                "一度見た映像は、タイトルへ戻っても繰り返さない。");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ステージを選ぶ    →"));
        }
    }
}
