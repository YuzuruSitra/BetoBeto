# お化けモデルとモーション

元データは `G:\Blender\CleanAssets\BETOBETO_CuteGhost\Blender\CuteGhost_Rig.blend`。
指定の `G:\Blender\5.2.0\blender-5.2.0-windows-x64\blender.exe` をMCP経由で使用して書き出しました。

ゲーム用Prefabは `Assets/BetoBeto/Prefabs/Characters/ApronGhost.prefab` のままです。GUID、GhostController、シーンからの参照を維持し、Visual以下をリグ付きモデルに置き換えています。元の浮遊処理は外し、FBXのアニメーションで動かします。モデルの前方を既存のActorFacingに合わせて180度回転し、床から0.15m浮かせています。Visualのスケールを1.5倍にしており、モデル再適用時にもこの倍率を維持します。

## 動作

見下ろし画面に合わせてモデルを画面奥へ15度傾けています。向きを変えてもカメラの奥側への傾きを維持します。`Visual > GhostModelVisual > View Tilt Degrees` で角度を変更できます。よだれは傾きを反映した口の位置から発生します。

| 状況 | 動作 |
|---|---|
| 停止〜移動 | `Speed` 0〜1でIdle／Moveをブレンド。盤面端で停止した場合も含め、実際の移動量を最高速度で正規化します。 |
| よだれボタン押下中 | 設置の成否によらず`YODAREStart`→`Yodare`へ。押下中ずっとカメラを向いてループします。離すと開始モーションの途中でも移動ブレンドへ戻ります。 |
| 怖がらせるボタンを離したとき | `Spook`を0.5秒間再生して移動ブレンドへ戻ります。よだれボタン押下中はよだれを優先し、怖がらせるチャージを解除します。 |
| ポーズ／ヒットストップ／ゲーム終了 | Animator、Spookの残り時間、よだれエフェクトのシミュレーションを停止します。 |

Spookの長さはPrefabの `Visual > GhostModelVisual > Spook Seconds` で変更できます。Spook自体は元の4フレームの動きを指定時間だけ繰り返します。よだれの設置タイミング・クールダウンや怖がらせる判定は従来のゲーム処理を使用します。長押しによって水たまりを自動連続設置する機能は追加していません。

## 体とエプロンのマテリアル

Blenderの `CuteGhost_PBR` は体用の `Assets/BetoBeto/Art/Characters/CuteGhost/Materials/GhostBody.mat`、`Apron` はエプロン用の従来の `Materials/Apron.mat` に対応します。インポート時に名前で割り当てるため、スロット順を変更して再出力しても対応を維持します。この2つのマテリアル名を維持してください。

エプロンは従来のURP/LitによるPBRです。体の `BetoBeto/Ghost Body Rim` は元の色・顔・法線・PBRマップを使い、視線に対して斜めになる輪郭へ青いHDR発光を加えます。発光自体はBloomが無効なカメラでも表示されます。`Materials/GhostBody.mat` の **Rim emission color / strength / falloff** で色・強度・幅を調整でき、これらの調整値はモデル再適用時も維持します。WebGL向けの通常の頂点・フラグメントシェーダで描画します。

## 透明なよだれエフェクト

`Visual > GhostDroolVfx` がアニメーション済みの `Yodare` ボーンを発生位置として使います。開始アニメーションの口が開く区間から流れ始め、押下中は10本の液体の流れと毎秒160個のしずくを発生させ、重力で地面へ落とします。接地時は飛沫の輪が広がります。離すと新規発生を止め、既に落下中のしずくは着地して消えます。

初期化時にLineRendererを94個（流れ10、しずく64、飛沫20）確保して再利用します。毎フレームの生成・破棄、物理Raycast、Compute Shaderは使いません。流量・太さ・しずく上限・床の高さは同コンポーネントから調整できます。床は現在のステージと同じ水平面です。

`Art/Shaders/DroolLiquid.shader` はURPのOpaque Textureをサンプリングして背景を歪ませ、丸い液体断面の疑似法線によるスペキュラ反射とリムライトを加える透明シェーダです。薄いミント色の透過光を主体とし、GrabPassや画面空間レイトレーシングを使わないWebGL向け構成です。WebGLが選ぶ `Assets/Settings/Mobile_RPAsset.asset` のOpaque Textureを有効にしています。屈折対象は不透明描画済みの背景で、他の透明物は含みません。

質感は `Assets/BetoBeto/Art/Characters/CuteGhost/Materials/DroolLiquid.mat` のOpacity、Refraction、Rim、Specularで調整できます。カメラ向き固定はVisualにだけ適用し、離すと通常の向きへ戻します。移動・グリッド上の向き・怖がらせる判定はモデルの回転から独立しています。

WebGLではキャラクター用PBRマップを展開する際のメモリーを抑えるため、お化け・フルーツのテクスチャに最大1024pxのWebGL専用オーバーライドを設定しています。元画像とPC用インポート設定は維持されます。`BetoBeto → Build → Configure WebGL` またはWebGLビルドメニューから再設定できます。

