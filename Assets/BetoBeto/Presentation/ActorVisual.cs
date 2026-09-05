using UnityEngine;

namespace BetoBeto.Presentation
{
    public sealed class ActorVisual : MonoBehaviour
    {
        public bool ghost;
        public bool spinning;
        Vector3 rest;
        float phase;
        void Awake() { rest = transform.localPosition; phase = Random.value * 6.28f; }
        void Update()
        {
            if (Core.GameController.Instance != null && Core.GameController.Instance.Session != null &&
                Core.GameController.Instance.Session.State == Core.GameState.Paused) return;
            if (spinning) { transform.Rotate(0, 240 * Time.deltaTime, 0, Space.Self); return; }
            float t = Time.time * (ghost ? 2.5f : 8) + phase;
            transform.localPosition = rest + Vector3.up * (ghost ? .07f * Mathf.Sin(t) : .035f * Mathf.Abs(Mathf.Sin(t)));
            if (ghost) transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * .6f) * 3);
        }
    }
}
