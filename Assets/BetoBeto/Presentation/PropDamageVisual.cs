using BetoBeto.Core;
using UnityEngine;

namespace BetoBeto.Presentation
{
    /// <summary>Animates authored fracture islands. Gameplay owns durability and respawn.</summary>
    public sealed class PropDamageVisual : MonoBehaviour
    {
        public SkinnedMeshRenderer fracture;
        public float breakDuration = .38f;
        int crackIndex = -1, breakIndex = -1;
        float targetCrack, shownCrack, breakAge;
        bool broken, initialized;

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (fracture == null) fracture = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (fracture == null || fracture.sharedMesh == null) return;
            var mesh = fracture.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string key = mesh.GetBlendShapeName(i);
                if (key == "Crack" || key.EndsWith(".Crack")) crackIndex = i;
                if (key == "Break" || key.EndsWith(".Break")) breakIndex = i;
            }
            // Shape keys extend beyond the intact mesh bounds during the short burst.
            var bounds = mesh.bounds; bounds.Expand(1.5f); fracture.localBounds = bounds;
        }

        public void SetDamage(float fraction, bool isBroken)
        {
            Initialize();
            if (fracture == null) return;
            targetCrack = Mathf.Clamp01(fraction) * 85;
            if (!isBroken && (broken || fraction <= 0))
            {
                breakAge = 0; shownCrack = 0;
                if (breakIndex >= 0) fracture.SetBlendShapeWeight(breakIndex, 0);
            }
            if (isBroken && !broken) breakAge = 0;
            broken = isBroken;
            fracture.enabled = !broken || breakAge < breakDuration;
            if (fraction <= 0 && crackIndex >= 0) fracture.SetBlendShapeWeight(crackIndex, 0);
        }

        void Update()
        {
            if (fracture == null) return;
            var game = GameController.Instance;
            if (game != null && game.Session != null && game.Session.State != GameState.Playing) return;
            float dt = game != null && game.Feedback != null ? game.Feedback.SimulationDelta : Time.deltaTime;
            shownCrack = Mathf.MoveTowards(shownCrack, targetCrack, dt * 650);
            if (crackIndex >= 0) fracture.SetBlendShapeWeight(crackIndex, shownCrack);
            if (!broken) return;
            breakAge += dt;
            if (breakIndex >= 0) fracture.SetBlendShapeWeight(breakIndex, Mathf.SmoothStep(0, 100, breakAge / breakDuration));
            if (breakAge >= breakDuration) fracture.enabled = false;
        }
    }
}
