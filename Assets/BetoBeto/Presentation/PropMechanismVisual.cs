using BetoBeto.Core;
using UnityEngine;

namespace BetoBeto.Presentation
{
    /// <summary>Independent rotor/wheel/flap parts; never moves a gameplay root.</summary>
    public sealed class PropMechanismVisual : MonoBehaviour
    {
        public Transform blade;
        public Transform[] wheels;
        public Transform outletFlap;
        Vector3 previousPosition;
        Quaternion flapRest;
        float flapTime;
        void Awake()
        {
            previousPosition = transform.position;
            if (outletFlap != null) flapRest = outletFlap.localRotation;
        }
        public void OpenOutlet() { flapTime = .65f; }
        void Update()
        {
            var game = GameController.Instance;
            if (game != null && game.Session != null && game.Session.State != GameState.Playing) return;
            float dt = game != null && game.Feedback != null ? game.Feedback.SimulationDelta : Time.deltaTime;
            if (blade != null) blade.Rotate(0, 280 * dt, 0, Space.Self);
            float distance = Vector3.Dot(transform.position - previousPosition, transform.right);
            previousPosition = transform.position;
            if (wheels != null && Mathf.Abs(distance) < 1)
                foreach (var wheel in wheels)
                    if (wheel != null) wheel.Rotate(transform.forward, -distance / .085f * Mathf.Rad2Deg, Space.World);
            if (outletFlap != null)
            {
                flapTime = Mathf.Max(0, flapTime - dt);
                float open = Mathf.Sin(Mathf.Clamp01(flapTime / .65f) * Mathf.PI);
                // Authored local X runs from -28 to -76 degrees, always outward.
                outletFlap.localRotation = flapRest * Quaternion.Euler(open * -48, 0, 0);
            }
        }
    }
}
