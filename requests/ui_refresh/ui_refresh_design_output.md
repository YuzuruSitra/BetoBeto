<!-- DESIGN_OUTPUT -->
# Design: Gameplay HUD visual refresh

## Overview

参照画像の菓子ゲーム風HUDへゲームプレイ画面のみを更新する。背景は今後シーンメッシュで構成するため、HUD画像はすべて透明な独立素材とする。レシピ内のタルトは既存の `TartPreview` による3D描画を維持する。

## Requirements

1. 脱出、スコア、時間、一時停止、レシピ、材料、進捗、操作案内を参照画像の画風へ揃える。
2. ラベル、数値、ステージ名、材料名はUnity UI Textとして動的に表示する。
3. HUD素材に木目、植物、盤面などの背景を含めない。
4. 各HUD部品を独立したRGBAテクスチャとして配置し、一枚の画面アトラスへ結合しない。
5. 3Dタルト、収穫連動、落下アニメーション、ゲームパッド操作を維持する。

## Coding Standards

- Unity規約を適用。新規コードではLINQとtry-catchを追加しない。
- 既存の1600×900設計座標、`UiView.Art`、`KitchenArt` のロード方式を継続する。

## Architecture

- `KitchenArt`: 個別HUDテクスチャを遅延ロードしてSprite化する。
- `GameHud`: 個別Spriteを先に配置し、その上へ動的テキスト、3Dタルト、材料アイコン、入力可能な一時停止ボタンを重ねる。
- `TartPreview`: 変更せず、透明中央穴へRenderTextureを表示する。
- `KitchenUiAssetImporter`: HUD画像を1024px上限・非圧縮・ミップマップなしで取り込む。

## Design Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| 背景 | HUDから除外 | シーンメッシュ化する方針のため |
| HUD素材 | 9個の独立PNG | 個別配置、調整、非表示、ボタン判定を容易にするため |
| タルト | 既存3D描画 | ユーザー指定および収穫連動を保持するため |
| 文字 | Unity UI Text | 動的値とローカライズ可能性を保持するため |

## Testing Strategy

- 全HUDテクスチャがResourcesからロードでき、別々のTextureであることをEditModeで確認する。
- Unity再コンパイルと `KitchenUiTests` を実行する。
- 1600×900のゲーム画面を撮影し、HUD重なり、タルト穴、カード、操作案内を目視確認する。

## Open Questions

- 背景メッシュの最終カメラ構図に合わせたHUD座標の微調整は、背景側の完成後に再確認する。

<!-- /DESIGN_OUTPUT -->
