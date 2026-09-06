# 窓と観葉植物のあるキッチンの反射・木漏れ日

## 共通のライティング・背景シーン

`Assets/BetoBeto/Scenes/KitchenEnvironment.unity` に照明・テーブル・背景小物をまとめています。既存5ステージにはLightコンポーネントを置かず、`KitchenEnvironmentLoader` が開始時にこのシーンをAdditiveで読み込みます。ステージの生成をStageImporterから行う場合も同じ構成です。メニューへのSingle遷移では共通シーンも一緒にアンロードされ、再試行時に重複させません。ステージの進行時間・敵の更新は共通環境の準備を待ちます。

編集時は `BetoBeto > Open Shared Kitchen Environment` で、ステージと並べて開けます。共通シーンの `Shared kitchen · lighting and background` を編集してください。Directional Lightは `Window sunlight · leaf cookie` です。HDR反射と環境色は `KitchenEnvironment` コンポーネントに保存しています。

木目のテーブル、鉢植え2点、保存容器2点、ボウルと泡立て器、麺棒、クッキーを載せた天板、ギンガムの布を配置しました。小物位置はコンポーネントの `Board Anchors`（盤面端）と `Offsets`（端からの距離）で調整できます。ステージ幅・高さが変わっても盤面の外側に追従します。装飾のレイヤーは既存のIgnore Raycast（2）を利用し、ゲーム判定用のコライダーは持ちません。

共通シーンの背景カメラが画面全体を描画し、ステージのカメラが盤面を重ねます。両者の投影を合わせ、画面枠の境界でテーブルがずれないようにしています。ゲーム中の背景イラストは表示せず、HUDの情報カードだけを残しています。タルトのUIプレビュー用リグも、ステージ中は共通シーンに所属させます。

小物の編集用blend：`G:\Blender\CleanAssets\BETOBETO\Environment\Decor`。Unity用は `Models` / `Prefabs` / `Materials` に保存しています。blendでは部品を分け、FBXでは小物単位に結合して描画数を抑えています。再生成元は `Tools/Blender/build_kitchen_decor.py` です。

## シュレッダーの刃

刃は元の平らな8歯ローターで、切削端に細い面取りを付けています。持ち上げ・反り・ひねりは取り消しました。金属の照明計算と暗部の修正は維持し、`BladeReflection.exr` の白い帯と暗い間隔を使用します。平面の反射方向を時間だけで動かす表現は加えていません。

専用マップは内蔵image_genで生成した元画像を、Blenderで最大14の輝度に展開したEXRです。UnityではRGBAHalf・通常のミップマップとしてインポートし、線形HDRとして読み取ります。刃の `Metallic` は1.0で、URPのBRDF計算に実際に使用しています。キッチン反射を銀色のベースにし、黒背景の専用マップを追加のハイライトとして加算します。黒い部分はベース反射を暗くしません。材質の `Kitchen reflection exposure` は2.2、`Additive highlight reflection` は0.28です。編集用 `BladeReflection.blend` と元画像を保存しています。

生成プロンプト：Create a 2:1 equirectangular HDR reflection SOURCE texture specifically for extremely flashy spinning metal blades in a stylized game. Abstract reflection environment only: deep near-black charcoal background, a few broad brilliant white curved light bands with razor-sharp edges, thin icy blue-white strips, one subtle warm gold strip, large black intervals for maximal contrast. Bright angular white patches around the equator and upper hemisphere, asymmetrical rhythmic arrangement so spinning metal shows alternating dark and dazzling bright reflections. No room, no objects, no blade, no text, no logos, no material spheres, no lens flare baked in. Full spherical equirectangular panorama, seamless left-right, 2:1 wide image. White sources will be expanded to HDR intensity in Blender. Art-directed chrome reflection map, intentionally dramatic rather than soft studio illumination.

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
