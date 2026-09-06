using BetoBeto.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    // Copy the final stage view after player tracking and impact shake, avoiding a one-frame seam.
    [DefaultExecutionOrder(100)]
    public sealed class KitchenEnvironment : MonoBehaviour
    {
        public Cubemap reflection;
        public Color ambient = new Color(.48f,.54f,.61f);
        public float reflectionIntensity = .8f;
        public Camera backgroundCamera;
        public Transform recipeCloth;
        public Transform[] decorations;
        public Vector2[] boardAnchors;
        public Vector3[] offsets;
        GameController game;
        Renderer[] clothRenderers;
        void Awake() { if (backgroundCamera != null) backgroundCamera.enabled = false; }

        public void Connect(GameController stage)
        {
            game = stage;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = reflection;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            Layout(stage.Board.Data.width, stage.Board.Data.height);
            SyncCamera(stage.GameCamera);
        }

        public void Layout(float width, float height)
        {
            for (int i=0;i<decorations.Length;i++)
                if (decorations[i] != null)
                    decorations[i].localPosition = offsets[i] + new Vector3(boardAnchors[i].x*width*.5f,0,boardAnchors[i].y*height*.5f);
        }

        void LateUpdate()
        {
            if (game != null) SyncCamera(game.GameCamera);
        }

        public void SyncCamera(Camera stage)
        {
            if (backgroundCamera == null || stage == null) return;
            var viewport = stage.rect;
            float size = stage.orthographicSize / Mathf.Max(.01f,viewport.height);
            float aspect = (float)Screen.width / Mathf.Max(1,Screen.height);
            backgroundCamera.orthographicSize = size;
            backgroundCamera.transform.rotation = stage.transform.rotation;
            backgroundCamera.transform.position = stage.transform.position
                + stage.transform.right*((.5f-viewport.center.x)*2*size*aspect)
                + stage.transform.up*((.5f-viewport.center.y)*2*size);
            // Both base cameras render the same table rays at the viewport border.
            // This avoids URP's unsupported Depth-only base-camera clear mode.
            backgroundCamera.enabled = true;
            AlignRecipeCloth();
        }

        void AlignRecipeCloth()
        {
            if (recipeCloth == null) return;
            clothRenderers ??= recipeCloth.GetComponentsInChildren<Renderer>();
            if (clothRenderers.Length == 0) return;
            Rect target = UI.KitchenLayout.Viewport(UI.KitchenLayout.RecipeCloth, Screen.width, Screen.height);
            Rect current = ClothViewport();
            if (current.width <= 0 || current.height <= 0) return;
            // Uniform scale retains the authored mesh, folds and checks; no extra cloth image is drawn.
            float fit = Mathf.Min(target.width / current.width, target.height / current.height);
            recipeCloth.localScale *= fit;
            current = ClothViewport();
            var table = new Plane(Vector3.up, recipeCloth.position);
            Ray from = backgroundCamera.ViewportPointToRay(current.center);
            Ray to = backgroundCamera.ViewportPointToRay(target.center);
            if (table.Raycast(from, out float fromDistance) && table.Raycast(to, out float toDistance))
                recipeCloth.position += to.GetPoint(toDistance) - from.GetPoint(fromDistance);
        }

        Rect ClothViewport()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var renderer in clothRenderers)
            {
                Bounds bounds = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector2 point = backgroundCamera.WorldToViewportPoint(renderer.transform.TransformPoint(corner));
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
