using BetoBeto.Core;
using BetoBeto.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BetoBeto.Player
{
    public sealed class GhostController : MonoBehaviour
    {
        public float moveSpeed = 5.5f;
        GameController game;
        Vector2Int facing = Directions.Down;
        public void Initialize(GameController controller) { game = controller; }
        void Update()
        {
            if (game == null || game.Session.State != GameState.Playing) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x--;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x++;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y++;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y--;
            if (input.sqrMagnitude > 0)
            {
                Vector2 move = input.normalized;
                transform.position += new Vector3(move.x, 0, move.y) * (moveSpeed * game.Feedback.SimulationDelta);
                facing = Mathf.Abs(input.x) > Mathf.Abs(input.y) ? new Vector2Int((int)Mathf.Sign(input.x), 0) : new Vector2Int(0, -(int)Mathf.Sign(input.y));
            }
            var data = game.Board.Data;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, -(data.width - 1) * .5f, (data.width - 1) * .5f);
            position.z = Mathf.Clamp(position.z, -(data.height - 1) * .5f, (data.height - 1) * .5f);
            transform.position = position;
            var cell = data.Cell(position);
            var target = cell + facing;
            bool onBoard = false;
            var mouse = Mouse.current;
            if (mouse != null && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var screen = mouse.position.ReadValue();
                if (game.GameCamera.pixelRect.Contains(screen))
                {
                    var ray = game.GameCamera.ScreenPointToRay(screen);
                    if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
                    {
                        target = data.Cell(ray.GetPoint(distance));
                        onBoard = data.Contains(target);
                    }
                }
            }
            game.ShowPlacement(target, onBoard || keyboard.eKey.isPressed);
            if ((mouse != null && onBoard && mouse.leftButton.wasPressedThisFrame) || keyboard.eKey.wasPressedThisFrame)
                game.TryPlaceIce(target);
            if (keyboard.spaceKey.wasPressedThisFrame || (mouse != null && onBoard && mouse.rightButton.wasPressedThisFrame))
                game.TryPlaceDrool(cell);
        }
    }
}
