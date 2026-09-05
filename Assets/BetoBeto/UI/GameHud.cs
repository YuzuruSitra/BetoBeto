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
        void BuildHud()
        {
            var header = Stretch(root, "Header", new Vector2(0, .885f), Vector2.one, new Vector2(22, 0), new Vector2(-22, -20), Cream);
            Label(header, "BETO BETO", new Vector2(25, -14), new Vector2(270, 40), 31, Ink, FontStyle.Bold);
            Label(header, "おばけのスイーツキッチン", new Vector2(27, -51), new Vector2(310, 24), 14, Muted);
            stageText = Label(header, "", new Vector2(385, -17), new Vector2(460, 27), 20, Ink, FontStyle.Bold);
            Label(header, "COOKIE KITCHEN", new Vector2(387, -49), new Vector2(410, 22), 12, Muted);
            var pause = Button(header, "一時停止  II", new Vector2(-182, -24), new Vector2(156, 40), Ink, Cream, TogglePause);
            pause.anchorMin = pause.anchorMax = new Vector2(1, 1);

            var sidebar = Stretch(root, "Recipe", new Vector2(.783f, .132f), new Vector2(1, .86f), new Vector2(0, 0), new Vector2(-25, 0), Cream);
            Label(sidebar, "TODAY'S RECIPE", new Vector2(24, -22), new Vector2(260, 23), 12, Muted, FontStyle.Bold);
            dessertText = Label(sidebar, game.Board.Data.dessert, new Vector2(24, -51), new Vector2(285, 38), 25, Ink, FontStyle.Bold);
            dessertText.resizeTextForBestFit = true; dessertText.resizeTextMinSize = 15; dessertText.resizeTextMaxSize = 25;
            stageText.resizeTextForBestFit = true; stageText.resizeTextMinSize = 13; stageText.resizeTextMaxSize = 20;
            Label(sidebar, "逃げるフルーツを集めよう", new Vector2(25, -94), new Vector2(280, 24), 14, Muted);
            for (int i = 0; i < 4; i++)
            {
                float y = -143 - i * 79;
                var badge = Box(sidebar, "Fruit badge", new Vector2(23, y), new Vector2(46, 48), FruitColors[i]);
                Label(badge, new[] { "S", "B", "O", "M" }[i], new Vector2(0, -3), new Vector2(46, 37), 25, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                Label(sidebar, FruitNames[i], new Vector2(83, y), new Vector2(180, 24), 16, Ink, FontStyle.Bold);
                recipeCounts[i] = Label(sidebar, "0 / 0", new Vector2(-84, y), new Vector2(62, 26), 16, Ink, FontStyle.Bold, TextAnchor.MiddleRight);
                recipeCounts[i].rectTransform.anchorMin = recipeCounts[i].rectTransform.anchorMax = new Vector2(1, 1);
                Label(sidebar, FruitHints[i], new Vector2(83, y - 26), new Vector2(200, 19), 11, Muted);
                var track = Box(sidebar, "Ingredient track", new Vector2(83, y - 51), new Vector2(196, 5), Hex("E9E2D6"));
                recipeFills[i] = Fill(track, FruitColors[i]);
            }
            countText = Label(sidebar, "できあがり  0%", new Vector2(25, -476), new Vector2(250, 30), 18, Ink, FontStyle.Bold);
            var progress = Box(sidebar, "Recipe progress", new Vector2(24, -517), new Vector2(266, 12), Hex("E9E2D6"));
            recipeFill = Fill(progress, Mint);
            Label(sidebar, "お菓子をつないで、ピンクの刃へ！", new Vector2(25, -551), new Vector2(285, 26), 13, Muted);

            var stats = Stretch(root, "Round status", new Vector2(.02f, .84f), new Vector2(.765f, .88f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            escapeText = Label(stats, "", new Vector2(12, 0), new Vector2(390, 28), 18, Cream, FontStyle.Bold);
            timeText = Label(stats, "00:00", new Vector2(-200, 0), new Vector2(170, 28), 18, Cream, FontStyle.Bold, TextAnchor.MiddleRight);
            timeText.rectTransform.anchorMin = timeText.rectTransform.anchorMax = new Vector2(1, 1);
            scoreText = Label(stats, "", new Vector2(470, 0), new Vector2(350, 28), 16, Cream);
            noticeText = Label(root, "", Vector2.zero, new Vector2(760, 38), 19, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            noticeText.rectTransform.anchorMin = noticeText.rectTransform.anchorMax = new Vector2(.385f, .13f);
            noticeText.rectTransform.pivot = new Vector2(.5f, 0);
            countdown = Label(root, "", Vector2.zero, new Vector2(500, 140), 72, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            countdown.rectTransform.anchorMin = countdown.rectTransform.anchorMax = new Vector2(.39f, .51f);
            countdown.rectTransform.pivot = new Vector2(.5f, .5f);

            var footer = Stretch(root, "Controls", Vector2.zero, new Vector2(1, .106f), new Vector2(23, 19), new Vector2(-23, 0), Cream);
            Label(footer, "左スティック / 十字キー  移動", new Vector2(23, -13), new Vector2(390, 26), 17, Ink, FontStyle.Bold);
            Label(footer, "右スティックで向き調整  ·  MENUで一時停止", new Vector2(24, -43), new Vector2(410, 23), 13, Muted);
            scareText = Label(footer, "", new Vector2(440, -10), new Vector2(550, 26), 17, Ink, FontStyle.Bold);
            Label(footer, "左ボタン (X / □)  ·  単押し：自分＋前方 / 長押し：最大半径6マス", new Vector2(441, -36), new Vector2(560, 23), 13, Muted);
            scareCharge = Fill(Box(footer, "Scare charge track", new Vector2(441, -65), new Vector2(530, 5), Hex("E9E2D6")), Hex("AD82D6"));
            droolText = Label(footer, "", new Vector2(1020, -13), new Vector2(400, 26), 17, Ink, FontStyle.Bold);
            Label(footer, "下ボタン  ·  足元に置いて連鎖！", new Vector2(1021, -43), new Vector2(430, 23), 13, Muted);
            feedbackLayer = new GameObject("Floating feedback", typeof(RectTransform)).GetComponent<RectTransform>();
            feedbackLayer.SetParent(root, false);
            feedbackLayer.anchorMin = Vector2.zero; feedbackLayer.anchorMax = Vector2.one;
            feedbackLayer.offsetMin = feedbackLayer.offsetMax = Vector2.zero;
        }
        public FloatingWord FloatMessage(Vector3 worldPoint, string message, Color color, int size, string channel = null)
        {
            if (channel != null && messages.TryGetValue(channel, out var previous) && previous != null)
            {
                previous.gameObject.SetActive(false); Destroy(previous.gameObject);
            }
            Vector3 screen = game.GameCamera.WorldToScreenPoint(worldPoint);
            Vector2 anchor = new Vector2(Mathf.Clamp(screen.x / Screen.width, .095f, .69f), Mathf.Clamp(screen.y / Screen.height, .24f, .76f));
            var label = Label(feedbackLayer, message, Vector2.zero, new Vector2(360, 70), size, color, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = anchor;
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
            Label(card, "よだれで滑らせて連鎖！ 普段のフルーツは刃を避ける。\n驚かす単押し：自分＋前方1マスの敵を、向いている方向へ。\n長押し：1.5秒で半径6マス。おばけから離れる向きに変える。\n刃の1マス前は、驚かすだけでも突入できる。\n\n" + GimmickHelp, new Vector2(46, -104), new Vector2(675, 268), 16, Ink);
            Label(card, "左スティック：移動 / 右スティック：向き / 下ボタン：よだれ\n左ボタン：驚かす（離すと発動） / 音量：上下・左右で調整", new Vector2(46, -382), new Vector2(675, 48), 15, Muted);
            SliderRow(card, "BGM", -450, game.Audio.MusicVolume, game.Audio.SetMusic);
            SliderRow(card, "効果音", -520, game.Audio.EffectsVolume, game.Audio.SetEffects);
            var resume = Button(card, pausedByModal ? "キッチンに戻る" : "閉じる", new Vector2(50, -613), new Vector2(660, 55), Pink, Color.white, () => CloseModal(true), 21);
            if (pausedByModal) Button(card, "ステージ選択へ", new Vector2(50, -685), new Vector2(660, 38), Hex("E9EEE9"), Ink, GameFlow.StageSelect, 16);
            else Label(card, "音量は自動で保存されます", new Vector2(50, -694), new Vector2(660, 24), 13, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            FocusScope(modal.transform, resume.GetComponent<Button>());
        }
        public const string GimmickHelp = "ゼリー：反転して滑り続ける。\nクッキー：滑走かオレンジで割る。復帰は初期20秒。\n移動シュレッダー：往復する刃に、タイミングを合わせる。\nスコーン：歩いて乗っても転向。滑走20回で破壊、復帰は初期5秒。\nフリーザー：しばらく減速。滑走と連鎖は続く。\n歩行：壁で右→左→右。盤外で脱出。パイプは逆走不可。";
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
            escapeText.text = $"脱出  {session.Escaped:00} / {session.EscapeLimit:00}";
            escapeText.color = session.Escaped >= session.EscapeLimit - 3 ? Hex("FFC2C5") : Cream;
            timeText.text = FormatTime(session.Elapsed);
            scoreText.text = $"SCORE  {session.Score:N0}";
            for (int i = 0; i < 4; i++)
            {
                int goal = session.Recipe.For((FruitKind)i);
                recipeCounts[i].text = $"{Mathf.Min(goal, session.Harvested[i])} / {goal}";
                recipeFills[i].rectTransform.anchorMax = new Vector2(goal == 0 ? 1 : Mathf.Clamp01(session.Harvested[i] / (float)goal), 1);
            }
            float fraction = session.RecipeCount / (float)session.Recipe.Total;
            countText.text = $"できあがり  {Mathf.RoundToInt(fraction * 100)}%";
            recipeFill.rectTransform.anchorMax = new Vector2(fraction, 1);
            var player = game.Player;
            bool charging = player != null && player.IsCharging;
            scareText.text = charging
                ? player.ChargeSeconds < ScareRules.TapSeconds ? "BOO!   自分＋前方1マス · 向いている方向へ"
                    : $"BOO!   {(player.Charge01 >= 1 ? "MAX! " : "チャージ ")}半径{player.ScareRadius}マス · 離して発動"
                : "BOO!   驚かして向きを変える";
            scareCharge.rectTransform.anchorMax = new Vector2(charging ? player.Charge01 : 0, 1);
            droolText.text = game.DroolCooldown > 0 ? $"DROOL   あと {game.DroolCooldown:0.0} 秒" : "DROOL   よだれで滑らせる";
            noticeText.text = !GamepadControls.Ready ? "ゲームパッドを接続して、いずれかのボタンを押してください" : session.State == GameState.Playing && Time.unscaledTime < game.NoticeUntil ? game.Notice : "";
            countdown.text = GamepadControls.Ready && session.State == GameState.Playing && game.Countdown > 0 ? Mathf.CeilToInt(game.Countdown).ToString() : "";
        }
    }
}
