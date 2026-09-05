using BetoBeto.Core;
using BetoBeto.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    public sealed class FruitMotionVfx : MonoBehaviour
    {
        GameController game;
        FruitAgent fruit;
        Transform visual;
        Vector3 restScale;
        readonly TrailRenderer[] trails = new TrailRenderer[2];
        float impact, dropTimer;
        public void Initialize(GameController controller, FruitAgent actor, Transform model)
        {
            game = controller; fruit = actor; visual = model;
            if (visual != null) restScale = visual.localScale;
            for (int i = 0; i < trails.Length; i++)
            {
                var go = new GameObject("Drool speed trail");
                go.transform.SetParent(transform, false);
                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = game.assets.effectMaterial;
                trail.time = .23f; trail.minVertexDistance = .045f;
                trail.widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
                trail.widthMultiplier = i == 0 ? .26f : .13f;
                trail.startColor = new Color(.55f, 1, .88f, .72f);
                trail.endColor = new Color(.85f, 1, .94f, 0);
                trail.numCapVertices = 3;
                trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
                trail.emitting = false;
                trails[i] = trail;
            }
        }
        public void StartSlide() { impact = .55f; }
        public void Impact(float power) { impact = power; }
        void LateUpdate()
        {
            if (fruit.Removed || game.Session.State == GameState.Paused) return;
            float dt = game.Feedback.SimulationDelta;
            Vector3 side = Vector3.Cross(fruit.Forward, Vector3.up);
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] == null) continue;
                trails[i].transform.localPosition = Vector3.up * .13f + side * (i == 0 ? -.2f : .2f) - fruit.Forward * .14f;
                trails[i].emitting = fruit.Sliding;
                trails[i].time = .22f + Mathf.Min(fruit.Chain - 1, 5) * .025f;
            }
            if (dt <= 0) return;
            impact = Mathf.MoveTowards(impact, 0, dt * 3.6f);
            if (visual != null)
            {
                float stretch = fruit.Sliding ? .17f : 0;
                float pulse = impact * .3f;
                var scale = new Vector3(1 + stretch + pulse, 1 - stretch - pulse, 1 + stretch + pulse);
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.Scale(restScale, scale), dt * 23);
                var lean = fruit.Sliding ? Quaternion.Euler(fruit.Forward.z * 17, 0, -fruit.Forward.x * 17) : Quaternion.identity;
                visual.localRotation = Quaternion.Slerp(visual.localRotation, lean, dt * 15);
            }
            if (!fruit.Sliding) { dropTimer = 0; return; }
            dropTimer -= dt;
            if (dropTimer <= 0)
            {
                dropTimer = .065f;
                game.Feedback.TrailDrop(transform.position - fruit.Forward * .3f, fruit.Forward);
            }
        }
        public void DetachTrails()
        {
            foreach (var trail in trails)
            {
                if (trail == null) continue;
                trail.emitting = false;
                trail.transform.SetParent(game.FeedbackRoot, true);
                Destroy(trail.gameObject, .5f);
            }
        }
    }
}
