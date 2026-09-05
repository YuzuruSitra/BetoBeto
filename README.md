# BetoBeto — おばけのスイーツキッチン

`Comcept/comcept.md` に沿った、Unity 6000.6.0f1 / URP のプレイ可能な初期プロトタイプです。
見た目とサウンドは差し替え前提の仮素材です。キャラクター・壁・床・罠・パイプ・アクション配置物は実際のPrefabになっています。

## 起動

1. Unity Hubからこのプロジェクトを開く。
2. `Assets/BetoBeto/Scenes/Title.unity` を開き、Play。
3. 「ステージを選ぶ」からキッチンを選択して開始。

Windows用のビルドは `Builds/Windows/BetoBeto.exe` です。`BetoBeto_Data` などの隣接ファイルとセットで利用してください。

UnityのPlay開始シーンにもTitleを設定しています。別のシーンを編集していても、Playするとタイトルから始まります。
`BetoBeto → Create Initial Prototype` は不足している初期アセット／シーンの生成と、Build Settingsの登録に利用できます。既存Prefabと既存シーンの見た目は上書きしません。

## シーン構成

```text
Title.unity
    ↓
StageSelect.unity
    ├─ Kitchen.unity     16×9 / パイプ2 / シュレッダー4
    └─ Kitchen02.unity   20×12 / パイプ3 / シュレッダー6
                ↓
           Result.unity
           ├─ 同じステージに再挑戦
           └─ ステージ選択に戻る
```

タイトル・ステージセレクト・リザルトは、それぞれ本編とは別のシーンです。遷移はSingleモードで行い、本編のオブジェクトをアウトゲームに残しません。音量設定を持つ音声オブジェクトのみシーン間で維持します。ゲーム中のEscは一時停止と音量設定を開きます。

## 操作とルール

| 入力 | 動作 |
| --- | --- |
| WASD / 矢印キー | 移動。ゴーストはクッキーと氷を通過可能 |
| 左クリック | マウスで指定した盤面のマスに氷を生成 |
| E | 前方のマスに氷を生成（マウスが盤面上ならそのマス） |
| Space / 右クリック | ゴーストの足元によだれを置く |
| Esc | 一時停止／復帰。アウトゲームでは戻る |
| Enter | タイトルからステージセレクトへ |
| R | リザルトから同じステージに再挑戦 |

氷は初期設定で5秒、よだれは10秒で消えます。再使用待ちは氷0.7秒、よだれ1.25秒です。壁・パイプ・罠・出口や、フルーツのいるマスには氷を置けません。

イチゴは標準速度、ブルーベリーは高速で角を曲がります。オレンジはゆっくりと左右交互・下向きに進みます。袋小路では引き返し、同じ場所を回り続けないよう訪問回数を使います。メロンは最初のシュレッダー接触を耐えて跳ね返り、短い無敵時間の後にもう一度当てると収穫できます。

フルーツは通常移動中、シュレッダーを障害物として避けます。安全な道がなければ止まり、自分から刃には入りません。**よだれで滑らせてシュレッダーに突っ込ませることが収穫の条件**です。

よだれに触れたフルーツは、そのときの進行方向へ壁まで滑ります。ほかのフルーツに触れると巻き込み、同じ連鎖の全員が加速します。得点は1体につき100点×連鎖数。同じ刃での連続収穫は得点表示を合算します。壁や氷への激突ではフルーツが潰れて跳ね返り、短く硬直します。壁への激突だけでは収穫されません。

滑走にはよだれの軌跡と飛沫、衝突には衝撃波・壁の揺れ・カメラの揺れ・短いヒットストップ・音程が上がる連鎖音を付けています。レシピの全種類が必要数そろうとクリア。違う種類の余剰分では代用できません。脱出上限に達すると失敗です。

## 独立ステージエディタ

`Tools/StageEditor/index.html` をChromeまたはEdgeで直接開いて使用できます。Unity、サーバー、npmのインストールは不要です。インターネットにも接続しません。

- 左ドラッグ：選択した配置物でペイント。右ドラッグ：床に戻す。
- ホイール：カーソル位置を中心に拡大縮小。
- 中ボタンドラッグ／Space＋左ドラッグ：パン。
- 1〜6：ブラシ選択。F：全体表示。
- Ctrl+Z：Undo。Ctrl+Shift+Z / Ctrl+Y：Redo。
- ステージ名、サイズ、必要フルーツ数、脱出上限、出現間隔、氷／よだれの寿命を編集可能。
- 端末内への自動保存、JSONの読み込み／書き出し、配置数と到達可能性の検証。

