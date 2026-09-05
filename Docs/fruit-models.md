# フルーツモデル

2026-09-06に、4種類のフルーツのプレースホルダを `G:\Blender\CleanAssets\BETOBETO_Fruits\Unity\BETOBETO_Fruits.unitypackage` のモデルに置き換えました。

- 元データは `Assets/BetoBeto/Art/Characters/Fruits`。FBX・テクスチャ・マテリアル・表情スクリプト・Animator Controllerを同梱しています。元素材の利用条件を引き継ぎます。
- ゲーム用Prefabは従来の `Assets/BetoBeto/Prefabs/Characters/{Strawberry,Blueberry,Orange,Melon}.prefab`。GUIDとFruitAgentを維持し、Visualの中身を差し替えています。
- FBXは前方+Z、高さ約2.1mのため、モデル子オブジェクトをY軸180度回転、0.44倍にしています。Visualの倍率はブルーベリー0.64、イチゴ0.94、オレンジ1.04、メロン1.14。ブルーベリー＜イチゴ＜オレンジ＜メロンの順に大きくなります。
- マテリアルはURP/Litへ変換し、BaseColor・Normal・MetallicSmoothnessを設定しています。
- `FruitModelVisual` が `FruitAgent.Sliding` を参照します。通常はExpression_Normal＋Run、滑走中はExpression_Surprised。滑り始めるたびにSlide_Supine（仰向け）またはSlide_Prone（うつ伏せ）を等確率で選び、その滑走中は姿勢を維持します。壁などで滑走が終わると通常顔とRunに戻ります。手足は常時表示します。
- 驚かされた後は3秒間、Expression_Surprised＋ScaredRun（Motion=3）を使います。手を上げて交互に振り、膝を上げて走る0.6秒のループです。もう一度驚かされると3秒に延長し、滑走が始まれば滑走モーションを優先します。時間は `ScareRules.FleeSeconds` で管理します。
- ポーズ・ヒットストップ・硬直中はAnimatorも停止します。Root Motionは無効で、移動と当たり判定は既存ゲーム処理が担当します。
- 仰向けの滑走開始時にY軸回転を毎秒120〜360度、左右ランダムで選びます。その滑走中は同じ角速度を維持し、うつ伏せでは回転しません。見た目のVisualだけを回すので移動方向は変わりません。ポーズ・ヒットストップ中は停止し、滑走終了時は元の向きに戻ります。
- `FruitMotionVfx` の旧プレースホルダ用の傾きは、新モデルでは使用しません。滑走の姿勢は付属アニメーションが担当し、既存の伸縮・残像・飛沫は継続します。

再適用はUnityメニューの **BetoBeto → Apply Fruit Models**。ゲーム用4PrefabのVisualの子を再生成するので、モデル配置の調整は `FruitModelImporter.cs` に反映してください。

驚き走りのFBXは `Animations/{Kind}_ScaredRun.fbx`、編集用Blenderファイルは `Source~/{Kind}_Rig.blend` です。どちらもこのFruitsフォルダ内で管理します。`Source~` はUnityのインポート対象外なので、Blenderファイルの自動変換は発生しません。書き出しスクリプトは `Tools/Blender/export_fruit_scared_run.py`。指定されたBlender 5.2.0で各ソースを開いて実行します。元のメッシュFBXを上書きせず、既存の走り・滑走を保持します。

書き出し後は **BetoBeto → Update Fruit Scared Run Animations** で4つのControllerへ反映できます。このメニューはゲーム用Prefabの配置やサイズを変更しません。

## 確認

Unity 6000.6.0f1 / UnityCLI + Unity Pipelineで実施。

驚き走り追加時はコンパイルとアセット取り込みを確認。ユーザーの希望により追加のテストコードは作成せず、ゲーム内の動作確認はユーザーが行います。以下のPlayMode結果は驚き走り追加前の記録です。

- コンパイル：エラー0。
- PlayMode `FruitModelPlayTests`：2/2成功。4種類を順に検証し、3つのスキンメッシュ、URPマテリアル、通常→滑走→ポーズ→壁停止→再滑走の表情・Motion切り替えを確認。滑走モーションが仰向け・うつ伏せのいずれかになり、同じ滑走中とポーズ中は選択を維持することも確認済みです。仰向けだけY軸回転すること、移動方向が変わらないこと、回転のポーズ・再開・滑走終了時の向きの復帰も確認しています。
- サイズ：Runの同一フレームで表示メッシュを比較し、幅・高さ・奥行きがブルーベリー＜イチゴ＜オレンジ＜メロンの順になることを確認。
- EditMode `AssetCatalogUsesRealPrefabAssets`：1/1成功。ゲームのPrefab参照を確認。
- Unity描画：`Logs/QA/FruitModels-preview.png`。上段は通常＋Run、下段は驚き＋Slide_Supine。左からイチゴ、ブルーベリー、オレンジ、メロン。
- PlayMode結果：`Logs/QA/FruitModels-PlayMode-results.xml`。

WebGL出力は今回再ビルドしていません。
