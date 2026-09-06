using System.Collections.Generic;
using BetoBeto.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.UI
{
    /// <summary>The live tart, shown as a real model: each ingredient group fills independently from the recipe,
    /// including zero-goal recipes, and every newly earned group drops onto the shell.</summary>
    public sealed class TartPreview : MonoBehaviour
    {
        public static readonly Vector2 DesignSize = new Vector2(500, 340);
        // Every preview keeps its own pocket of empty space, far under the kitchen no camera or ray reaches.
        static readonly Vector3 StageOrigin = new Vector3(0, -1000, 0);
        static readonly HashSet<int> takenPockets = new HashSet<int>();

        RawImage view;
        RenderTexture texture;
        Camera previewCamera;
        TartModelStage stage;
        GameObject rig;
        readonly int[] counted = new int[4];
        int pocket = -1;
        bool primed;

        public TartModelStage Stage => stage;
        public Camera PreviewCamera => previewCamera;

        public void Initialize()
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.sizeDelta;
            if (size.x < 1 || size.y < 1) size = rect.sizeDelta = DesignSize;
            view = new GameObject("Tart render", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            var frame = view.rectTransform;
            frame.SetParent(transform, false);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            view.raycastTarget = false;
            // Rendered at twice the design size so the tart stays crisp on larger windows; the extra
            // resolution does most of the smoothing, so the samples only match the pipeline's own MSAA.
            texture = new RenderTexture(Mathf.RoundToInt(size.x * 2), Mathf.RoundToInt(size.y * 2), 24,
                RenderTextureFormat.ARGB32) { name = "Recipe tart preview", antiAliasing = 2 };
            view.texture = texture;
            view.color = Color.white;
            BuildStage(size.x / size.y);
        }
        void BuildStage(float aspect)
        {
            rig = new GameObject("Recipe tart preview");
            var environment = UnityEngine.SceneManagement.SceneManager.GetSceneByName(BetoBeto.Presentation.KitchenEnvironmentLoader.SceneName);
            if (environment.isLoaded) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig, environment);
            // A free pocket, never a shared one: two previews in one place would light and film each other.
            for (pocket = 0; !takenPockets.Add(pocket); pocket++) { }
            rig.transform.position = StageOrigin + new Vector3(pocket * 40, 0, 0);
            var model = new GameObject("Tart", typeof(TartModelStage));
            model.transform.SetParent(rig.transform, false);
            stage = model.GetComponent<TartModelStage>();
            if (!stage.Build())
            {
                Discard(rig); rig = null; stage = null;
                view.enabled = false;
                return;
            }
            previewCamera = new GameObject("Tart camera", typeof(Camera)).GetComponent<Camera>();
            previewCamera.transform.SetParent(rig.transform, false);
            previewCamera.orthographic = true;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            // Transparent, and tinted like the recipe panel behind it so an opaque clear would still blend in.
            previewCamera.backgroundColor = new Color(.992f, .953f, .910f, 0);
            previewCamera.useOcclusionCulling = false;
            previewCamera.allowHDR = false;
            Frame(aspect);
            previewCamera.targetTexture = texture;
            rig.SetActive(isActiveAndEnabled);
        }
        // A hidden panel must not keep filming: the rig follows the element that owns it.
        void OnEnable() { if (rig != null) rig.SetActive(true); }
        void OnDisable() { if (rig != null) rig.SetActive(false); }
        /// <summary>A key light of the preview's own, so the tart reads the same on the unlit menus as in a kitchen.
        /// It is a point light with a short range: the kitchen far above can never be touched by it.</summary>
        void KeyLight(Vector3 center, float size, Vector3 right, Vector3 up, Vector3 forward)
        {
            bool sceneIsLit = false;
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                sceneIsLit |= light.isActiveAndEnabled && light.type == LightType.Directional && light.intensity > .2f;
            var key = new GameObject("Tart key light", typeof(Light)).GetComponent<Light>();
            key.transform.SetParent(rig.transform, false);
            key.type = LightType.Point;
            key.color = new Color(1, .96f, .89f);
            key.shadows = LightShadows.None;
            float distance = Mathf.Max(size, .01f) * 8;
            key.range = distance * 3;
            key.transform.position = center + (up * 1.1f - forward * .9f + right * .5f).normalized * distance;
            // Point lights fall off with the square of the distance; this keeps the lift itself scale free.
            key.intensity = (sceneIsLit ? .5f : 1.35f) * distance * distance;
        }
        /// <summary>Frames the tart as a cylinder, so the slow sway never pushes an ingredient out of view,
        /// and leaves headroom above it for the falling ingredients to drop through.</summary>
        void Frame(float aspect)
        {
            Bounds bounds = stage.RestBounds;
            // The tart turns around its own axis, which the plate and spoon are not centred on.
            Vector3 axis = stage.transform.position;
            var center = new Vector3(axis.x, bounds.center.y, axis.z);
            float radius = 0;
            for (int corner = 0; corner < 4; corner++)
                radius = Mathf.Max(radius, new Vector2(
                    ((corner & 1) == 0 ? bounds.min.x : bounds.max.x) - axis.x,
                    ((corner & 2) == 0 ? bounds.min.z : bounds.max.z) - axis.z).magnitude);
            float height = bounds.extents.y;
            previewCamera.transform.rotation = Quaternion.Euler(23, 18, 0);
            Vector3 right = previewCamera.transform.right, up = previewCamera.transform.up;
            Vector3 forward = previewCamera.transform.forward;
            float halfWidth = 0, halfHeight = 0;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3((corner & 1) == 0 ? -radius : radius,
                    (corner & 2) == 0 ? -height : height, (corner & 4) == 0 ? -radius : radius);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(offset, right)));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(offset, up)));
            }
            float fit = Mathf.Max(halfHeight, halfWidth / Mathf.Max(.1f, aspect)) * 0.5f;
            float opening = fit * 1f;
            float distance = radius + height + 5;
            previewCamera.orthographicSize = opening;
            previewCamera.transform.position = center + up * ((opening - fit) * .7f) - forward * distance;
            previewCamera.nearClipPlane = .01f;
            previewCamera.farClipPlane = distance * 2 + 10;
            // Ingredients start above the opening, so they always enter the frame from off screen.
            stage.dropHeight = opening * 1.7f;
            KeyLight(center, Mathf.Max(radius, height), right, up, forward);
        }
        public static bool IsToppingCollected(int slot, int harvested, int goal)
            => goal > 0 && harvested > 0 && (slot == 2 ? harvested >= goal
                : slot < Mathf.CeilToInt(Mathf.Clamp01(harvested / (float)goal) * 2));
        public void Refresh(GameSession session)
        {
            if (stage == null) return;
            for (int kind = 0; kind < 4; kind++)
            {
                int goal = session.Recipe.For((FruitKind)kind), harvested = session.Harvested[kind];
                bool changed = false;
                for (int slot = 0; slot < 3; slot++)
                    // The opening state is placed rather than dropped, so a retry never rains its old progress back in.
                    changed |= stage.SetCollected(kind, slot, goal > 0 && IsToppingCollected(slot, harvested, goal), !primed);
                // Counts that do not finish a group still register, so every harvest shows on the tart.
                if (primed && !changed && harvested > counted[kind]) stage.Nudge(kind);
                counted[kind] = harvested;
            }
            primed = true;
        }
        public void ShowComplete()
        {
            primed = true;
            if (stage != null) stage.ShowAll();
        }
        void OnDestroy()
        {
            if (previewCamera != null) previewCamera.targetTexture = null;
            Discard(rig);
            if (texture != null) { texture.Release(); Discard(texture); }
            if (pocket >= 0) takenPockets.Remove(pocket);
            pocket = -1;
            rig = null; texture = null; stage = null; previewCamera = null;
        }
        static void Discard(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}
