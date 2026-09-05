using UnityEngine;
using UnityEngine.UI;

namespace BetoBeto.Presentation
{
    public sealed class FloatingWord : MonoBehaviour
    {
        public int ScoreValue { get; set; }
        Text label;
        Vector2 start;
        Color tint;
        float age;
        void Awake()
        {
            label = GetComponent<Text>(); start = label.rectTransform.anchoredPosition; tint = label.color;
        }
        void Update()
        {
            if (Core.GameController.Instance != null && Core.GameController.Instance.Session.State == Core.GameState.Paused) return;
            age += Time.deltaTime;
            label.rectTransform.anchoredPosition = start + Vector2.up * (age * 42);
            float pop = 1 + Mathf.Sin(Mathf.Clamp01(age / .2f) * Mathf.PI) * .2f;
            label.rectTransform.localScale = Vector3.one * pop;
            var color = tint; color.a = Mathf.Clamp01((.85f - age) / .28f); label.color = color;
            if (age >= .85f) Destroy(gameObject);
        }
    }
}
