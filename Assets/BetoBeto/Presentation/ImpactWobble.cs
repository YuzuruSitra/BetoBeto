using UnityEngine;

namespace BetoBeto.Presentation
{
    public sealed class ImpactWobble : MonoBehaviour
    {
        Quaternion restRotation;
        Vector3 restScale, axis;
        float age = 1;
        void Awake() { restRotation = transform.localRotation; restScale = transform.localScale; }
        public void Hit(Vector3 direction) { axis = Vector3.Cross(Vector3.up, direction); age = 0; }
        void Update()
        {
            if (age >= .4f || (Core.GameController.Instance != null && Core.GameController.Instance.Session.State == Core.GameState.Paused)) return;
            age = Mathf.Min(.4f, age + Time.deltaTime);
            float pulse = Mathf.Sin(age * 34) * Mathf.Pow(1 - age / .4f, 2);
            transform.localRotation = restRotation * Quaternion.AngleAxis(pulse * 9, axis);
            transform.localScale = Vector3.Scale(restScale, new Vector3(1 + pulse * .045f, 1 - pulse * .085f, 1 + pulse * .045f));
        }
    }
}
