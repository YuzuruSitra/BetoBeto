using UnityEngine;

namespace BetoBeto.Player
{
    public static class ScareRules
    {
        public const float TapSeconds = .25f;
        public const float FullChargeSeconds = 1.5f;
        public const float FleeSeconds = 1.5f;
        public const int MaxRadius = 6;

        public static bool IsCharged(float seconds) => seconds >= TapSeconds;
        public static float Charge01(float seconds) => Mathf.Clamp01(seconds / FullChargeSeconds);
        public static int Radius(float seconds) => Mathf.Clamp(1 + Mathf.FloorToInt(
            Mathf.InverseLerp(TapSeconds, FullChargeSeconds, seconds) * (MaxRadius - 1)), 1, MaxRadius);

        public static bool Contains(Vector2Int player, Vector2Int facing, Vector2Int enemy, float seconds)
        {
            if (!IsCharged(seconds)) return enemy == player || enemy == player + facing;
            int radius = Radius(seconds);
            return (enemy - player).sqrMagnitude <= radius * radius;
        }

        public static Vector2Int Away(Vector2 delta, Vector2Int previous)
        {
            if (delta.sqrMagnitude < .0001f) return -previous;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}
