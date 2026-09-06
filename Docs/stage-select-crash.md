# ステージ選択時クラッシュ調査（2026-09-06）

## 原因と対応

Unity 6000.6.0f1 のPC用URP設定でGPU Resident Drawerを有効にすると、TitleからStageSelectへの切替直後にUnityのネイティブ処理がクラッシュする。`Assets/Settings/PC_RPAsset.asset` の `m_GPUResidentDrawerMode` を1から0に変更し、この経路を回避する。

停止箇所は `ParallelFilterChangedInstancesAndCreateScriptingArray / FilterInstancesAndCollectScriptingObjectsJobData::ExecuteJob_NoScriptingArray`。同梱Render Pipelines Coreの `GPUResidentDrawer.PostPostLateUpdate` → `WorldProcessor.Update` → `ObjectDispatcher.GetTypeChangesAndClear(... noScriptingArray: true)` の経路と一致する。ステージ切替に伴うオブジェクト・アセットの破棄／生成時に発生するが、Unityネイティブ内部の不正アクセス対象や競合の詳細までは未確定。

ステージJSONの参照・寸法・行長、ミニマップの頂点添字に異常は見つからなかった。UI・フォント・ステージデータの変更は行わず、描画最適化のみ無効化した。GPU Resident Drawerによる最適化効果は失われるため、将来再導入する場合は修正版エンジンで検証する。

## 再現証拠

- 元の障害: `Logs/Editor.log:780` に同じネイティブスタック。
- 修正前の描画付き自動試験: `Logs/StageCrash-Rendered-Baseline.log`。558行でGPU Resident Drawer生成、562行でPC品質、590行でTitle、602行でStageSelect読込、613行でCrash、770行で同じ停止箇所。
- 独立エージェントもパッケージの呼出経路、JSON、メッシュ、追加テスト、再現ログを確認。

## 回帰テスト

`SceneFlowTests.PcQualityCanRepeatedlySelectAndLeaveBothStages` はPC品質を明示し、タイトルの選択ボタン→ステージ選択→第1／第2キッチン→ステージ選択への復帰を3周する。復帰後は10フレーム待機し、元の品質設定はfinallyで復元する。

バッチ試験は修正前でも成功したが、GPU Resident Drawerの生成ログが出ず、障害経路を検証できなかった。`-batchmode` や `-nographics` を付けず通常Editorの描画付き試験を使用する。

```powershell
& 'D:/DeviceApps/UnityEditors/6000.6.0f1/Editor/Unity.exe' -buildTarget Win64 -projectPath 'D:/0_Data/Developer/unity_works/BetoBeto' -runTests -testPlatform PlayMode -testFilter BetoBeto.Tests.SceneFlowTests -testResults Logs/StageCrash-Rendered-Fixed.xml -logFile Logs/StageCrash-Rendered-Fixed.log
```

修正後はコンパイル成功、通常Editor描画付きのSceneFlowTestsが3件すべて成功（失敗0、テスト実行7.52秒）。PC品質の反復遷移、成功／失敗リザルト、再挑戦、選択画面への復帰を確認した。同じ追加テストが修正前はクラッシュし、GRD設定だけ変更した修正後は成功した。これにより今回の障害を回避できることを確認した。Standalone実行ファイルのビルド試験は今回の範囲に含めない。

結果は上記XMLとログで確認できる。ログはローカルの検証成果物で、リポジトリには含めない。
