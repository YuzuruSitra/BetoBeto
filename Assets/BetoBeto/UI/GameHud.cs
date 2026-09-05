using System;
using System.Collections.Generic;
using BetoBeto.Core;
using BetoBeto.Presentation;
using BetoBeto.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    /// <summary>Presentation and menus only. Gameplay state lives in GameSession.</summary>
    public sealed class GameHud : UiView
    {
        public static readonly string[] FruitNames = { "イチゴ", "ブルーベリー", "オレンジ", "メロン" };
        static readonly string[] FruitHints = { "右→左と曲がる", "速く交互に曲がる", "クッキーを削る", "2回当てると収穫" };
        GameController game;
        GameObject modal;
        RectTransform feedbackLayer;
        readonly Dictionary<string, FloatingWord> messages = new Dictionary<string, FloatingWord>();
        Text escapeText, timeText, scoreText, noticeText, countText, stageText, scareText, droolText, countdown;
        Text dessertText;
        readonly Text[] recipeCounts = new Text[4];
        readonly Image[] recipeFills = new Image[4];
        Image recipeFill, scareCharge;
        bool pausedByModal;
        GameState stateBeforeModal;
        public bool ModalOpen => modal != null && modal.activeSelf;

        public void Initialize(GameController controller)
        {
            game = controller;
            InitializeUi();
            BuildHud();
            DisableNavigation();
        }
        TartPreview tart;
        Text escapeLimitText;
        void BuildHud()
        {
            BuildBackdrop(true);
            Label(root, "BETO BETO", new Vector2(200, -18), new Vector2(246, 50), 38, Ink, FontStyle.Bold);
            Label(root, "おばけのスイーツキッチン", new Vector2(223, -66), new Vector2(232, 24), 14, Ink);
            stageText = Label(root, "", new Vector2(218, -90), new Vector2(226, 18), 10, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(root, "！", new Vector2(511, -25), new Vector2(60, 58), 43, Hex("D93858"), FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "脱出", new Vector2(582, -27), new Vector2(65, 42), 29, Hex("BD304D"), FontStyle.Bold);
            var danger = Box(root, "Escape counter", new Vector2(650, -23), new Vector2(151, 50), Hex("DE4561"));
            escapeText = Label(danger, "", Vector2.zero, new Vector2(151, 50), 29, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            escapeLimitText = Label(root, "", new Vector2(551, -76), new Vector2(249, 23), 14, Hex("CB4260"), FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(root, "★", new Vector2(866, -29), new Vector2(54, 51), 41, Hex("FFB249"), FontStyle.Bold);
            Label(root, "SCORE", new Vector2(930, -26), new Vector2(118, 23), 15, Navy, FontStyle.Bold);
            scoreText = Label(root, "0", new Vector2(930, -46), new Vector2(116, 47), 31, Ink, FontStyle.Bold);
            Box(root, "Score divider", new Vector2(1049, -31), new Vector2(2, 49), Hex("F0D7C5"));
            Label(root, "◷", new Vector2(1080, -28), new Vector2(58, 53), 41, Hex("4E90D0"), FontStyle.Bold);
            Label(root, "TIME", new Vector2(1143, -26), new Vector2(106, 23), 15, Navy, FontStyle.Bold);
            timeText = Label(root, "00:00", new Vector2(1143, -46), new Vector2(128, 47), 31, Ink, FontStyle.Bold);
            Button(root, "一時停止  II", new Vector2(1369, -36), new Vector2(192, 46), Navy, Color.white, TogglePause, 21);

            Label(root, "TODAY'S RECIPE", new Vector2(1130, -133), new Vector2(391, 28), 18, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            dessertText = Label(root, game.Board.Data.dessert, new Vector2(1093, -174), new Vector2(468, 45), 31, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            dessertText.resizeTextForBestFit = true; dessertText.resizeTextMinSize = 22; dessertText.resizeTextMaxSize = 31;
            Label(root, "フルーツを集めて、すてきなタルトをつくろう！", new Vector2(1097, -222), new Vector2(466, 25), 14, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            tart = Dessert(root, new Vector2(1074, -253));
            Label(root, "Yummy!", new Vector2(1466, -279), new Vector2(92, 31), 20, Pink, FontStyle.Bold);
            for (int i = 0; i < 4; i++)
            {
                float x = 1092 + i % 2 * 233, y = -580 - i / 2 * 74;
                var card = Box(root, "Ingredient card " + FruitNames[i], new Vector2(x, y), new Vector2(221, 65), Hex("FFF9F0"));
                Border(card, Hex("F1D9C4"), 1.5f);
                var badge = Box(card, "Fruit badge", new Vector2(7, -6), new Vector2(54, 52), Color.Lerp(FruitColors[i], Cream, .78f));
                Art(badge, FruitNames[i] + " icon", KitchenArt.Fruit(i), new Vector2(-5, 6), new Vector2(63, 63));
                Label(card, FruitNames[i], new Vector2(72, -8), new Vector2(144, 22), 15, Ink, FontStyle.Bold);
                recipeCounts[i] = Label(card, "0 / 0", new Vector2(81, -30), new Vector2(120, 28), 22, Ink, FontStyle.Bold, TextAnchor.MiddleRight);
                recipeFills[i] = Fill(Box(card, "Ingredient track", new Vector2(73, -59), new Vector2(132, 3), Hex("F0E4D5")), FruitColors[i]);
            }
            var progress = Box(root, "Recipe progress card", new Vector2(1091, -739), new Vector2(282, 85), Hex("FFF9F0"));
            Border(progress, Hex("F1D9C4"), 1.5f);
            Label(progress, "できあがり", new Vector2(16, -13), new Vector2(123, 33), 18, Ink, FontStyle.Bold);
            countText = Label(progress, "0%", new Vector2(143, -2), new Vector2(121, 49), 36, Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            recipeFill = Fill(Box(progress, "Recipe progress", new Vector2(13, -55), new Vector2(256, 20), Hex("EFDDCF")), Pink);
            Label(root, "おいしいタルトを\nつくろう！", new Vector2(1391, -754), new Vector2(162, 58), 19, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "あつめて、つくる。しあわせなスイーツ", new Vector2(1091, -862), new Vector2(465, 27), 15, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            var sign = Label(root, "Sweets\nmake\neveryone\nhappy!", new Vector2(9, -248), new Vector2(108, 155), 18, Hex("B7855E"), FontStyle.Bold, TextAnchor.MiddleCenter);
            sign.rectTransform.localEulerAngles = new Vector3(0, 0, 9);

            noticeText = Label(root, "", new Vector2(144, -772), new Vector2(864, 28), 16, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            countdown = Label(root, "", new Vector2(290, -371), new Vector2(580, 150), 88, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            Border(countdown.rectTransform, Navy, 3);
            Label(root, "移動", new Vector2(215, -842), new Vector2(75, 23), 16, Navy, FontStyle.Bold);
            Label(root, "左スティック", new Vector2(207, -864), new Vector2(113, 18), 10, Navy);
            Box(root, "Control divider", new Vector2(326, -841), new Vector2(1, 35), Hex("E6C2A8"));
            droolText = Label(root, "よだれ", new Vector2(346, -841), new Vector2(216, 24), 15, Navy, FontStyle.Bold);
            Label(root, "下ボタン（A / ×）で足元に置く", new Vector2(345, -865), new Vector2(245, 18), 10, Navy);
            Box(root, "Control divider", new Vector2(578, -841), new Vector2(1, 35), Hex("E6C2A8"));
            scareText = Label(root, "", new Vector2(599, -841), new Vector2(382, 24), 15, Navy, FontStyle.Bold);
            Label(root, "左ボタン（X / □） 単押し：前方 / 長押し：周囲", new Vector2(599, -865), new Vector2(400, 18), 10, Navy);
            scareCharge = Fill(Box(root, "Scare charge track", new Vector2(600, -885), new Vector2(364, 3), Hex("EDDECE")), Hex("AD82D6"));
            feedbackLayer = new GameObject("Floating feedback", typeof(RectTransform)).GetComponent<RectTransform>();
            feedbackLayer.SetParent(root, false);
            feedbackLayer.anchorMin = Vector2.zero; feedbackLayer.anchorMax = Vector2.one;
            feedbackLayer.offsetMin = feedbackLayer.offsetMax = Vector2.zero;
            foreach (var value in new[] { escapeText, scoreText, timeText, countText, countdown }) Numeric(value);
            scoreText.resizeTextForBestFit = true; scoreText.resizeTextMinSize = 18; scoreText.resizeTextMaxSize = 31;
            stageText.resizeTextForBestFit = true; stageText.resizeTextMinSize = 8; stageText.resizeTextMaxSize = 10;
        }
        public FloatingWord FloatMessage(Vector3 worldPoint, string message, Color color, int size, string channel = null)
        {
            if (channel != null && messages.TryGetValue(channel, out var previous) && previous != null)
            {
                previous.gameObject.SetActive(false); Destroy(previous.gameObject);
            }
            Vector2 position = KitchenLayout.FeedbackPosition(game.GameCamera.WorldToViewportPoint(worldPoint));
            var label = Label(feedbackLayer, message, Vector2.zero, new Vector2(360, 70), size, color, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero;
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.pivot = new Vector2(.5f, .5f);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.1f, .21f, .27f, .9f); outline.effectDistance = new Vector2(2, -2);
            var floating = label.gameObject.AddComponent<FloatingWord>();
            if (channel != null) messages[channel] = floating;
            return floating;
        }
        public void ScoreMessage(Vector3 point, int score)
        {
            string channel = "score:" + game.Board.Data.Cell(point);
            if (messages.TryGetValue(channel, out var previous) && previous != null) score += previous.ScoreValue;
            FloatMessage(point, "+" + score, new Color(1, .93f, .64f), 35, channel).ScoreValue = score;
        }
        public void ClearFeedback()
        {
            messages.Clear();
            for (int i = feedbackLayer.childCount - 1; i >= 0; i--)
            {
                feedbackLayer.GetChild(i).gameObject.SetActive(false);
                Destroy(feedbackLayer.GetChild(i).gameObject);
            }
        }
        public void TogglePause()
        {
            if (ModalOpen) { CloseModal(true); return; }
            ShowOptions();
        }
        public void ShowOptions()
        {
            if (ModalOpen) return;
            stateBeforeModal = game.Session.State;
            pausedByModal = stateBeforeModal == GameState.Playing;
            if (pausedByModal) game.Session.State = GameState.Paused;
            game.Player?.CancelScare();
            modal = Overlay("Options", .85f);
            var card = CenterCard(modal.transform, "Options card", new Vector2(760, 790));
            Label(card, pausedByModal ? "ひとやすみ" : "あそびかた・音量設定", new Vector2(35, -29), new Vector2(690, 51), 30, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "よだれで滑らせて連鎖！ 普段のフルーツは刃を避ける。\n驚かす単押し：自分＋前方1マスの敵を、向いている方向へ。\n長押し：1.5秒で半径6マス。おばけから離れる向きに変える。\n刃の1マス前は、驚かすだけでも突入できる。\n\n" + GimmickHelp, new Vector2(46, -104), new Vector2(675, 292), 16, Ink);
            Label(card, "左スティック：移動 / 右スティック：向き / 下ボタン：よだれ\n左ボタン：驚かす（離すと発動） / 音量：上下・左右で調整", new Vector2(46, -406), new Vector2(675, 48), 15, Muted);
            SliderRow(card, "BGM", -474, game.Audio.MusicVolume, game.Audio.SetMusic);
            SliderRow(card, "効果音", -544, game.Audio.EffectsVolume, game.Audio.SetEffects);
            var resume = Button(card, pausedByModal ? "キッチンに戻る" : "閉じる", new Vector2(50, -613), new Vector2(660, 55), Pink, Color.white, () => CloseModal(true), 21);
            if (pausedByModal) Button(card, "ステージ選択へ", new Vector2(50, -685), new Vector2(660, 38), Hex("E9EEE9"), Ink, GameFlow.StageSelect, 16);
            else Label(card, "音量は自動で保存されます", new Vector2(50, -694), new Vector2(660, 24), 13, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            FocusScope(modal.transform, resume.GetComponent<Button>());
        }
        public const string GimmickHelp = "ゼリー：反転して滑り続ける。\nクッキー：滑走かオレンジで割る。復帰は初期20秒。\n移動シュレッダー：往復する刃に、タイミングを合わせる。\nスコーン：斜面で転向、側面は壁。滑走20回で破壊、初期5秒で復帰。\nチョコフォンデュ：しばらく減速。滑走と連鎖は続く。\n氷：水を驚かすと壁に。初期5秒、衝突が途切れて0.5秒で水に。\n歩行：壁で右→左→右。盤外で脱出。パイプは逆走不可。";
        void CloseModal(bool restore)
        {
            if (modal != null) { modal.SetActive(false); Destroy(modal); modal = null; }
            if (restore && pausedByModal && game.Session.State == GameState.Paused) game.Session.State = stateBeforeModal;
            pausedByModal = false;
            DisableNavigation();
            GamepadControls.SuppressActionsUntilRelease();
            PlayerPrefs.Save();
        }
        public void Refresh()
        {
            var session = game.Session;
            stageText.text = game.Board.Data.name;
            dessertText.text = game.Board.Data.dessert;
            escapeText.text = $"{session.Escaped:00} / {session.EscapeLimit:00}";
            escapeLimitText.text = $"{session.EscapeLimit}でゲームオーバー";
            escapeText.color = Color.white;
            escapeText.transform.parent.GetComponent<Image>().color = session.Escaped >= session.EscapeLimit - 3 ? Hex("BD2346") : Hex("DE4561");
            timeText.text = FormatTime(session.Elapsed);
            scoreText.text = $"{session.Score:N0}";
            for (int i = 0; i < 4; i++)
            {
                int goal = session.Recipe.For((FruitKind)i);
                recipeCounts[i].text = $"{Mathf.Min(goal, session.Harvested[i])} / {goal}";
                recipeFills[i].rectTransform.anchorMax = new Vector2(goal == 0 ? 1 : Mathf.Clamp01(session.Harvested[i] / (float)goal), 1);
            }
            float fraction = session.Recipe.Total == 0 ? 1 : Mathf.Clamp01(session.RecipeCount / (float)session.Recipe.Total);
            tart.Refresh(session);
            countText.text = $"{Mathf.RoundToInt(fraction * 100)}%";
            recipeFill.rectTransform.anchorMax = new Vector2(fraction, 1);
            var player = game.Player;
            bool charging = player != null && player.IsCharging;
            scareText.text = charging
                ? player.ChargeSeconds < ScareRules.TapSeconds ? "BOO!  前方へ驚かす"
                    : $"BOO!  {(player.Charge01 >= 1 ? "MAX! " : "")}半径{player.ScareRadius}マス ・ 離して発動"
                : "BOO!   驚かして向きを変える";
            scareCharge.rectTransform.anchorMax = new Vector2(charging ? player.Charge01 : 0, 1);
            droolText.text = game.DroolCooldown > 0 ? $"よだれ  あと {game.DroolCooldown:0.0} 秒" : "よだれで滑らせる";
            noticeText.text = !GamepadControls.Ready ? "ゲームパッドを接続して、いずれかのボタンを押してください" : session.State == GameState.Playing && Time.unscaledTime < game.NoticeUntil ? game.Notice : "";
            countdown.text = GamepadControls.Ready && session.State == GameState.Playing && game.Countdown > 0 ? Mathf.CeilToInt(game.Countdown).ToString() : "";
        }
    }
}
