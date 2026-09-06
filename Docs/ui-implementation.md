# 完成イメージに合わせたUI実装

基準は `ui-images-unique-2026-09-06/完成イメージ.png`。提供された無文字フレームとフォント情報も参照しました。ステージの配置・ゲームルールは維持し、UIの構図、素材、文字、盤面の画面内配置を変更しています。

## 表示

- HUD: 木目と植物・クロス・ティーカップの装飾、ロゴ、脱出警告、スコア、時計、一時停止、右側レシピ、2×2材料カード、総合進捗、操作案内。
- タルト: 台・皿・スプーンと4種の独立したトッピングを重ねます。材料の未達分は薄いシルエット、収穫済みはカラー。各種類を3段階で表現し、最後のトッピングはその種類の必要数達成時だけ完成します。必要数0の種類は表示しません。
- タイトル、ステージ選択、成功・失敗リザルト、設定・一時停止を共通の配色・パネル・書体に統一。パッドのフォーカスとマウス操作を維持。
- 文字、数値、材料名、ステージ名、説明はUnity UI Text。完成画像にある「ダッシュ」は実際の操作と異なるため、下ボタン＝よだれ、左ボタン＝驚かすの案内を表示します。

## 素材と調整箇所

| 内容 | ファイル |
| --- | --- |
| 無文字の装飾背景 | `Assets/BetoBeto/Resources/UI/KitchenBackdrop.png` |
| 透明なタルト台・皿・スプーン | `Assets/BetoBeto/Resources/UI/TartBase.png` |
| 透明な4種フルーツの2×2アトラス | `Assets/BetoBeto/Resources/UI/FruitIcons.png` |
| 素材生成の記録 | `Docs/UI/art-prompts.md` |
| フォント出典とライセンス | `Docs/third-party.md` |
| 共通の1600×900設計座標と盤面領域 | `Assets/BetoBeto/UI/KitchenLayout.cs` |
| HUD配置・現在値の更新 | `Assets/BetoBeto/UI/GameHud.cs` |
| タルトの位置・段階表示 | `Assets/BetoBeto/UI/TartPreview.cs` |
| アウトゲームUI | `Assets/BetoBeto/UI/MenuScreen.cs` |

背景画像は文字を含みません。GameViewは中央の1600×900設計面に縦横比を保って収め、上下左右の余白で対応します。縦長画面でも配置が崩れない設計ですが、操作と読みやすさは横持ちを前提としています。

`KitchenBackdropGraphic` が盤面部分を描かずに3Dゲームを見せます。カメラの背後には `KitchenCameraBackdrop` の木目板を置き、画像の同じ範囲を表示して木枠との境界をつなぎます。浮遊スコアも共通の盤面領域から位置を計算します。

提供済み3DタルトのFBX・Prefabはそのまま残し、今回の平面HUDには完成イメージの画風に合わせて生成したイラストを使用しています。

## 検証手順

`UnityCLI command --project-path . run_tests --mode editor --filter BetoBeto.Tests.KitchenUiTests --filter_type testName --async_tests true` でUIテストを実行できます。PlayModeは `SceneFlowTests` と `GamepadPlayTests.PadMenusSelectSecondStageAdjustVolumePauseResumeAndReturn` がシーン遷移・音量・復帰・再挑戦を検証します。

異なる解像度を実描画する場合は `Temp/ui-capture-size.txt` に `1600 900` などを保存し、Pipelineの `eval_file --file Tools/UI/SetGameViewSize.cs` を実行します。撮影コマンドの幅・高さだけを変えて画像を引き伸ばす方法ではなく、GameView自体を変更します。

最終的な実行範囲と結果は `Docs/verification.md` に記録します。
