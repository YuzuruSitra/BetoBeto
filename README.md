# BetoBeto — おばけのスイーツキッチン

`Comcept/comcept.md` に沿った、Unity 6000.6.0f1 / URP のプレイ可能な初期プロトタイプです。
見た目とサウンドは差し替え前提の仮素材です。キャラクター・壁・床・罠・パイプ・アクション配置物は実際のPrefabになっています。

## 起動

1. Unity Hubからこのプロジェクトを開く。
2. `Assets/BetoBeto/Scenes/Title.unity` を開き、Play。
3. 「ステージを選ぶ」からキッチンを選択して開始。

主要ビルドターゲットは **WebGL** です。ゲームパッドを接続して使用します。

1. Unityメニューの `BetoBeto → Build → WebGL` で `Builds/WebGL` に出力。
2. プロジェクト直下で `python Tools/serve_webgl.py` を実行し、`http://localhost:8090` を開く。
3. 読み込み後「クリックしてはじめる」を押し、パッドのボタンを押して認識させる。

WebGLはHTTP配信が必要です。`index.html` のダブルクリック（`file://`）ではブラウザの制限により起動しないことがあります。ローカルでは上記サーバー、またはUnityの「Build And Run」を使います。サーバーは確認中起動したままにし、終了する場合はそのターミナルでCtrl+Cを押します。公開時は `Builds/WebGL` 全体を同じ構成で配信してください。Gzip圧縮とUnityの展開フォールバックを有効にしてあります。最初のクリックはブラウザの音声開始に必要です。旧Windowsビルドは今回の変更を含みません。

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

タイトル・ステージセレクト・リザルトは、それぞれ本編とは別のシーンです。遷移はSingleモードで行い、本編のオブジェクトをアウトゲームに残しません。音量設定を持つ音声オブジェクトのみシーン間で維持します。ゲーム中のMENU / STARTは一時停止と音量設定を開きます。

## 操作とルール

| 入力 | 動作 |
| --- | --- |
| 左スティック / 十字キー | 移動。ゴーストはクッキーを通過可能 |
| 右スティック | その場で向きを変更。移動中も向きを指定可能 |
| 右側の4ボタンの下（Xbox A / PlayStation ×） | 足元のマスによだれ。メニューでは決定 |
| 右側の4ボタンの左（Xbox X / PlayStation □） | 驚かす。単押しは自分のマス＋前方1マス、長押しは周囲。離すと発動 |
| MENU / START | 一時停止／復帰、メニューの設定を開閉 |
| 右側の4ボタンの右（Xbox B / PlayStation ○） | 戻る／一時停止から復帰 |
| メニューで左スティック / 十字キー | 選択を移動。音量バーでは左右で音量調整 |

ボタンの名称が違うパッドでも物理的な位置で操作します。顔と足元の矢印が向きを表し、停止中も向きを保持します。スティックの小さな揺れを無視し、斜め付近では向きが頻繁に切り替わるのを抑えています。紫色の枠は自分のマスと前方1マスを囲み、単押しで驚かす対象を示します。よだれは常に足元です。

ゲームパッドが未接続なら開始カウントとフルーツの進行を待機します。プレイ中の切断は自動で一時停止し、再接続後に復帰操作を行います。キーボード／マウスによる本編の移動と設置は廃止しました。画面上のメニューボタンはマウスでも押せます。

驚かすボタンを0.25秒未満で離すと、自分のマスと向いている前方1マスの敵が、プレイヤーが向いている方向へ進みます。自分と重なっている敵にも効きます。0.25秒以上ためると自分を中心とする範囲効果に変わり、半径1〜6マスへ段階的に広がります。1.5秒で最大チャージとなり、離すまで保持できます。範囲は各フルーツがいるマスの中心で判定する円形で、壁越しにも届きます。チャージ中も移動・向き調整・よだれ設置が可能です。床の紫色の枠／円と白いゲージ、HUDに現在の範囲が表示されます。

