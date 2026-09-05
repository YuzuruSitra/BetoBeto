using UnityEngine;

namespace BetoBeto.Presentation
{
    public sealed class ActorVisual : MonoBehaviour
    {
        public bool ghost;
        public bool spinning;
        Vector3 rest;
        float phase;
        float animationTime;
        Enemies.FruitAgent fruit;
        void Awake() { rest = transform.localPosition; phase = Random.value * 6.28f; fruit = GetComponentInParent<Enemies.FruitAgent>(); }
        void Update()
        {
            if (Core.GameController.Instance != null && Core.GameController.Instance.Session != null &&
                Core.GameController.Instance.Session.State == Core.GameState.Paused) return;
            if (spinning) { transform.Rotate(0, 240 * Time.deltaTime, 0, Space.Self); return; }
            animationTime += Time.deltaTime * (fruit != null && fruit.IsFrozen ? .35f : 1);
            float t = animationTime * (ghost ? 2.5f : 8) + phase;
            transform.localPosition = rest + Vector3.up * (ghost ? .07f * Mathf.Sin(t) : .035f * Mathf.Abs(Mathf.Sin(t)));
            if (ghost) transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * .6f) * 3);
        }
    }
}
