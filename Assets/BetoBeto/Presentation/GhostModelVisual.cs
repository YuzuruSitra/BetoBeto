using BetoBeto.Core;
using BetoBeto.Player;
using UnityEngine;

namespace BetoBeto.Presentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class GhostModelVisual : MonoBehaviour
    {
        [Min(.05f)] public float spookSeconds = .5f;
        [Range(0, 35)] public float viewTiltDegrees = 15;
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int Drool = Animator.StringToHash("Drool");
        static readonly int Drooling = Animator.StringToHash("Drooling");
        static readonly int Spook = Animator.StringToHash("Spook");
        static readonly int Spooking = Animator.StringToHash("Spooking");
        Animator animator;
        GameController game;
        GhostController ghost;
        float spookRemaining;
        bool wasDrooling;
        GhostDroolVfx droolVfx;
        Quaternion restRotation;
        ActorViewTilt viewTilt;
        public bool IsDrooling => ghost != null && ghost.IsDrooling;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            droolVfx = GetComponent<GhostDroolVfx>();
            restRotation = transform.localRotation;
            if (animator != null) animator.applyRootMotion = false;
            if (animator != null) viewTilt = ActorViewTilt.Create(animator.transform, viewTiltDegrees);
        }

        public void Initialize(GameController controller, GhostController player)
        {
            game = controller;
            ghost = player;
        }

        void Update()
        {
            if (animator == null || game == null) return;
            bool paused = game.Session.State != GameState.Playing || !GamepadControls.Ready || game.Feedback.HitStopped;
            animator.speed = paused ? 0 : 1;
            if (paused) return;
            float dt = game.Feedback.SimulationDelta;
            bool held = ghost.IsDrooling;
            if (held && !wasDrooling)
            {
                spookRemaining = 0;
                animator.ResetTrigger(Spook);
                animator.SetTrigger(Drool);
            }
            if (!held) animator.ResetTrigger(Drool);
            wasDrooling = held;
            spookRemaining = Mathf.Max(0, spookRemaining - dt);
            animator.SetBool(Spooking, spookRemaining > 0);
            animator.SetBool(Drooling, held);
            animator.SetFloat(Speed, ghost.MoveSpeed01, .1f, dt);
        }

        void LateUpdate()
        {
            if (animator == null || game == null) return;
            bool paused = game.Session.State != GameState.Playing || !GamepadControls.Ready || game.Feedback.HitStopped;
            if (paused) return;
            if (ghost.IsDrooling && game.GameCamera != null)
            {
                // Turn only the visual: movement, grid-facing and scare targeting remain stable.
                Vector3 direction = game.GameCamera.orthographic ? -game.GameCamera.transform.forward :
                    game.GameCamera.transform.position - transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > .0001f)
                    transform.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            }
            else transform.localRotation = restRotation;
            // Update the mouth's tilted world position before spawning drool this frame.
            if (viewTilt != null) viewTilt.Apply();
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool mouthOpen = state.IsName("Yodare") || (state.IsName("YODAREStart") && state.normalizedTime >= .65f);
            if (droolVfx != null) droolVfx.Tick(game.Feedback.SimulationDelta, ghost.IsDrooling && mouthOpen);
        }

        public void PlaySpook()
        {
            if (animator == null || IsDrooling) return;
            spookRemaining = spookSeconds;
            animator.ResetTrigger(Drool);
            animator.SetBool(Drooling, false);
            animator.SetBool(Spooking, true);
            animator.SetTrigger(Spook);
        }
    }
}
