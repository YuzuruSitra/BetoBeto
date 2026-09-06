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
            StretchArt(root, "Escape HUD", KitchenArt.HudEscape, new Vector2(430, -8), new Vector2(450, 140));
            StretchArt(root, "Score HUD", KitchenArt.HudScore, new Vector2(900, -2), new Vector2(285, 176));
            StretchArt(root, "Time HUD", KitchenArt.HudTime, new Vector2(1184, -12), new Vector2(230, 150));
            StretchArt(root, "Pause HUD", KitchenArt.HudPause, new Vector2(1410, -19), new Vector2(184, 102));
            // The scene's real gingham towel backs the recipe; only the sewn title label is UI art.
            StretchArt(root, "Recipe cloth label", KitchenArt.HudRecipeLabel, new Vector2(1190, -177), new Vector2(358, 112));
            Art(root, "Chef ghost", KitchenArt.HudChef, new Vector2(1480, -135), new Vector2(118, 118));
            // These source textures include transparent margins. Size their painted interiors to the text.
            StretchArt(root, "Controls HUD", KitchenArt.HudControls, new Vector2(365, -758), new Vector2(835, 170));
            stageText = Label(root, "", new Vector2(155, -24), new Vector2(278, 30), 16, Cream,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "脱出", new Vector2(570, -49), new Vector2(88, 42), 29, Color.white, FontStyle.Bold);
            var danger = Box(root, "Escape counter", new Vector2(660, -36), new Vector2(186, 48), Color.clear);
            escapeText = Label(danger, "", Vector2.zero, new Vector2(186, 48), 29, Color.white,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            escapeLimitText = Label(root, "", new Vector2(570, -85), new Vector2(278, 22), 13,
                Hex("A82A55"), FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "SCORE", new Vector2(1020, -58), new Vector2(120, 24), 17, Ink,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            scoreText = Label(root, "0", new Vector2(1018, -82), new Vector2(124, 43), 36, Ink,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "TIME", new Vector2(1286, -63), new Vector2(91, 20), 14, Navy,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            timeText = Label(root, "00:00", new Vector2(1267, -81), new Vector2(130, 35), 27, Navy,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            Button(root, "一時停止  II", new Vector2(1421, -36), new Vector2(164, 60), Color.clear,
                Color.white, TogglePause, 19);

            dessertText = Label(root, game.Board.Data.dessert, new Vector2(1230, -200), new Vector2(268, 32), 24,
                Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            dessertText.resizeTextForBestFit = true; dessertText.resizeTextMinSize = 16; dessertText.resizeTextMaxSize = 24;
            Label(root, "をつくろう！", new Vector2(1230, -234), new Vector2(268, 27), 22,
                Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            tart = Dessert(root, new Vector2(1154, -278), .86f);
            for (int i = 0; i < 4; i++)
            {
                float x = 1158 + i % 2 * 216, y = -558 - i / 2 * 104;
                var card = Box(root, "Ingredient card " + FruitNames[i], new Vector2(x, y),
                    new Vector2(212, 100), Color.white);
                var cardImage = card.GetComponent<Image>();
                cardImage.sprite = KitchenArt.HudIngredientCard;
                cardImage.type = Image.Type.Simple;
                cardImage.color = Color.Lerp(Color.white, FruitColors[i], .08f);
                Art(card, FruitNames[i] + " icon", KitchenArt.Fruit(i), new Vector2(12, -18), new Vector2(65, 65));
                Label(card, FruitNames[i], new Vector2(78, -25), new Vector2(117, 24), 15, Ink, FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                recipeCounts[i] = Label(card, "0 / 0", new Vector2(78, -48), new Vector2(117, 30), 23, Ink,
                    FontStyle.Bold, TextAnchor.MiddleCenter);
                Numeric(recipeCounts[i]);
                recipeFills[i] = Fill(Box(card, "Ingredient track", new Vector2(86, -79),
                    new Vector2(97, 4), new Color(1, 1, 1, .35f)), FruitColors[i]);
            }
            StretchArt(root, "Recipe progress badge", KitchenArt.HudProgressBadge, new Vector2(1444, -433),
                new Vector2(151, 124));
            Label(root, "できあがり", new Vector2(1462, -467), new Vector2(115, 22), 14, Color.white,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            countText = Label(root, "0%", new Vector2(1462, -488), new Vector2(115, 34), 28, Color.white,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            recipeFill = Fill(Box(root, "Recipe progress", new Vector2(1183, -775),
                new Vector2(386, 5), new Color(1, 1, 1, .35f)), Pink);

            noticeText = Label(root, "", new Vector2(145, -748), new Vector2(950, 31), 16, Cream,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            countdown = Label(root, "", new Vector2(290, -371), new Vector2(580, 150), 88, Cream,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            Border(countdown.rectTransform, Navy, 3);
            ControlBadge("L", new Vector2(390, -812), Hex("A47D6A"));
            ControlBadge("A", new Vector2(544, -812), Hex("4C9BE9"));
            ControlBadge("X", new Vector2(807, -812), Hex("EC6097"));
            Label(root, "移動", new Vector2(431, -811), new Vector2(90, 23), 16, Navy, FontStyle.Bold);
            Label(root, "左スティック", new Vector2(431, -837), new Vector2(95, 18), 11, Navy);
            Box(root, "Control divider", new Vector2(530, -808), new Vector2(1, 39), Hex("E6C2A8"));
            droolText = Label(root, "よだれ", new Vector2(584, -811), new Vector2(207, 24), 15, Navy, FontStyle.Bold);
            Label(root, "下ボタン（A / ×）で足元に置く", new Vector2(547, -839), new Vector2(243, 18), 11, Navy);
            Box(root, "Control divider", new Vector2(794, -808), new Vector2(1, 39), Hex("E6C2A8"));
            scareText = Label(root, "", new Vector2(847, -811), new Vector2(331, 24), 15, Navy, FontStyle.Bold);
            Label(root, "左ボタン（X / □） 単押し：前方 / 長押し：周囲", new Vector2(810, -839),
                new Vector2(373, 18), 11, Navy);
            scareCharge = Fill(Box(root, "Scare charge track", new Vector2(816, -861),
                new Vector2(347, 3), Hex("EDDECE")), Hex("AD82D6"));
            feedbackLayer = new GameObject("Floating feedback", typeof(RectTransform)).GetComponent<RectTransform>();
            feedbackLayer.SetParent(root, false);
            feedbackLayer.anchorMin = Vector2.zero; feedbackLayer.anchorMax = Vector2.one;
            feedbackLayer.offsetMin = feedbackLayer.offsetMax = Vector2.zero;
            foreach (var value in new[] { escapeText, scoreText, timeText, countText, countdown }) Numeric(value);
            scoreText.resizeTextForBestFit = true; scoreText.resizeTextMinSize = 18; scoreText.resizeTextMaxSize = 36;
            stageText.resizeTextForBestFit = true; stageText.resizeTextMinSize = 11; stageText.resizeTextMaxSize = 16;
        }
        void ControlBadge(string text, Vector2 position, Color color)
        {
            var badge = Box(root, "Control " + text, position, new Vector2(32, 27), color);
            Label(badge, text, Vector2.zero, new Vector2(32, 27), 19, Color.white,
                FontStyle.Bold, TextAnchor.MiddleCenter);
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
            escapeText.color = session.Escaped >= session.EscapeLimit - 3 ? Hex("FFF3A8") : Color.white;
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
