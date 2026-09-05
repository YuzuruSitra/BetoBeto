using BetoBeto.Core;
using UnityEngine;

namespace BetoBeto.Presentation
{
    /// <summary>Tilts the rendered model toward screen depth, independently of actor heading.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class ActorViewTilt : MonoBehaviour
    {
        [Range(0, 35)] public float degrees = 15;

        public static ActorViewTilt Create(Transform model, float degrees)
        {
            if (model == null) return null;
            var pivot = new GameObject("View tilt").transform;
            pivot.SetParent(model.parent, false);
            // Keep the model's authored scale, offset, and forward-axis correction.
            model.SetParent(pivot, false);
            var tilt = pivot.gameObject.AddComponent<ActorViewTilt>();
            tilt.degrees = degrees;
            tilt.Apply();
            return tilt;
        }

        void LateUpdate() => Apply();

        public void Apply()
        {
            var game = GameController.Instance;
            var camera = game != null ? game.GameCamera : Camera.main;
            Vector3 away = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up) : Vector3.forward;
            if (away.sqrMagnitude < .0001f)
                away = camera != null ? Vector3.ProjectOnPlane(camera.transform.up, Vector3.up) : Vector3.forward;
            if (away.sqrMagnitude < .0001f) return;
            var tilt = Quaternion.AngleAxis(degrees, Vector3.Cross(Vector3.up, away.normalized));
            var parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            // Apply tilt in camera/world space before facing, drooling turns, or slide spins.
            transform.localRotation = Quaternion.Inverse(parentRotation) * tilt * parentRotation;
        }
    }
}
