# フルーツタルト・分割アセット

配置先: `Assets/BetoBeto/Art/FruitTart`

## 使い方

- `Prefabs/FruitTart_Assembled.prefab` をシーンへドラッグすると、皿・タルト・各フルーツ・ミント・スプーンを組み立てた見本を配置できます。Hierarchy内の各子オブジェクトを個別に移動・回転・拡縮できます。
- `Prefabs/Parts/` は単品の配置用Prefabです。マテリアルとColliderを設定済みで、Rigidbodyは付けていません。
- `Prefabs/Physics/` はフルーツ5種・ミント・スプーンのRigidbody付きPrefabです。重力が有効なため、床や受け皿のあるシーンに配置してください。
- `Prefabs/FruitTart_PartsGallery.prefab` で全10種類を一覧配置できます。
- `Models/` のFBXは1ファイル1メッシュです。Unity以外でも個別に読み込めます。

## パーツ

| ファイル名 | 内容 | 元データ |
|---|---|---|
| Tart_Shell | タルト台 | tripo_part_2 |
| Pink_Plate | ピンクの皿 | tripo_part_3 |
| Heart_Spoon | ハートのスプーン | tripo_part_0 |
| Strawberry_Whole | 丸ごとのイチゴ | tripo_part_5 |
| Strawberry_Half | 半割イチゴ | tripo_part_9 |
| Mandarin_Segment | ミカンの房 | tripo_part_7 |
| Melon_Cube | メロン角切り | tripo_part_6 |
| Blueberry | ブルーベリー | tripo_part_4 |
| Mint_Sprig | ミントの葉付き枝 | tripo_part_8 |
| Custard_Filling | カスタード | 組み立て補助用に追加 |

## 寸法・描画

1 Unity unit = 1 m。タルト直径24 cm、皿直径30 cm。全パーツの原点は底面中央、Unity上でYが上、標準スケールは(1,1,1)です。大きさの異なるゲームでは親オブジェクトで一括調整してください。半割イチゴとミカンの単品は元の寝かせた向きを基準にしており、組み立て例では回転を設定しています。

Unity 6000.6.0f1 / URP 17.6.0向けのLitマテリアルを設定済み。元のBaseColor・Normal・Roughnessを同梱しています。MetallicSmoothnessはR=金属度、A=1−粗さ。食品は非金属、スプーンは金属として設定しました。元素材の形とテクスチャを活かしたアセットで、参考写真の構成を組み立て例に反映しています。

## 修正内容

タルト台と皿の縦横比をそろえて円形を補正し、底面とタルトの縁を整えました。フルーツには控えめな平滑化を適用し、ブルーベリーの輪郭・メロンの比率と角を補正しました。ミントの三重接合面を修復して裏側の欠損を塞ぎました。各モデルの法線を再計算し、UVを保持してFBXへ書き出しています。

当たり判定は操作・物理計算用の近似形状です。フルーツはSphere/Capsule/Box、ミントとスプーンはBoxです。タルト台は底の凸Meshと28個のBoxによる複合Colliderで、内部を空洞に保っています。皿は静的MeshColliderです。組み立てPrefabは自動で崩れないようRigidbodyを含めていません。物理用Prefabを使う場合は、各素材を個別の動的オブジェクトとして配置してください。

## 検証・編集元

`Documentation/geometry_report.json` に形状修正とポリゴン数、`fbx_validation.json` にFBX再読み込み検証、`unity_validation.json` にUnity実インポート検証を記録しています。

`Documentation/Parts_Preview.png` は形状を見比べやすいよう、各パーツの表示サイズをそろえた一覧です。実際の寸法はFBXおよび各検証レポートを参照してください。

編集用Blender: `G:/Blender/AssetWork/BETOBETO_FruitTart/FruitTart_Modular.blend`

元アーカイブ: `G:/Blender/Assets/BETOBETO_Pack/food+props+3d+model.zip`（未変更）

生成・検証は共有プロジェクトから隔離した検証用Unityで実施しました（作業記録: `G:/Blender/AssetWork/BETOBETO_FruitTart`）。既存プロジェクトのスクリプト・シーン・設定・既存アセットは変更せず、専用フォルダの追加のみ行っています。検証用Editorスクリプトや検証用URP設定は配布フォルダに含めていません。