## Blend更新後の再出力

1. Blenderで編集したBlendを保存します。
2. Unityプロジェクトのルートから次を実行します。

   ```powershell
   .\Tools\Blender\Export-CuteGhost.ps1
   ```

3. UnityがFBXを再読み込みします。既存のAnimator Controllerから同名クリップへの参照は維持され、変更したモーションとフレーム範囲が反映されます。

Blender内で直接実行する場合は、Blendと同じフォルダの `export_cute_ghost.py` をテキストエディターで開き、CuteGhost_Rigシーンで「スクリプト実行」を押してください。この方法では現在開いているデータを出力します。スクリプトは元Blendを上書き保存せず、アクション・フレーム・ポーズ・選択を復元します。管理用の同一スクリプトをUnity側の `Tools/Blender` にも同梱しています。

別の配置で使う場合、PowerShellの `-BlendFile` / `-Blender` 引数でパスを変更できます。Python単体実行の出力先は環境変数 `BETOBETO_UNITY_PROJECT` で変更できます。

## 出力とUnity設定

- `Assets/BetoBeto/Art/Characters/CuteGhost/Models/CuteGhost_Rig.fbx`：メッシュ、11ボーン、Smileシェイプキー。
- `Animations/*.fbx`：Idle、Move、YODAREStart、Yodare、Spook。元の`Idle_Float`はIdle、`YodareStart`はYODAREStartへ対応します。
- クリップごとに全ボーンを毎フレームベイクし、Smileドライバーの結果もFBXへ保存します。Apply Modifiersは無効です。
- `GhostFbxPostprocessor`がGeneric、ループ設定、クリップ名、最新のフレーム範囲を自動設定します。Root Motionは無効です。
- `Textures`と`Materials/Apron.mat`：エプロン用URP/Lit、BaseColor、Normal、Metallic＋Smoothness。SmoothnessはRoughnessから反転生成します。体の`Materials/GhostBody.mat`も同じマップを参照し、青いリム発光を加えます。
- `export-report.json`：書き出した元ファイル、Blender実行ファイル、各アクションの範囲と表情値。

初回セットアップやマテリアルの再生成は **BetoBeto → Apply Ghost Model**。この操作はVisual以下と生成Animator Controllerを作り直すため、モデル配置や遷移の恒久的な変更は `GhostModelImporter.cs` に反映してください。通常のモーション更新では再実行不要です。テクスチャのRoughness／Metallicを編集した場合はこのメニューで合成マップを再生成してください。

## 検証

体・エプロン分離後の検証（2026-09-06）：2つのサブメッシュへの別マテリアル割り当て、1.5倍スケール、5クリップの表情カーブを確認しました。PlayModeの4件はすべて成功（`Logs/QA/GhostMaterials-PlayMode-results.xml`）。描画プレビューは `Logs/QA/GhostMaterials-preview.png` です。

**BetoBeto → Verify and Preview Ghost Model** で5クリップの表情カーブを検査し、`Logs/QA/GhostModels-preview.png` と `GhostModels-import.txt` を出力できます。画像の左からIdle、Move、YODAREStart、Yodare、Spookです。

PlayModeテスト `BetoBeto.Tests.GhostModelPlayTests` は速度ブレンド、盤面端の停止、よだれ開始・ループ・表情・ポーズ・復帰、短押しの中断、Spookの時間経過による復帰、カメラ向き固定、骨の参照、しずくの発生・接地・停止、プールの再利用を確認します。

2026-09-06、Unity 6000.6.0f1で現在の4件すべて成功。結果は `Logs/QA/Drool-PlayMode-results.xml`。同梱PowerShellから再出力した後も、Controllerファイルを変更せず5つのクリップ参照と表情カーブを維持することを確認済みです。描画確認は `Logs/QA/Drool-preview.png`。

再出力後の既存ゲームパッド操作テスト `RightStickTurnsInPlaceAndScaresInFrontDroolsAtFeet` も成功。結果は `Logs/QA/GhostModels-Gamepad-results.xml`。

WebGLでのメモリー不足を避けるため、キャラクター用テクスチャのWebGL上限を1024pxに設定しました。また、既存の飛沫処理が実行時に生成するSphereのコンポーネントがストリッピングで失われないよう、`Assets/BetoBeto/link.xml` でSphereColliderを保持しています。

最終WebGLビルドはエラー0で成功（`Builds/WebGL`）。ビルドレポートは `Logs/QA/Drool-WebGL-build-status.json`。ブラウザーのWebGL 2.0でキッチンを起動し、テスト用の標準ゲームパッド入力を通じて長押し中の流れとループ、解放後の通常ポーズへの復帰を確認しました。最終版で実行時エラーはありません。Windows用配布ビルドは今回更新していません。
