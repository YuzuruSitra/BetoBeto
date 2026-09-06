using Betobeto.Fruits;
using BetoBeto.Core;
using BetoBeto.Enemies;
using UnityEngine;

namespace BetoBeto.Presentation
{
    /// <summary>Connects the imported fruit's expression and in-place motion to gameplay.</summary>
    [DisallowMultipleComponent]
    public sealed class FruitModelVisual : MonoBehaviour
    {
        [Range(0, 35)] public float viewTiltDegrees = 15;
        static readonly int Motion = Animator.StringToHash("Motion");
        FruitAgent fruit;
        FruitExpressionSwitcher expression;
        Animator animator;
        ActorViewTilt viewTilt;
        bool? sliding;
        int slideMotion, currentMotion = -1;
        Quaternion restRotation;
        float spinSpeed, spinAngle;
        Vector3 restPosition, restScale;
        float reboundAge = 1;

        void Awake()
        {
            restRotation = transform.localRotation;
            restPosition = transform.localPosition; restScale = transform.localScale;
            fruit = GetComponentInParent<FruitAgent>();
            expression = GetComponentInChildren<FruitExpressionSwitcher>(true);
            animator = GetComponentInChildren<Animator>(true);
            if (animator != null) animator.applyRootMotion = false;
            if (animator != null) viewTilt = ActorViewTilt.Create(animator.transform, viewTiltDegrees);
            Refresh();
        }

        void LateUpdate()
        {
            Refresh();
            var game = GameController.Instance;
            float dt = game != null && game.Feedback != null ? game.Feedback.SimulationDelta : Time.deltaTime;
            bool paused = game != null && game.Session != null && game.Session.State != GameState.Playing;
            if (!paused && reboundAge < .34f)
            {
                reboundAge = Mathf.Min(.34f, reboundAge + dt);
                float stretch = reboundAge < .065f ? -.24f * (1 - reboundAge / .065f)
                    : Mathf.Sin((reboundAge - .065f) / .275f * Mathf.PI * 2) * .18f * (1 - (reboundAge - .065f) / .275f);
                transform.localScale = Vector3.Scale(restScale, new Vector3(1 - stretch * .45f, 1 + stretch, 1 - stretch * .45f));
                transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(reboundAge / .34f * Mathf.PI) * .18f);
                if (reboundAge >= .34f) { transform.localScale = restScale; transform.localPosition = restPosition; }
            }
            if (animator == null || spinSpeed == 0 || animator.speed == 0) return;
            spinAngle = Mathf.Repeat(spinAngle + spinSpeed * dt * animator.speed, 360);
            // Only the visual rotates; navigation and the facing marker keep their travel direction.
            transform.localRotation = Quaternion.AngleAxis(spinAngle, Vector3.up) * restRotation;
        }

        public void Rebound() { reboundAge = 0; }

        public void Refresh()
        {
            if (fruit == null) return;
            bool next = fruit.Sliding && !fruit.Removed;
            if (sliding != next)
            {
                sliding = next;
                // Pick once per slide: 1 = supine, 2 = prone. Keep it through turns and pauses.
                slideMotion = next ? Random.Range(1, 3) : 0;
                spinSpeed = slideMotion == 1 ? Random.Range(120f, 360f) * (Random.value < .5f ? -1 : 1) : 0;
                spinAngle = 0;
                transform.localRotation = restRotation;
            }
            // A slide takes priority over panic running; repeated scares extend FruitAgent's timer.
            int motion = next ? slideMotion : fruit.IsFleeing && !fruit.Removed ? 3 : 0;
            // Keep supine slide spins on the vertical Y axis, without the top-down view tilt.
            if (viewTilt != null) viewTilt.degrees = motion == 1 ? 0 : viewTiltDegrees;
            if (currentMotion != motion)
            {
                currentMotion = motion;
                if (expression != null) expression.SetExpressionIndex(motion == 0 ? 0 : 1);
                if (animator != null) animator.SetInteger(Motion, motion);
            }
            if (animator == null) return;
            var game = GameController.Instance;
            bool paused = game != null && game.Session != null &&
                (game.Session.State != GameState.Playing || (game.Feedback != null && game.Feedback.HitStopped));
            animator.speed = paused || fruit.IsStunned ? 0 : fruit.IsFrozen ? .35f : 1;
        }
    }
}
