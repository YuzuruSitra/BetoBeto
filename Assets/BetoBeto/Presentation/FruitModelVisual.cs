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

        void Awake()
        {
            restRotation = transform.localRotation;
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
            if (animator == null || spinSpeed == 0 || animator.speed == 0) return;
            var game = GameController.Instance;
            float dt = game != null && game.Feedback != null ? game.Feedback.SimulationDelta : Time.deltaTime;
            spinAngle = Mathf.Repeat(spinAngle + spinSpeed * dt * animator.speed, 360);
            // Only the visual rotates; navigation and the facing marker keep their travel direction.
            transform.localRotation = Quaternion.AngleAxis(spinAngle, Vector3.up) * restRotation;
        }

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
