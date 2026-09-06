using BetoBeto.Audio;
using BetoBeto.Core;
using BetoBeto.Stage;
using UnityEngine;
using BetoBeto.Player;
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
        Selectable primary, previousSelection;
        void Awake()
        {
            GameFlow.SceneReady();
            audioBus = GameAudio.GetOrCreate();
            // The illustrated menus replace the old 3D title diorama, including in letterboxed windows.
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.cullingMask = 0;
                camera.backgroundColor = Navy;
            }
            InitializeUi();
            BuildHeader();
            if (screen == MenuKind.Title) BuildTitle();
            else if (screen == MenuKind.StageSelect) BuildStageSelect();
            else BuildResult();
            FocusScope(root, primary);
        }
        void Update()
        {
            if (GameFlow.IsLoading) return;
            if (GamepadControls.CancelPressed)
            {
                if (options != null) CloseOptions();
                else if (screen == MenuKind.StageSelect) GameFlow.Title();
                else if (screen == MenuKind.Result) GameFlow.StageSelect();
            }
            if (GamepadControls.PausePressed) { if (options != null) CloseOptions(); else ShowOptions(); }
        }
        void BuildHeader()
        {
            BuildBackdrop();
            Label(root, "BETO BETO", new Vector2(200, -18), new Vector2(246, 50), 38, Ink, FontStyle.Bold);
            Label(root, "おばけのスイーツキッチン", new Vector2(223, -66), new Vector2(232, 24), 14, Ink);
            Label(root, "COOKIE KITCHEN", new Vector2(239, -91), new Vector2(193, 17), 10, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            string category = screen == MenuKind.Title ? "さあ、ひとくちの冒険へ" : screen == MenuKind.StageSelect ? "キッチンを選ぼう" : "きょうのキッチンだより";
            Label(root, category, new Vector2(515, -37), new Vector2(292, 36), 21, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "あつめて、つくる。しあわせなスイーツ", new Vector2(854, -39), new Vector2(420, 35), 17, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Button(root, "あそびかた・音量", new Vector2(1369, -36), new Vector2(192, 46), Navy, Color.white, ShowOptions, 17);
            Label(root, "左スティック / 十字キーで選択     ・     下ボタンで決定     ・     右ボタンで戻る", new Vector2(207, -843), new Vector2(780, 32), 15, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, "おいしい時間を、いっしょに。", new Vector2(1120, -862), new Vector2(408, 27), 15, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void RecipeShowcase(string title, string subtitle)
        {
            Label(root, "TODAY'S SPECIAL", new Vector2(1130, -150), new Vector2(391, 28), 18, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, title, new Vector2(1100, -194), new Vector2(456, 50), 29, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(root, subtitle, new Vector2(1099, -252), new Vector2(455, 48), 16, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            var preview = Dessert(root, new Vector2(1074, -333)); preview.ShowComplete();
            for (int i = 0; i < 4; i++)
            {
                Art(root, GameHud.FruitNames[i], KitchenArt.Fruit(i), new Vector2(1124 + i * 109, -699), new Vector2(69, 69));
                Label(root, GameHud.FruitNames[i], new Vector2(1111 + i * 109, -772), new Vector2(98, 24), 12, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
        }
        void BuildTitle()
        {
            RecipeShowcase("ごほうびフルーツタルト", "ちいさなおばけの、とびきり甘い大作戦。");
            var card = Box(root, "Welcome card", new Vector2(185, -198), new Vector2(790, 552), Cream);
            Border(card, Hex("ECC6AA"), 3);
            Label(card, "WELCOME TO THE COOKIE KITCHEN", new Vector2(49, -38), new Vector2(690, 27), 15, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "BETO BETO", new Vector2(32, -88), new Vector2(725, 98), 76, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "にげるフルーツ、まとめてツルン。", new Vector2(31, -218), new Vector2(728, 57), 31, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "わっ！と驚かせて、よだれでひと滑り。\nフルーツを集めて、すてきなスイーツをつくろう！", new Vector2(56, -300), new Vector2(678, 75), 20, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            primary = Button(card, "ステージを選ぶ    →", new Vector2(145, -405), new Vector2(500, 69), Pink, Color.white, GameFlow.StageSelect, 25).GetComponent<Button>();
            Label(card, "ゲームパッドをつないで、はじめよう", new Vector2(88, -502), new Vector2(614, 25), 15, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        void BuildStageSelect()
        {
            RecipeShowcase("今日は、何をつくる？", "キッチンごとに、材料も仕掛けもいろいろ。\nお気に入りのレシピを見つけよう。");
            Label(root, "CHOOSE YOUR KITCHEN", new Vector2(168, -161), new Vector2(650, 26), 14, Cream, FontStyle.Bold);
            Label(root, "今日は、どのキッチン？", new Vector2(167, -202), new Vector2(700, 50), 35, Cream, FontStyle.Bold);
            Button(root, "← タイトルへ", new Vector2(858, -204), new Vector2(148, 40), Navy, Cream, GameFlow.Title, 15);
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
                Label(pageRect, "ステージがありません。", new Vector2(180, -330), new Vector2(800, 90), 25, Cream); return;
            }
            for (int slot = 0; slot < 2; slot++)
            {
                int index = page * 2 + slot;
                if (index >= catalog.stages.Length) break;
                var entry = catalog.stages[index];
                var data = StageData.Parse(entry.layoutJson.text);
                var card = Box(pageRect, entry.title, new Vector2(167 + slot * 430, -278), new Vector2(408, 451), Cream);
                Border(card, Hex("E6BB99"), 2);
                Label(card, $"KITCHEN {index + 1:00}", new Vector2(24, -20), new Vector2(240, 24), 13, Muted, FontStyle.Bold);
                Label(card, data.width + " × " + data.height, new Vector2(274, -20), new Vector2(110, 24), 13, Muted, FontStyle.Normal, TextAnchor.MiddleRight);
                DrawMiniMap(card, data);
                var title = Label(card, entry.title, new Vector2(24, -235), new Vector2(360, 40), 25, Ink, FontStyle.Bold);
                title.resizeTextForBestFit = true; title.resizeTextMinSize = 19; title.resizeTextMaxSize = 25;
                Label(card, entry.description, new Vector2(25, -283), new Vector2(358, 49), 14, Muted);
                Label(card, $"材料 {data.recipe.Total}個   ・   脱出上限 {data.escapeLimit}個", new Vector2(25, -341), new Vector2(358, 25), 14, Muted);
                var play = Button(card, "このキッチンで作る →", new Vector2(24, -385), new Vector2(360, 47), Pink, Color.white, () => GameFlow.PlayStage(index), 18).GetComponent<Button>();
                if (slot == 0) primary = play;
            }
            int pages = (catalog.stages.Length + 1) / 2;
            if (pages > 1)
            {
                if (page > 0) Button(pageRect, "← 前へ", new Vector2(372, -755), new Vector2(120, 36), Navy, Cream, () => { page--; DrawStagePage(); });
                if (page + 1 < pages) Button(pageRect, "次へ →", new Vector2(690, -755), new Vector2(120, 36), Navy, Cream, () => { page++; DrawStagePage(); });
                Label(pageRect, $"{page + 1} / {pages}", new Vector2(520, -759), new Vector2(140, 26), 16, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            FocusScope(root, primary);
        }
        void DrawMiniMap(RectTransform card, StageData data)
        {
            var backing = Box(card, "Layout preview", new Vector2(24, -59), new Vector2(360, 159), Hex("244B5D"));
            float cell = Mathf.Min(340f / data.width, 139f / data.height);
            float left = (360 - cell * data.width) * .5f, top = (159 - cell * data.height) * .5f;
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                Color color = data.rows[y][x] switch
                {
                    '#' => Hex("E9BD7F"), 'C' => Hex("D8A15F"), 'J' => Hex("B783D3"), 'F' => Hex("ABE6F4"),
                    'P' => Mint, 'X' or 'H' or 'V' => Pink, 'G' => Cream, _ => Hex("6AA4B8")
                };
                var tile = Box(backing, "Cell", new Vector2(left + x * cell, -top - y * cell), new Vector2(cell - 2, cell - 2), color);
                tile.GetComponent<Image>().raycastTarget = false;
                char symbol = data.rows[y][x];
                if (GimmickRules.IsScone(symbol))
                {
                    var glyph = new GameObject("Scone slope", typeof(RectTransform)).AddComponent<SconeMapGraphic>();
                    glyph.rectTransform.SetParent(tile, false);
                    glyph.rectTransform.anchorMin = Vector2.zero; glyph.rectTransform.anchorMax = Vector2.one;
                    glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;
                    glyph.turns = symbol - '1'; glyph.color = Hex("FFE0A2"); glyph.raycastTarget = false;
                }
                else if (symbol == 'H' || symbol == 'V')
                    Label(tile, symbol == 'H' ? "↔" : "↕", Vector2.zero, new Vector2(cell - 2, cell - 2), Mathf.RoundToInt(cell * .7f), Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
        }
        void BuildResult()
        {
            var result = GameFlow.LastResult;
            RecipeShowcase(result == null ? "ごほうびフルーツタルト" : result.dessert,
                result != null && !result.won ? "もう一度、甘い大作戦に挑戦しよう！" : "おいしいごほうび、めしあがれ。");
            var card = Box(root, "Kitchen report", new Vector2(172, -188), new Vector2(834, 570), Cream);
            Border(card, Hex("E6BB99"), 3);
            Label(card, "KITCHEN REPORT", new Vector2(40, -30), new Vector2(754, 27), 15, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (result == null)
            {
                Label(card, "さあ、スイーツを作ろう。", new Vector2(40, -152), new Vector2(754, 60), 32, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                primary = Button(card, "ステージを選ぶ", new Vector2(137, -365), new Vector2(560, 65), Pink, Color.white, GameFlow.StageSelect, 22).GetComponent<Button>(); return;
            }
            Label(card, result.won ? "タルト、できあがり！" : "フルーツが逃げちゃった…", new Vector2(25, -80), new Vector2(784, 75), result.won ? 42 : 36, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, result.stageName, new Vector2(40, -162), new Vector2(754, 30), 17, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            string message = result.won ? result.dessert + "の完成！\nおいしい大作戦、成功です。" : $"集まった材料  {result.recipeCount} / {result.recipeTotal}\n驚かせて、逃げる先によだれを置いてみよう。";
            Label(card, message, new Vector2(40, -214), new Vector2(754, 68), 21, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Numeric(Label(card, $"SCORE  {result.score:N0}", new Vector2(30, -303), new Vector2(774, 55), 40, Pink, FontStyle.Bold, TextAnchor.MiddleCenter));
            Label(card, $"収穫 {result.harvested}個   /   脱出 {result.escaped}個   /   最大 {result.bestChain} CHAIN   /   {FormatTime(result.elapsed)}", new Vector2(30, -377), new Vector2(774, 35), 16, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            primary = Button(card, "もう一度つくる", new Vector2(48, -452), new Vector2(354, 65), Pink, Color.white, GameFlow.Retry, 22).GetComponent<Button>();
            Button(card, "ステージ選択へ", new Vector2(432, -452), new Vector2(354, 65), Hex("EFE1CF"), Ink, GameFlow.StageSelect, 22);
        }
        public void ShowOptions()
        {
            if (options != null) return;
            previousSelection = events.currentSelectedGameObject != null ? events.currentSelectedGameObject.GetComponent<Selectable>() : primary;
            options = Overlay("Menu options", .86f);
            var card = CenterCard(options.transform, "How to play", new Vector2(760, 790));
            Label(card, "あそびかた・音量設定", new Vector2(40, -35), new Vector2(680, 53), 29, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(card, "左スティック：移動 / 右スティック：向き / 下ボタン：よだれ\n左ボタン：驚かす。単押しは自分＋前方1マスを向いている方へ。\n長押しは1.5秒で半径6マス。周囲の敵はおばけから離れる。\nよだれで滑らせて連鎖！ 刃の1マス前は驚かすだけで突入。\nMENU：一時停止 / 右ボタン：戻る", new Vector2(43, -117), new Vector2(680, 131), 16, Ink);
            Label(card, GameHud.GimmickHelp, new Vector2(43, -267), new Vector2(680, 162), 14, Ink);
            Label(card, "必要な材料が全部そろえばクリア。逃げられすぎると失敗。", new Vector2(43, -436), new Vector2(680, 40), 16, Muted);
            SliderRow(card, "BGM", -503, audioBus.MusicVolume, audioBus.SetMusic);
            SliderRow(card, "効果音", -573, audioBus.EffectsVolume, audioBus.SetEffects);
            var close = Button(card, "閉じる", new Vector2(50, -654), new Vector2(660, 59), Pink, Color.white, CloseOptions, 22);
            FocusScope(options.transform, close.GetComponent<Button>());
        }
        void CloseOptions()
        {
            if (options == null) return;
            options.SetActive(false); Destroy(options); options = null; PlayerPrefs.Save();
            FocusScope(root, previousSelection);
        }
    }
}
