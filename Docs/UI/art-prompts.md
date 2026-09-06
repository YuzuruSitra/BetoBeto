# UI artwork generation

## Individual gameplay HUD assets (2026-09-06 revision)

Tool: built-in `image_gen.imagegen`. The supplied proposal image was used as the style reference. This revision supersedes the full-screen HUD/background approach for gameplay: environment art is omitted because the background will be scene mesh geometry. Every delivered asset is a separate RGBA PNG with a transparent corner; the recipe frame also has a transparent center for the existing 3D tart.

Shared prompt constraints: match the proposal's premium cute Japanese casual sweets-game UI, glossy rounded 3D clay/candy bevels, soft warm highlights and clean small-size silhouettes. Isolated production Unity UI asset, genuine alpha transparency, no checkerboard, no text, letters, numbers, fruit, tart or watermark.

| Asset | Final request |
| --- | --- |
| `HudEscape.png` | Pink escape capsule with attached exclamation badge and small rays; blank text area. |
| `HudScore.png` | Cream score capsule with attached golden star and sparkles; blank text area. |
| `HudTime.png` | Pale-blue time capsule with attached blue stopwatch; blank text area. |
| `HudPause.png` | Royal-blue pill button with white/pale-blue bevel and small rays; blank interior. |
| `HudRecipeFrame.png` | Pink gingham vertical recipe frame, cream top speech plaque and scalloped center opening. A background-extraction pass made both the exterior and the tart opening genuinely transparent. |
| `HudIngredientCard.png` | Neutral cream rounded ingredient card, tintable in Unity. |
| `HudProgressBadge.png` | Small pink hanging percentage badge with a concave lower edge. |
| `HudControls.png` | Long cream controls strip with subtle pink edge and blank interior. |
| `HudChef.png` | White chef ghost mascot with pink cheeks and a heart accent. |

The generated source sizes vary by composition. `KitchenUiAssetImporter` caps the runtime HUD imports at 1024px while preserving aspect ratio and disables mipmaps and compression.

Tool: built-in `image_gen.imagegen` (no CLI/API fallback). Reference: user-provided `ui-images-unique-2026-09-06/完成イメージ.png`. The original reference and other supplied source images were not overwritten.

## KitchenBackdrop.png

Edit this reference into a production game UI background, 16:9 landscape 1600x900 composition. Faithfully preserve this exact soft hand-painted pastel kitchen art, positions, proportions, colors, lighting, wooden counter, foliage edges, teacup and gingham cloth. Remove ALL text, ALL numbers, logos, symbols, counter icons, crown, progress bars and ALL game elements from the blue board including blue board itself. The left gameplay opening from x=8% to 65%, y=15% to 90% must become a clean warm medium brown wooden recessed tray, completely empty. Keep the cute white chef ghost at top left and its cream scalloped plaque. Top center left pink plaque remains blank. Top center right cream plaque remains blank. Top right navy rounded pause button remains blank. The right cream/pink recipe panel from x=66.5% to 99%, y=14% to 93% stays in the exact position but is COMPLETELY EMPTY cream inside: remove tart, spoon, plate, ingredient cards, all text, all progress and ghost drawing; keep panel border, surrounding tablecloth and tiny leaves at margins. Bottom left cream rounded controls plaque remains blank. Hanging left sign remains blank. No lettering of any kind. This is solely the reusable decorative background: all gameplay, ingredient icons, tart illustration and typography will be layered by real game engine. Do not move or resize the plaques and panels. Flat straight-on UI canvas.

## TartBase.png — first generation and transparency correction

Create a production transparent PNG illustration sprite for this game's recipe preview. Use the supplied image ONLY as the reference for the exact tart art style. Isolated EMPTY golden scalloped fruit tart shell filled with smooth pale yellow custard, on a large delicate pink scalloped porcelain plate, a silver heart-handled spoon resting diagonally on the lower right of the plate, a mint sprig on lower left. Same beautiful soft pastel hand-painted 2.5D game illustration, warm highlights, golden shortcrust fluting. Three-quarter overhead view: can see top filling and front tart wall, ellipse top. No fruit toppings whatsoever: completely EMPTY cream filling, with no marks and no silhouettes, no dashed outlines. Preserve a large flat empty filling surface for individual fruit sprites to be layered later. Genuinely transparent background, no checkerboard baked in, no text, no labels, no other objects, no cloth. Entire plate and spoon fit within square canvas with 5% transparent padding. Tart dominates center, spoon within edges.

The first output had a baked checkerboard and was rejected. Final edit prompt:

Background extraction edit. This image incorrectly has a BAKED checkerboard background. Remove all checkerboard pixels and replace background with ACTUAL alpha transparency in the PNG file. Keep the entire tart, pink plate, mint and heart spoon unchanged, exact same position and shape. Transparent PNG cutout. No checkerboard pattern drawn anywhere. Output must have an RGBA alpha channel. Isolated illustrated empty custard tart on pink plate with silver spoon, on transparent background.

Final delivered file is RGBA, 1536×1024; the top-left pixel alpha is 0.

## HudRecipeLabel.png — sewn label over the existing model towel (2026-09-06)

Built-in image generation tool; copied unchanged with its generated alpha to `Assets/BetoBeto/Resources/UI/HudRecipeLabel.png`. Only the title patch is generated. The gingham backing is the scene's existing 3D `GinghamTowel`; Japanese text and the tart remain live Unity elements.

Final prompt:

Use case: stylized-concept. Asset type: single transparent PNG game HUD recipe title label, text-free. Create a warm ivory cotton fabric sewn-on label to sit over an existing pink gingham kitchen towel in a cute 3D sweets game. Only ONE wide horizontal soft rectangular cream linen patch with gently rounded irregular fabric corners, subtle cotton weave, very thin dusty rose double stitching just inside its edges, tiny folded fabric corners, restrained warm soft edge shadow. Large uninterrupted light ivory blank center for two lines of dark Japanese text to be added in Unity. Front-on flat view, width to height about 3:1, centered with minimal transparent padding. Real transparent RGBA background outside this single label. No letters, no text, no symbols, no food, no tart, no plate, no gingham background, no wooden background, no surrounding frame or circular opening, no plastic gloss, no puffy candy border, no decorative objects. High quality soft stylized 3D fabric game UI sprite.

## FruitIcons.png

Production game sprite sheet, transparent PNG, exact square 2 by 2 grid of FOUR separate fruit icons. Transparent background, no checkerboard painted, no shadows outside icons, no text, no lines, no badges, no panels. Each icon centered in its own equal-size quadrant with generous transparent padding, nothing crosses quadrant boundaries. Top-left: one shiny red whole strawberry with little green leaf crown, upright, seeds, cute soft hand-painted highlights. Top-right: one large juicy indigo blue blueberry with visible star calyx, round, soft violet reflected light. Bottom-left: one bright orange half-moon citrus wedge with juicy golden segments and orange peel, angled upright slightly to right. Bottom-right: one pale luminous green melon cube with very softly rounded edges and pale cream rind edge at bottom. Match supplied reference's ingredient icons and tart fruit, beautifully hand-painted Japanese cozy sweets game illustration, fine warm outlines, pastel light, semi 3D volume, polished charming food art, three-quarter overhead view. These sprites must read at 48 pixels and be suitable as toppings placed on a tart. Four distinct sprites only, one per quadrant, centered and occupying 65% of each quadrant.

Final delivered file is RGBA, 1254×1254. The atlas is sliced at runtime without modifying the source image. All three final images are committed project resources, not external cache dependencies.
