# 窓と観葉植物のあるキッチンの反射・木漏れ日

## 共通のライティング・背景シーン

`Assets/BetoBeto/Scenes/KitchenEnvironment.unity` に照明・テーブル・背景小物をまとめています。既存5ステージにはLightコンポーネントを置かず、`KitchenEnvironmentLoader` が開始時にこのシーンをAdditiveで読み込みます。ステージの生成をStageImporterから行う場合も同じ構成です。メニューへのSingle遷移では共通シーンも一緒にアンロードされ、再試行時に重複させません。ステージの進行時間・敵の更新は共通環境の準備を待ちます。

編集時は `BetoBeto > Open Shared Kitchen Environment` で、ステージと並べて開けます。共通シーンの `Shared kitchen · lighting and background` を編集してください。Directional Lightは `Window sunlight · leaf cookie` です。HDR反射と環境色は `KitchenEnvironment` コンポーネントに保存しています。

木目のテーブル、鉢植え2点、保存容器2点、ボウルと泡立て器、麺棒、クッキーを載せた天板、ギンガムの布を配置しました。小物位置はコンポーネントの `Board Anchors`（盤面端）と `Offsets`（端からの距離）で調整できます。ステージ幅・高さが変わっても盤面の外側に追従します。装飾のレイヤーは既存のIgnore Raycast（2）を利用し、ゲーム判定用のコライダーは持ちません。

共通シーンの背景カメラが画面全体を描画し、ステージのカメラが盤面を重ねます。両者の投影を合わせ、画面枠の境界でテーブルがずれないようにしています。ゲーム中の背景イラストは表示せず、HUDの情報カードだけを残しています。タルトのUIプレビュー用リグも、ステージ中は共通シーンに所属させます。

小物の編集用blend：`G:\Blender\CleanAssets\BETOBETO\Environment\Decor`。Unity用は `Models` / `Prefabs` / `Materials` に保存しています。blendでは部品を分け、FBXでは小物単位に結合して描画数を抑えています。再生成元は `Tools/Blender/build_kitchen_decor.py` です。

## 背景小物の更新

トレーの6枚のクッキーは、ステージの `BreakableCookie` の未破壊形状に置き換え、`BreakableCookie_Surface` / `BreakableCookie_Crumb` の材質を共有しています。背景用にはシェイプキーを焼き付けた静的メッシュを使用します。

`GinghamTowel` は4頂点・2三角形の板ポリ1枚です。チェック模様と織り目は `Textures/GinghamCloth_Albedo.png` にまとめ、厚み・しわ・チェックの個別メッシュを使用しません。材質はURP Lit、非金属、Smoothness 0.1、両面表示です。画像は内蔵image_genで生成し、プロンプトを `Comcept/PropReferences/Background/generation-prompts.json` に保存しています。

編集用blendとUnity用FBXを更新済みです。再生成は `Tools/Blender/refresh_background_props.py`、材質の再適用は `BetoBeto > Refresh Background Cookie And Cloth Materials`。既存シーンの配置は維持し、参照するモデル・材質だけを更新します。

保存ポット・ボウルと泡立て器・麺棒の外注画像と構造メモは `Comcept/PropReferences/Background` にあります。この3点の本番モデルは外注差し替え待ちです。

## シュレッダーの刃

刃は8歯ローターで、中心部は平らに保ち、切削端だけ最大0.024マスの浅い起伏と0.004マスの面取りを付けています。刃全体の高さや外径は据え置きです。`BladePlanarReflection.png` をカメラ方向に揃えたワールドXZ平面から投影し、法線が一様な面にも銀色・暗部・白い帯を出します。MatCapや反射方向による専用Cubemap参照ではありません。

専用平面マップは明暗の幅を数値で設計した512×512の線形RGB画像です。Repeat・ミップ付き・Trilinearで取り込みます。刃の中心を投影の原点にし、投影の軸はカメラ基準で固定します。帯は刃の回転に追従せず、その中を刃だけが回ります。移動シュレッダーでは原点が本体の移動に追従します。時間によるスクロールもありません。これは見栄えを優先した疑似反射で、キッチンの物体位置を正確に映すものではありません。

画面上の位置と視線方向による反射の偏り・伸縮も加えています。透視投影では投影行列のFOVを使い、平行投影では反射用の仮想FOV（既定45度）を使います。ゲームカメラ自体の投影は変更しません。`View direction distortion`（既定0.35）で歪みを調整でき、0では従来の平面投影です。帯の投影軸は刃の回転に追従しませんが、実際の浅い歯の傾斜に対する直接光・環境反射は回転で変わります。

刃の `Metallic` は1.0でURPのBRDF計算に使用します。`Planar reflection exposure` は0.65、`Bands per local unit` は1.3。実際のキッチン反射も `Kitchen reflection exposure` 0.8で加えています。固定・移動シュレッダー共通です。再生成と適用は `BetoBeto > Rebuild Blade Planar Reflection`。生成元は `KitchenEnvironmentBuilder.BuildBladePlanarMap`、再インポート時の材質設定も同クラスの `ConfigureBladeMaterial` に統一しています。形状の生成元は `Tools/Blender/prepare_props.py` の `create_shredder_rotor`。既存の外装を保って刃だけを更新するスクリプトは `Tools/Blender/update_shallow_blades.py` です。

以前の専用Cubemap `BladeReflection.exr` は現在の刃では使用しません。編集用 `BladeReflection.blend` と元画像は保存しています。以下はその旧マップの生成記録です。

