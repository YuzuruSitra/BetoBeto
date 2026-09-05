using BetoBeto.Core;
using BetoBeto.Presentation;
using BetoBeto.Stage;
using UnityEngine;

namespace BetoBeto.Player
{
    public sealed class GhostController : MonoBehaviour
    {
        public float moveSpeed = 5.5f;
        GameController game;
        ActorFacing visualFacing;
        ScarePreview preview;
        bool fullChargeFeedback;
        public Vector2Int Facing { get; private set; } = Directions.Down;
        public Vector2Int Cell => game.Board.Data.Cell(transform.position);
        public Vector2Int ScareTarget => Cell + Facing;
        public bool IsCharging { get; private set; }
        public float ChargeSeconds { get; private set; }
        public float Charge01 => ScareRules.Charge01(ChargeSeconds);
        public int ScareRadius => ScareRules.Radius(ChargeSeconds);
        public void Initialize(GameController controller)
        {
            game = controller;
            visualFacing = gameObject.AddComponent<ActorFacing>();
            visualFacing.Initialize(game.assets.effectMaterial, true);
            preview = gameObject.AddComponent<ScarePreview>();
            preview.Initialize(game, this);
        }
        void Update()
        {
            if (game == null) return;
            if (game.Session.State != GameState.Playing || !GamepadControls.Ready)
            {
                CancelScare(); return;
            }
            Vector2 input = GamepadControls.Move;
            if (input.sqrMagnitude > 0)
            {
                transform.position += new Vector3(input.x, 0, input.y) * (moveSpeed * game.Feedback.SimulationDelta);
                Facing = GamepadControls.Direction(input, Facing);
            }
            Facing = GamepadControls.Direction(GamepadControls.Aim, Facing);
            visualFacing.Face(Facing);
            var data = game.Board.Data;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, -(data.width - 1) * .5f, (data.width - 1) * .5f);
            position.z = Mathf.Clamp(position.z, -(data.height - 1) * .5f, (data.height - 1) * .5f);
            transform.position = position;
            if (GamepadControls.ScarePressed)
            {
                IsCharging = true; ChargeSeconds = 0; fullChargeFeedback = false;
            }
            if (IsCharging)
            {
                if (GamepadControls.ScareHeld)
                {
                    ChargeSeconds = Mathf.Min(ScareRules.FullChargeSeconds, ChargeSeconds + game.Feedback.SimulationDelta);
                    if (Charge01 >= 1 && !fullChargeFeedback)
                    {
                        fullChargeFeedback = true;
                        game.Feedback.ScareReady(transform.position);
                    }
                }
                else
                {
                    if (GamepadControls.ScareReleased) game.TryScare(transform.position, Facing, ChargeSeconds);
                    CancelScare();
                }
            }
            if (GamepadControls.DroolPressed) game.TryPlaceDrool(Cell);
        }
        public void CancelScare()
        {
            IsCharging = false; ChargeSeconds = 0; fullChargeFeedback = false;
        }
    }
}
