# BetoBeto project context

## HUD on model background (2026-09-06)
- Gameplay HUD now uses the existing 3D kitchen surround. Do not call `BuildBackdrop` from `GameHud` or cover the scene with background art.
- HUD source PNGs contain transparent padding: size the painted interior, not just the RectTransform, against the labels. In particular `HudControls` paints approximately y=794..872 in the 900-high design despite its rectangle extending below 900.
- Ingredient numbers are centered inside each card; the 100% badge, SCORE/TIME and long-hold control instruction have been checked in rendered Play mode. Max-charge instruction measures 255 design pixels within its 331-pixel label.
- Latest check: compilation passed, KitchenUiTests 39/39, pause/resume via HUD and recipe 13/13 live counter update passed. Screenshots: `Assets/Docs/UI/hud-model-background.png`, `hud-model-complete.png` (1920x1080 Game View captured at 1600x900).

## UI implementation (2026-09-06)
- Visual target: `ui-images-unique-2026-09-06/完成イメージ.png`. Other supplied images are supporting asset/style references.
- Goal: make the gameplay HUD resemble the supplied finished image. The environment/background is intentionally excluded from generated UI art because it will be implemented as scene mesh geometry.
- Keep labels, counters, instructions and stage names as real Unity UI text. Use Zen Maru Gothic for headings and M PLUS Rounded 1c for body/counters, bundled with licenses.
- Preserve actual gamepad controls (south = drool, west = scare); the reference's dash instruction is not a gameplay requirement.
- Preserve stage data, gameplay rules, existing scene content and supplied source images.
- HUD and camera must share a single layout definition; resize and floating feedback must use that same game viewport.
- Keep reusable art in project assets; never depend on a generated image outside the repository.
- Keep HUD chrome as independent transparent textures (escape, score, time, pause, recipe frame, ingredient card, progress badge, controls and chef mascot), not one full-screen atlas. This allows independent positioning and responsive tuning.
- Verify significant changes with an independent subagent, Unity compilation and meaningful scene/UI checks where available.

## Implementation plan
1. Inspect reference art, existing screens, fonts, camera and scene tests.
2. Create/reuse text-free kitchen artwork and ingredient/tart assets; add selected fonts.
3. Implement the reference HUD composition and recipe-driven tart preview, with shared responsive layout.
4. Apply the visual language to title, stage selection, result and options; retain navigation and live values.
5. Compile, inspect rendered screens and exercise recipe/scene/navigation behavior; document verified scope and outstanding environment limitations.

## 3D recipe tart (2026-09-06)
- The HUD/menu tart is the real `FruitTart_Assembled` model, not the flat `UI/TartBase` sprite. It lives in `Assets/BetoBeto/Art/FruitTart/Resources/` so `TartModelStage` can `Resources.Load` it; the part prefabs stay in `Art/FruitTart/Prefabs/Parts/`.
- `TartModelStage.ToppingParts` maps the prefab's direct children to 4 fruit kinds x 3 slots, so renaming a child in the prefab silently drops that ingredient (it logs a warning). `TartPreview.IsToppingCollected` still decides when a slot is earned.
- Each preview parks its own rig 1000 units under the kitchen with its own orthographic camera, RenderTexture and short-range point key light, so no gameplay camera, ray or light is touched. The key light dims itself when the scene already has a directional light: `Result` and `StageSelect` have none.
- The camera clears transparent but is tinted like the recipe panel behind it, so a lost alpha channel would still blend in. Keep post-processing off for that camera: `PC_RPAsset.asset` has `m_AllowPostProcessAlphaOutput: 0`, so a post pass would force alpha to 1 and flatten the cut-out. There is no Volume in any scene today, and the runtime camera relies on URP's default `renderPostProcessing = false`.
- `WebBuild.ClampWebTextures` caps the tart's 2048 PBR maps at 512 for WebGL; the model is now really loaded at runtime, so those maps count against `maximumMemorySize`.
- `TartPreview.Frame` has two zoom knobs: `fit` (how much of the model the opening must hold) and `opening` (extra height for the fall). They are currently tuned to a close-up that crops the plate rim and spoon; the test only guarantees that every ingredient's landing spot stays inside the panel and that ingredients still enter from off screen.

## Stage selection crash (2026-09-06)
- Keep `Assets/Settings/PC_RPAsset.asset` GPU Resident Drawer disabled (`m_GPUResidentDrawerMode: 0`) on Unity 6000.6.0f1. Enabling it reproducibly crashes the rendered Editor when Title loads StageSelect, inside `ParallelFilterChangedInstancesAndCreateScriptingArray / ExecuteJob_NoScriptingArray`.
- Regression: `SceneFlowTests.PcQualityCanRepeatedlySelectAndLeaveBothStages` explicitly selects PC quality and exercises both kitchens. Run with ordinary Editor rendering (`-runTests -testPlatform PlayMode`, without `-batchmode` or `-nographics`); batch tests passed even with the broken setting and did not create GPU Resident Drawer.
- Re-enable this optimization only after an engine fix and the rendered regression passes. See `Docs/stage-select-crash.md` for evidence and limitations.
<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: BetoBeto
- Unity version: Unity 6000.6.0f1
- Active scene:
  - Name: Title
  - Tags:
    - Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController
  - Layers:
    - Default, TransparentFX, Ignore Raycast, Water, UI
- Active game object:
  - Name: Recipe tart preview
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->
