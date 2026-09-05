using BetoBeto.Audio;
using BetoBeto.Core;
using BetoBeto.Stage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    public enum MenuKind { Title, StageSelect, Result }

    /// <summary>Used only by the three out-of-game scenes; never creates a gameplay session.</summary>
    public sealed class MenuScreen : UiView
    {
        public MenuKind screen;
        GameAudio audioBus;
        GameObject options;
        GameObject selectionPage;
        int page;
        void Awake()
        {
            GameFlow.SceneReady();
            audioBus = GameAudio.GetOrCreate();
            InitializeUi();
            BuildHeader();
            if (screen == MenuKind.Title) BuildTitle();
            else if (screen == MenuKind.StageSelect) BuildStageSelect();
            else BuildResult();
        }
        void Update()
        {
            if (Keyboard.current == null || GameFlow.IsLoading) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (options != null) CloseOptions();
                else if (screen == MenuKind.StageSelect) GameFlow.Title();
                else if (screen == MenuKind.Result) GameFlow.StageSelect();
            }
            if (options != null) return;
            if (screen == MenuKind.Title && Keyboard.current.enterKey.wasPressedThisFrame) GameFlow.StageSelect();
            if (screen == MenuKind.Result && Keyboard.current.rKey.wasPressedThisFrame && GameFlow.LastResult != null) GameFlow.Retry();
        }
        void BuildHeader()
        {
            var header = Stretch(root, "Menu header", new Vector2(0, .895f), Vector2.one, new Vector2(24, 0), new Vector2(-24, -22), Cream);
            Label(header, "BETO BETO", new Vector2(24, -14), new Vector2(260, 43), 29, Ink, FontStyle.Bold);
            Label(header, "おばけのスイーツキッチン", new Vector2(315, -24), new Vector2(400, 25), 16, Muted);
            var settings = Button(header, "あそびかた・音量", new Vector2(-207, -13), new Vector2(183, 44), Hex("E9EEE9"), Ink, ShowOptions, 16);
            settings.anchorMin = settings.anchorMax = new Vector2(1, 1);
            var footer = Label(root, "A LITTLE GHOST. A BIG APPETITE.                                      BETO BETO  /  PROTOTYPE 01", new Vector2(45, 38), new Vector2(1400, 25), 11, Hex("A6C1C7"));
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = Vector2.zero;
        }
        void BuildTitle()
        {
            Label(root, "WELCOME TO THE COOKIE KITCHEN", new Vector2(90, -170), new Vector2(640, 30), 14, Mint, FontStyle.Bold);
            Label(root, "BETO\nBETO", new Vector2(80, -224), new Vector2(600, 210), 90, Cream, FontStyle.Bold);
            Label(root, "にげるフルーツ、\nまとめてツルン。", new Vector2(90, -457), new Vector2(640, 97), 33, Cream, FontStyle.Bold);
            Label(root, "氷で道を変えて、よだれでひと滑り。\n小さなおばけの、とびきり甘い大作戦。", new Vector2(92, -576), new Vector2(580, 69), 18, Hex("B7D0D3"));
            Button(root, "ステージを選ぶ    →", new Vector2(90, -688), new Vector2(480, 67), Pink, Color.white, GameFlow.StageSelect, 23);
            Label(root, "ENTER でステージ選択へ", new Vector2(92, -776), new Vector2(480, 25), 12, Hex("A7C0C7"));
        }
        void BuildStageSelect()
        {
            Label(root, "CHOOSE YOUR KITCHEN", new Vector2(80, -130), new Vector2(750, 26), 13, Mint, FontStyle.Bold);
            Label(root, "今日は、どのキッチン？", new Vector2(78, -170), new Vector2(1000, 58), 36, Cream, FontStyle.Bold);
            var back = Button(root, "← タイトルへ", new Vector2(-260, -167), new Vector2(180, 43), Hex("315568"), Cream, GameFlow.Title, 15);
            back.anchorMin = back.anchorMax = Vector2.one;
            DrawStagePage();
        }
        void DrawStagePage()
        {
            if (selectionPage != null) { selectionPage.SetActive(false); Destroy(selectionPage); }
            selectionPage = new GameObject("Stage cards", typeof(RectTransform));
            var pageRect = selectionPage.GetComponent<RectTransform>(); pageRect.SetParent(root, false);
            pageRect.anchorMin = Vector2.zero; pageRect.anchorMax = Vector2.one; pageRect.offsetMin = pageRect.offsetMax = Vector2.zero;
            var catalog = StageCatalog.Load();
            if (catalog == null || catalog.stages == null || catalog.stages.Length == 0)
            {
                Label(pageRect, "ステージがありません。StageCatalogに登録してください。", new Vector2(90, -320), new Vector2(1100, 90), 25, Cream); return;
            }
            for (int slot = 0; slot < 2; slot++)
            {
                int index = page * 2 + slot;
                if (index >= catalog.stages.Length) break;
                var entry = catalog.stages[index];
                var data = StageData.Parse(entry.layoutJson.text);
                var card = Box(pageRect, entry.title, new Vector2(80 + slot * 770, -261), new Vector2(670, 532), Cream);
                Label(card, $"KITCHEN {index + 1:00}", new Vector2(30, -24), new Vector2(300, 24), 13, Muted, FontStyle.Bold);
                Label(card, data.width + " × " + data.height, new Vector2(490, -25), new Vector2(145, 24), 13, Muted, FontStyle.Normal, TextAnchor.MiddleRight);
                DrawMiniMap(card, data);
                Label(card, entry.title, new Vector2(30, -316), new Vector2(610, 39), 26, Ink, FontStyle.Bold);
                Label(card, entry.description, new Vector2(32, -361), new Vector2(605, 50), 15, Muted);
                Label(card, $"材料 {data.recipe.Total}個   ·   脱出上限 {data.escapeLimit}個", new Vector2(32, -441), new Vector2(330, 25), 14, Muted);
                Button(card, "このキッチンで作る →", new Vector2(360, -434), new Vector2(278, 58), Pink, Color.white, () => GameFlow.PlayStage(index), 18);
            }
            int pages = (catalog.stages.Length + 1) / 2;
            if (pages > 1)
            {
                if (page > 0) Button(pageRect, "← 前へ", new Vector2(660, -817), new Vector2(120, 36), Hex("315568"), Cream, () => { page--; DrawStagePage(); });
                if (page + 1 < pages) Button(pageRect, "次へ →", new Vector2(820, -817), new Vector2(120, 36), Hex("315568"), Cream, () => { page++; DrawStagePage(); });
            }
        }
        void DrawMiniMap(RectTransform card, StageData data)
        {
            var backing = Box(card, "Layout preview", new Vector2(28, -65), new Vector2(614, 230), Hex("244B5D"));
            float cell = Mathf.Min(580f / data.width, 206f / data.height);
            float left = (614 - cell * data.width) * .5f, top = (230 - cell * data.height) * .5f;
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                Color color = data.rows[y][x] switch
                {
                    '#' => Hex("E9BD7F"), 'P' => Mint, 'X' => Pink, 'E' => Ink, 'G' => Cream, _ => Hex("6AA4B8")
                };
                var tile = Box(backing, "Cell", new Vector2(left + x * cell, -top - y * cell), new Vector2(cell - 2, cell - 2), color);
                tile.GetComponent<Image>().raycastTarget = false;
            }
        }
        void BuildResult()
        {
            var result = GameFlow.LastResult;
            var card = CenterCard(root, "Kitchen report", new Vector2(800, 654));
            card.anchoredPosition = new Vector2(0, -25);
            Label(card, "KITCHEN REPORT", new Vector2(40, -33), new Vector2(720, 27), 14, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (result == null)
            {
                Label(card, "さあ、スイーツを作ろう。", new Vector2(40, -152), new Vector2(720, 60), 32, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                Button(card, "ステージを選ぶ", new Vector2(120, -365), new Vector2(560, 65), Pink, Color.white, GameFlow.StageSelect, 22); return;
            }
            Label(card, result.won ? "タルト、できあがり！" : "フルーツが逃げちゃった…", new Vector2(25, -90), new Vector2(750, 75), result.won ? 42 : 36, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, result.stageName, new Vector2(40, -172), new Vector2(720, 30), 16, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            string message = result.won ? result.dessert + "の完成！\nおいしい大作戦、成功です。" : $"集まった材料  {result.recipeCount} / {result.recipeTotal}\n氷で道を曲げて、よだれを置いてみよう。";
            Label(card, message, new Vector2(50, -231), new Vector2(700, 80), 23, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(card, $"SCORE  {result.score:N0}", new Vector2(30, -341), new Vector2(740, 51), 36, Pink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, $"収穫 {result.harvested}個   /   脱出 {result.escaped}個   /   最大 {result.bestChain} CHAIN   /   {FormatTime(result.elapsed)}", new Vector2(35, -414), new Vector2(730, 35), 16, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            Button(card, "もう一度つくる", new Vector2(55, -487), new Vector2(332, 64), Pink, Color.white, GameFlow.Retry, 22);
            Button(card, "ステージ選択へ", new Vector2(413, -487), new Vector2(332, 64), Hex("E5ECE7"), Ink, GameFlow.StageSelect, 22);
            Label(card, "R で再挑戦     /     ESC でステージ選択", new Vector2(40, -584), new Vector2(720, 27), 12, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        public void ShowOptions()
        {
            if (options != null) return;
            options = Overlay("Menu options", .86f);
            var card = CenterCard(options.transform, "How to play", new Vector2(760, 690));
            Label(card, "あそびかた・音量設定", new Vector2(40, -35), new Vector2(680, 53), 29, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "WASD / 矢印キー：移動。クッキーの壁をすり抜けられる。\n左クリック / E：氷を生成。Space / 右クリック：足元によだれ。\nフルーツは普段シュレッダーを避ける。よだれで滑らせよう！\nほかのフルーツを巻き込むほど加速＆得点アップ。\n滑った勢いでピンクの刃へ！ メロンは2回当てよう。", new Vector2(43, -130), new Vector2(680, 180), 17, Ink);
            Label(card, "必要な材料が全部そろえばクリア。\n一定数のフルーツに逃げられるとゲームオーバー。", new Vector2(43, -325), new Vector2(680, 65), 17, Muted);
            SliderRow(card, "BGM", -417, audioBus.MusicVolume, audioBus.SetMusic);
            SliderRow(card, "効果音", -487, audioBus.EffectsVolume, audioBus.SetEffects);
            Button(card, "閉じる", new Vector2(50, -580), new Vector2(660, 59), Pink, Color.white, CloseOptions, 22);
        }
        void CloseOptions() { if (options != null) { options.SetActive(false); Destroy(options); options = null; PlayerPrefs.Save(); } }
    }
}
