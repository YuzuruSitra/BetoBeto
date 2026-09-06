using BetoBeto.Core;
using UnityEngine;

namespace BetoBeto.Presentation
{
    /// <summary>Spring motion through the gelatin's three deform bones; the dish stays still.</summary>
    public sealed class JellyBoneWobble : MonoBehaviour
    {
        public Transform[] bones;
        Quaternion[] restRotations;
        Vector3[] restScales;
        Vector2 angle, velocity;
        float clock, impactAge = 10;

        void Awake()
        {
            restRotations = new Quaternion[bones.Length]; restScales = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                restRotations[i] = bones[i].localRotation; restScales[i] = bones[i].localScale;
            }
        }
        public void Hit(Vector3 direction)
        {
            Vector3 local = transform.InverseTransformDirection(direction.normalized);
            velocity += new Vector2(local.z, -local.x) * 135;
            velocity = Vector2.ClampMagnitude(velocity, 195); impactAge = 0;
        }
        void LateUpdate()
        {
            var game = GameController.Instance;
            if (game != null && game.Session != null && game.Session.State != GameState.Playing) return;
            float dt = game != null && game.Feedback != null ? game.Feedback.SimulationDelta : Time.deltaTime;
            dt = Mathf.Min(dt, .1f); clock += dt; impactAge += dt;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * 120)); float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                velocity += (-angle * 145 - velocity * 12) * step;
                angle += velocity * step;
            }
            // Compress immediately at contact, then overshoot and settle back into shape.
            float bounce = -Mathf.Cos(impactAge * 24) * Mathf.Exp(-impactAge * 4.3f) * .22f;
            for (int i = 0; i < bones.Length; i++)
            {
                float influence = (i + 1f) / bones.Length;
                float pitch = angle.x * influence + Mathf.Sin(clock * 5.4f + i * .6f) * .65f * influence;
                float roll = angle.y * influence + Mathf.Sin(clock * 4.3f + i * .8f) * .45f * influence;
                Vector3 axisX = bones[i].parent.InverseTransformDirection(transform.right);
                Vector3 axisZ = bones[i].parent.InverseTransformDirection(transform.forward);
                bones[i].localRotation = Quaternion.AngleAxis(pitch, axisX) * Quaternion.AngleAxis(roll, axisZ) * restRotations[i];
                float stretch = bounce * influence + Mathf.Sin(clock * 6.4f + i * .7f) * .013f * influence;
                bones[i].localScale = Vector3.Scale(restScales[i], new Vector3(1 - stretch * .45f, 1 + stretch, 1 - stretch * .45f));
            }
        }
    }
}
