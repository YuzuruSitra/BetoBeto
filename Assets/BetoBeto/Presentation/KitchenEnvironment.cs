using BetoBeto.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    public sealed class KitchenEnvironment : MonoBehaviour
    {
        public Cubemap reflection;
        public Color ambient = new Color(.48f,.54f,.61f);
        public float reflectionIntensity = .8f;
        public Camera backgroundCamera;
        public Transform[] decorations;
        public Vector2[] boardAnchors;
        public Vector3[] offsets;
        GameController game;
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
        }
    }
}
