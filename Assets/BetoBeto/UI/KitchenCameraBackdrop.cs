using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.UI
{
    /// <summary>Extends the illustrated wood through the live camera opening, behind all board geometry.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class KitchenCameraBackdrop : MonoBehaviour
    {
        Camera view;
        Transform surface;
        Material material;
        void Awake()
        {
            view = GetComponent<Camera>();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Illustrated wood behind board";
            Destroy(quad.GetComponent<Collider>());
            surface = quad.transform; surface.SetParent(transform, false);
            material = new Material(Resources.Load<Shader>("UI/KitchenWood"));
            material.mainTexture = Resources.Load<Texture2D>("UI/KitchenBackdrop");
            var opening = KitchenLayout.Board;
            material.mainTextureOffset = new Vector2(opening.x / 1600, opening.y / 900);
            material.mainTextureScale = new Vector2(opening.width / 1600, opening.height / 900);
            var renderer = quad.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            LateUpdate();
        }
        void LateUpdate()
        {
            surface.localPosition = new Vector3(0, 0, view.farClipPlane - 1);
            surface.localScale = new Vector3(2 * view.orthographicSize * view.aspect, 2 * view.orthographicSize, 1);
        }
        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (surface != null) Destroy(surface.gameObject);
        }
    }
}
