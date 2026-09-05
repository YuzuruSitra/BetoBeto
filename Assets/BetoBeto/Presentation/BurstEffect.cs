using UnityEngine;

namespace BetoBeto.Presentation
{
    public sealed class BurstEffect : MonoBehaviour
    {
        Vector3 velocity;
        float age;
        float lifetime;
        Vector3 initialScale;
        public void Initialize(Vector3 speed, float life) { velocity = speed; lifetime = life; initialScale = transform.localScale; }
        void Update()
        {
            if (Core.GameController.Instance != null && Core.GameController.Instance.Session != null && Core.GameController.Instance.Session.State == Core.GameState.Paused) return;
            age += Time.deltaTime;
            velocity += Vector3.down * (4 * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(125 * Time.deltaTime, 60 * Time.deltaTime, 0);
            transform.localScale = initialScale * Mathf.Max(0, 1 - age / lifetime);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
