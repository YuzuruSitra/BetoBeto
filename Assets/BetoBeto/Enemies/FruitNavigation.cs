using System;
using BetoBeto.Core;
using BetoBeto.Stage;
using UnityEngine;

namespace BetoBeto.Enemies
{
    public static class FruitNavigation
    {
        public static Vector2Int ChooseFlee(Vector2Int cell, Vector2Int source, Vector2Int forward, Func<Vector2Int, bool> blocked, bool keepDistance = true)
        {
            Vector2Int delta = cell - source;
            var candidates = new[] { forward, Directions.Left(forward), Directions.Right(forward), -forward };
            Vector2Int best = Vector2Int.zero;
            int bestScore = int.MinValue;
            foreach (var direction in candidates)
            {
                int outward = delta.x * direction.x + delta.y * direction.y;
                // A tap explicitly sends overlapping fruit in the player's facing direction.
                if (blocked(cell + direction) || (keepDistance && outward < 0)) continue;
                if (direction == forward) return forward;
                if (outward > bestScore) { bestScore = outward; best = direction; }
            }
            return best;
        }
        // Keep heading straight until an obstacle blocks the next tile.
        public static Vector2Int Choose(FruitKind kind, Vector2Int cell, Vector2Int forward,
            bool preferLeft, Func<Vector2Int, bool> blocked, Func<Vector2Int, int> visits)
        {
            if (!blocked(cell + forward)) return forward;
            return ChooseTurn(cell, forward, preferLeft, blocked);
        }
        public static Vector2Int ChooseTurn(Vector2Int cell, Vector2Int forward,
            bool preferLeft, Func<Vector2Int, bool> blocked)
        {
            var first = preferLeft ? Directions.Left(forward) : Directions.Right(forward);
            var second = -first;
            if (!blocked(cell + first)) return first;
            if (!blocked(cell + second)) return second;
            if (!blocked(cell - forward)) return -forward;
            return Vector2Int.zero;
        }
    }
}
