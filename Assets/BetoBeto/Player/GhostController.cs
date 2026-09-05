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
        public Vector2Int Facing { get; private set; } = Directions.Down;
        public Vector2Int Cell => game.Board.Data.Cell(transform.position);
        public Vector2Int IceTarget => Cell + Facing;
        public void Initialize(GameController controller)
        {
            game = controller;
            visualFacing = gameObject.AddComponent<ActorFacing>();
            visualFacing.Initialize(game.assets.effectMaterial, true);
        }
        void Update()
        {
            if (game == null) return;
            if (game.Session.State != GameState.Playing || !GamepadControls.Ready)
            {
                game.ShowPlacement(IceTarget, false); return;
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
            game.ShowPlacement(IceTarget, true);
            if (GamepadControls.IcePressed) game.TryPlaceIce(IceTarget);
            if (GamepadControls.DroolPressed) game.TryPlaceDrool(Cell);
        }
    }
}
