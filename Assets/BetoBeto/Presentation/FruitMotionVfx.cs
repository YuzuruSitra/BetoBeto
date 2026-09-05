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
        bool hasModelAnimation;
        readonly TrailRenderer[] trails = new TrailRenderer[2];
        float impact, dropTimer, fright, bounce;
        GameObject coating;
        public void Initialize(GameController controller, FruitAgent actor, Transform model)
        {
            game = controller; fruit = actor; visual = model;
            if (visual != null) restScale = visual.localScale;
            hasModelAnimation = visual != null && visual.GetComponent<FruitModelVisual>() != null;
            coating = new GameObject("Chocolate coating");
            coating.transform.SetParent(transform, false);
            coating.transform.localScale = Vector3.one * (fruit.kind == FruitKind.Blueberry ? .78f : fruit.kind == FruitKind.Melon ? 1.14f : 1);
            for (int i = 0; i < 6; i++)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                part.name = i == 0 ? "Chocolate dip" : "Drip";
                Destroy(part.GetComponent<Collider>());
                part.transform.SetParent(coating.transform, false);
                float angle = i * Mathf.PI * 2 / 5;
                part.transform.localPosition = i == 0 ? new Vector3(0, .24f, 0) : new Vector3(Mathf.Cos(angle) * .34f, .43f, Mathf.Sin(angle) * .34f);
                part.transform.localScale = i == 0 ? new Vector3(.84f, .38f, .84f) : new Vector3(.095f, .24f, .095f);
                part.GetComponent<Renderer>().sharedMaterial = game.assets.frostMaterial;
                part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            coating.SetActive(false);
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
        public void Scare() { fright = 1; }
        public void Ricochet() { bounce = .3f; impact = .9f; }
        void LateUpdate()
        {
            if (fruit.Removed || game.Session.State == GameState.Paused) return;
            if (coating != null) coating.SetActive(fruit.IsFrozen);
            float dt = game.Feedback.SimulationDelta;
            Vector3 side = Vector3.Cross(fruit.Forward, Vector3.up);
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] == null) continue;
                trails[i].transform.position = transform.position + Vector3.up * .13f + side * (i == 0 ? -.2f : .2f) - fruit.Forward * .14f;
                trails[i].emitting = fruit.Sliding;
                trails[i].time = .22f + Mathf.Min(fruit.Chain - 1, 5) * .025f;
            }
            if (dt <= 0) return;
            impact = Mathf.MoveTowards(impact, 0, dt * 3.6f);
            fright = Mathf.MoveTowards(fright, 0, dt * 3);
            bounce = Mathf.Max(0, bounce - dt);
            if (visual != null)
            {
                float stretch = fruit.Sliding ? .17f : 0;
                float pulse = impact * .3f;
                var scale = new Vector3(1 + stretch + pulse - fright * .16f, 1 - stretch - pulse + fright * .45f, 1 + stretch + pulse - fright * .16f);
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.Scale(restScale, scale), dt * 23);
                if (!hasModelAnimation)
                {
                    var lean = fruit.Sliding ? Quaternion.Euler(-17, 0, 0) : Quaternion.identity;
                    visual.localRotation = Quaternion.Slerp(visual.localRotation, lean, dt * 15);
                }
                if (hasModelAnimation) visual.localPosition = Vector3.zero;
                if (bounce > 0) visual.localPosition += Vector3.up * (Mathf.Sin(bounce / .3f * Mathf.PI) * .32f);
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
