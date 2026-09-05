using UnityEngine;
using UnityEngine.InputSystem;

namespace BetoBeto.Player
{
    /// <summary>Physical button positions keep the controls consistent across pad brands.</summary>
    public static class GamepadControls
    {
        static bool suppressActions;
        public static bool BrowserReady { get; private set; }
        public static Gamepad Device => Gamepad.current != null && Gamepad.current.enabled ? Gamepad.current : null;
        public static bool Connected => Device != null;
        public static bool Ready => BrowserReady && Connected;
        public static Vector2 Move => !Ready ? Vector2.zero : DeadZone(Device.dpad.ReadValue().sqrMagnitude > .1f ? Device.dpad.ReadValue() : Device.leftStick.ReadValue(), .18f);
        public static Vector2 Aim => !Ready ? Vector2.zero : DeadZone(Device.rightStick.ReadValue(), .3f);
        public static bool PausePressed => Ready && Device.startButton.wasPressedThisFrame;
        public static bool CancelPressed => Ready && Device.buttonEast.wasPressedThisFrame;
        public static bool DroolPressed => ActionsReady && Device.buttonSouth.wasPressedThisFrame;
        public static bool IcePressed => ActionsReady && Device.buttonWest.wasPressedThisFrame;
        static bool ActionsReady
        {
            get
            {
                if (!Ready) return false;
                if (suppressActions && !Device.buttonSouth.isPressed && !Device.buttonWest.isPressed && !Device.buttonEast.isPressed && !Device.startButton.isPressed)
                    suppressActions = false;
                return !suppressActions;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            BrowserReady = Application.platform != RuntimePlatform.WebGLPlayer;
            suppressActions = false;
        }
        public static void UnlockBrowser() { BrowserReady = true; SuppressActionsUntilRelease(); }
        public static void SuppressActionsUntilRelease() { suppressActions = true; }
        static Vector2 DeadZone(Vector2 value, float threshold) => value.sqrMagnitude < threshold * threshold ? Vector2.zero : Vector2.ClampMagnitude(value, 1);
        public static Vector2Int Direction(Vector2 stick, Vector2Int previous)
        {
            if (stick.sqrMagnitude < .01f) return previous;
            float x = Mathf.Abs(stick.x), y = Mathf.Abs(stick.y);
            // Keep the previous axis near a diagonal, instead of flickering between tiles.
            bool horizontal = Mathf.Abs(x - y) < .12f ? previous.x != 0 : x > y;
            return horizontal ? new Vector2Int(stick.x >= 0 ? 1 : -1, 0) : new Vector2Int(0, stick.y >= 0 ? -1 : 1);
        }
    }
}