生成プロンプト：Create a 2:1 equirectangular HDR reflection SOURCE texture specifically for extremely flashy spinning metal blades in a stylized game. Abstract reflection environment only: deep near-black charcoal background, a few broad brilliant white curved light bands with razor-sharp edges, thin icy blue-white strips, one subtle warm gold strip, large black intervals for maximal contrast. Bright angular white patches around the equator and upper hemisphere, asymmetrical rhythmic arrangement so spinning metal shows alternating dark and dazzling bright reflections. No room, no objects, no blade, no text, no logos, no material spheres, no lens flare baked in. Full spherical equirectangular panorama, seamless left-right, 2:1 wide image. White sources will be expanded to HDR intensity in Blender. Art-directed chrome reflection map, intentionally dramatic rather than soft studio illumination.

## 床のよだれの天井反射

`DroolPuddle_Liquid` に、`KitchenReflection_Source.png` の天井・窓上部を使った平面投影の疑似反射を追加しています。画面内の位置と視線方向による歪み、液面の緩い曲率を加えています。金属用の反射帯は使用せず、木材の色を抑えた柔らかい窓明かりを、明るい部分だけ薄く合成します。透明な部分では床が見え、既存の輪郭光・きらめき・通常の環境反射も残ります。

`Ceiling reflection opacity` は0.32、`Ceiling projection scale` は0.24、`Ceiling view distortion` は0.18です。メタルネスは0のままです。設定元は `KitchenEnvironmentBuilder.ConfigureDroolReflection`、再インポート時も適用します。他の透明小物はこの効果が既定0で、口からのしずくは変更していません。確認用静止画は `DroolCeilingPreview.png` です。

液面の法線には方向・波長の異なる3つの緩い波を合成しています。`Liquid ripple normal strength` は0.18、`Liquid waves per world unit` は2.6、`Liquid ripple speed` は0.7です。ワールド座標から波の勾配を計算し、直接光・環境反射・輪郭光に適用します。天井画像も同じ法線で歪ませるため、水面の波と映り込みが連動します。メッシュや判定は動かさず、床のよだれだけで有効です。強度0で停止前の平滑な表面に戻せます。

## キッチンHDRと木漏れ日

現在使用している反射マップは `KitchenReflection.exr` です。窓、観葉植物、木製カウンターを持つキッチンを内蔵image_genで生成し、Blenderで窓と日なたに最大10の線形輝度を与えた2048×1024・16-bit float RGBのEXRにしました。背景表示ではなく、ガラス・ゼリー・金属などの反射に使用します。

`LeafSunlightCookie.png` は別途生成した葉影のグレースケール画像です。Unityでは512px・線形色空間・Repeatとして取り込み、Directional LightのCookieに設定しています。投影サイズは8×8、光の強度は2.4、暖色の直射光と寒色寄りの環境光を組み合わせています。葉影は静止した投影です。ゲーム中に画像生成や反射カメラの撮影は行いません。

ステージでは共通シーン、メニューでは各シーンの照明設定を使います。URPのライトクッキー機能に加え、おばけ・透明小物・刃の独自シェーダーにもCookie対応を追加しました。口からのしずくの材質は変更していません。

編集元：`G:\Blender\CleanAssets\BETOBETO\Environment\KitchenReflection.blend`。元画像はパック済みです。

再生成は `build_studio_reflection.py -- --kitchen`、再適用はUnityメニュー `BetoBeto > Apply Kitchen Environment Lighting`。LightのAdditional Light DataにあるCookie Sizeで葉影の大きさを調整できます。再適用すると上記の既定値に戻ります。

生成プロンプトは [KitchenGenerationPrompts.md](KitchenGenerationPrompts.md) に記録しています。

## 以前のスタジオ版（保存のみ・現在は未使用）

`StudioReflection.exr`：2048×1024、16-bit float RGB、正距円筒の反射環境画像。
生成PNGをそのままHDRと呼ぶのではなく、Blenderのワールドノードで明るい照明部分に最大8の線形輝度を与えてEXRにレンダーしています。実写の露出を復元したHDRIではなく、ゲーム用に輝度を設計した環境です。

両EXRとも `StudioReflectionImporter` により256px/面・Specular convolution・ミップ付きのCubemapへ取り込みます。現在のキッチン版の反射強度は0.8です。実写の露出データではなく、明暗を設計したゲーム用HDR環境です。

編集元：`G:\Blender\CleanAssets\BETOBETO\Environment\StudioReflection.blend`。生成画像はblendにもパックしています。ノードのMap Rangeで輝度を変更できます。
再生成：Blenderで `Tools/Blender/build_studio_reflection.py` を実行し、Unityの `BetoBeto > Apply Delivered Prop Models` で反映します。

元画像は内蔵image_genで生成した `StudioReflection_Source.png` です。生成に使用したプロンプト：

> Use case: photorealistic-natural. Create one 2:1 equirectangular full 360-degree studio environment panorama to be used as the reflection environment for a bright playful 3D food game. Empty neutral charcoal gray photographic studio, several large luminous rectangular softboxes and long white strip lights arranged above the horizon at different angles, subtle warm ivory and pale cool blue fill, dark areas between the softboxes for clear contrast on polished steel, glass, water and jelly. Broad overhead white ceiling light, quiet gray floor below. Physically believable studio environment, perfectly seamless left/right wrap, equirectangular projection with polar distortion, no subject, no objects for sale, no characters, no text, no logo, no spheres, no sample material balls. Light sources must be distinct clean shapes rather than uniform white sky. Output a wide 2:1 panorama, ideally 2048x1024. This will be converted into a floating-point HDR reflection map in Blender with calibrated bright emitters.