驚かされたフルーツは、単押しならプレイヤーが向いている方向、長押しならプレイヤーの実際の立ち位置から離れる方向を向き、約1.5秒間その方向へ逃げます。通常は壁やシュレッダーを避け、安全な道がなければ待機します。長押しではプレイヤーに近づく方向を避けます。途中で驚かされた場合も位置を飛ばさずに方向を変えます。滑走中に驚かすと滑走方向が変わり、連鎖数は維持されます。ポーズやパッド切断ではチャージが取り消されます。

例外として、驚かせた瞬間に逃げる方向の隣接1マスがシュレッダーなら、よだれなしでも回避せず飛び込み、収穫できます。単押し・長押しとも対象で、2マス以上先や横・後ろの刃には飛び込みません。メロンの耐久と無敵時間は維持されます。

よだれは初期設定で10秒で消え、再使用待ちは0.3秒です。驚かすには再使用待ちがなく、単押しの連続使用や発動直後の再チャージが可能です。壁・パイプ・罠・出口にはよだれを置けません。氷ブロックの生成は廃止しました。

イチゴは標準速度、ブルーベリーは高速で角を曲がります。オレンジはゆっくりと左右交互・下向きに進みます。袋小路では引き返し、同じ場所を回り続けないよう訪問回数を使います。メロンは最初のシュレッダー接触を耐えて跳ね返り、短い無敵時間の後にもう一度当てると収穫できます。

フルーツは通常移動中、シュレッダーを障害物として避けます。安全な道がなければ止まり、自分から刃には入りません。**よだれで滑らせてシュレッダーに突っ込ませるのが基本の収穫方法**です。刃の隣で驚かせた場合は、前述の直接突入も使えます。

よだれに触れたフルーツは、そのときの進行方向へ壁まで滑ります。ほかのフルーツに触れると巻き込み、同じ連鎖の全員が加速します。得点は1体につき100点×連鎖数。同じ刃での連続収穫は得点表示を合算します。壁への激突ではフルーツが潰れて跳ね返り、短く硬直します。壁への激突だけでは収穫されません。

滑走にはよだれの軌跡と飛沫、衝突には衝撃波・壁の揺れ・カメラの揺れ・短いヒットストップ・音程が上がる連鎖音を付けています。レシピの全種類が必要数そろうとクリア。違う種類の余剰分では代用できません。脱出上限に達すると失敗です。

## 独立ステージエディタ

`Tools/StageEditor/index.html` をChromeまたはEdgeで直接開いて使用できます。Unity、サーバー、npmのインストールは不要です。インターネットにも接続しません。

- 左ドラッグ：選択した配置物でペイント。右ドラッグ：床に戻す。
- ホイール：カーソル位置を中心に拡大縮小。
- 中ボタンドラッグ／Space＋左ドラッグ：パン。
- 1〜6：ブラシ選択。F：全体表示。
- Ctrl+Z：Undo。Ctrl+Shift+Z / Ctrl+Y：Redo。
- ステージ名、サイズ、必要フルーツ数、脱出上限、出現間隔、よだれの寿命を編集可能。
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

ゲーム画面のUIはラフ画像に準拠せず、レシピ進捗・脱出数・操作が読み取れる独自構成です。仮BGM・効果音はコードで合成し、音量はPlayerPrefsに別々に保存します。日本語フォントはM PLUS Rounded 1cを同梱しています。ライセンスと出典は `Docs/third-party.md` に記載しています。

滑走速度と壁での硬直は `Enemies/FruitAgent.cs`、揺れ・飛沫・ヒットストップの強さは `Presentation/GameFeedback.cs`、残像と潰れ方は `Presentation/FruitMotionVfx.cs` で調整できます。滑走は初速7.5マス/秒、巻き込み1体ごとに0.65マス/秒加算（加速は6体分まで）、壁の硬直は0.48秒です。

驚かす範囲・チャージ時間・逃走時間は `Player/ScareRules.cs`、チャージと範囲の表示は `Presentation/ScarePreview.cs`、よだれの再使用待ちは `Core/GameController.cs` の `DroolReuseSeconds` で調整できます。旧氷PrefabとJSONの `iceLifetime` は既存データとの互換性のため残し、プレイヤーの行動やタイトルの飾りには使用していません。

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