### Unityへの取り込み

1. エディタで検証エラーを解消して「JSONを書き出す」。
2. Unityの `BetoBeto → Stage JSON Importer` を開く。
3. JSONを指定して「検証」。
4. 「新しいシーンを生成して保存」を選び、未使用のシーン名で保存する。
5. カタログとBuild Settingsに自動登録され、ステージセレクトから選択可能になる。

生成される `Stage / Tiles` と `Stage / Placements` の中身はPrefabインスタンスです。見た目の調整は各Prefab、位置調整はシーン上のインスタンスで行えます。実行時はシーンに存在する配置物から当たり判定を作り直すため、Unity上で移動した壁や罠も反映されます。配置は1マス単位で行ってください。エディタのプレビューは元JSONを表示するため、Unityだけで配置を変更した場合のプレビューは更新されません。

ステージの一覧・表示名・説明は `Assets/BetoBeto/Resources/StageCatalog.asset` で編集できます。

## 開発の分担

| フォルダ | 担当する内容 |
| --- | --- |
| `Assets/BetoBeto/Core` | ゲーム進行、勝敗、得点、シーン遷移、ステージカタログ |
| `Assets/BetoBeto/Player` | ゴーストの移動と入力 |
| `Assets/BetoBeto/Enemies` | フルーツAI、滑走、連鎖、メロン耐久 |
| `Assets/BetoBeto/Stage` | JSON仕様、グリッド座標、配置物と盤面 |
| `Assets/BetoBeto/UI` | 共通UI、ゲームHUD、各アウトゲーム画面 |
| `Assets/BetoBeto/Audio` | 独立したBGM／効果音音量、仮サウンド |
| `Assets/BetoBeto/Presentation` | 滑走の軌跡・飛沫・連鎖表示・壁の反動・ヒットストップ・収穫エフェクト |
| `Assets/BetoBeto/Prefabs` / `Art` | 差し替え可能なモデル、マテリアル、メッシュ |
| `Assets/BetoBeto/Editor` | Prefab生成、JSONからシーン生成 |
| `Assets/BetoBeto/Stages` / `Scenes` | ステージデータと各シーン |
| `Assets/BetoBeto/Tests` | EditMode／PlayModeテスト |
| `Tools/StageEditor` | Unityに依存しない配置ツール |

ゲーム画面のUIはラフ画像に準拠せず、レシピ進捗・脱出数・操作が読み取れる独自構成です。仮BGM・効果音はコードで合成し、音量はPlayerPrefsに別々に保存します。日本語フォントはWindowsのYu Gothic UI / Meiryoなどを利用します。他OS向けに配布する際は、日本語フォントを正式に同梱する工程が必要です。

滑走速度と壁での硬直は `Enemies/FruitAgent.cs`、揺れ・飛沫・ヒットストップの強さは `Presentation/GameFeedback.cs`、残像と潰れ方は `Presentation/FruitMotionVfx.cs` で調整できます。滑走は初速7.5マス/秒、巻き込み1体ごとに0.65マス/秒加算（加速は6体分まで）、壁の硬直は0.48秒です。

## 検証コマンド

同梱のUnity Pipelineとインストール済みUnityCLIを使用できます。

```powershell
$unityCli = "$env:LOCALAPPDATA/Unity/bin/unity.exe"
& $unityCli command --project-path . recompile
& $unityCli command --project-path . run_tests --mode editor --filter BetoBeto.Tests.EditMode --filter_type assembly
& $unityCli command --project-path . eval --code 'UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = null;'
& $unityCli command --project-path . run_tests --mode playmode --filter BetoBeto.Tests.PlayMode --filter_type assembly --async_tests true
& $unityCli command --project-path . test_status
node Tools/StageEditor/stage-model.test.cjs
```

JSONの詳細は `Docs/stage-format.md` を参照してください。
実行したテストと画面確認の範囲は `Docs/verification.md` にまとめています。

PlayModeテスト中はタイトル固定を一時解除します。テスト完了後、`BetoBeto → Create Initial Prototype` で通常の起動設定に戻せます。Unity 6000.6 / Pipelineの組み合わせで `test_status` がrunningのままになる場合、Unityが出力する `AppData/LocalLow/BetoBeto Kitchen/BetoBeto/TestResults.xml` にテスト結果が保存されます。
