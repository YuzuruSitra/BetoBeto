using UnityEngine;
using UnityEngine.Rendering;

namespace BetoBeto.Presentation
{
    public sealed class ShockRing : MonoBehaviour
    {
        LineRenderer line;
        Color tint;
        float start, end, lifetime, age;
        public void Initialize(Material material, Color color, float from, float to, float life)
        {
            start = from; end = to; lifetime = life; tint = color;
            line = gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false; line.loop = true;
            line.positionCount = 40; line.widthMultiplier = .12f;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2 / 40;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }
            Draw(0);
        }
        void Update()
        {
            if (Core.GameController.Instance != null && Core.GameController.Instance.Session.State == Core.GameState.Paused) return;
            age += Time.deltaTime;
            Draw(Mathf.Clamp01(age / lifetime));
            if (age >= lifetime) Destroy(gameObject);
        }
        void Draw(float progress)
        {
            float ease = 1 - (1 - progress) * (1 - progress);
            transform.localScale = Vector3.one * Mathf.Lerp(start, end, ease);
            var color = tint; color.a *= 1 - progress;
            line.startColor = line.endColor = color;
            line.widthMultiplier = Mathf.Lerp(.11f, .015f, progress);
        }
    }
}
