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
        // Normal berries always take an available corner. Visit cost breaks loops without seeking traps.
        public static Vector2Int Choose(FruitKind kind, Vector2Int cell, Vector2Int forward,
            bool preferLeft, Func<Vector2Int, bool> blocked, Func<Vector2Int, int> visits)
        {
            Vector2Int left = Directions.Left(forward), right = Directions.Right(forward);
            Vector2Int[] candidates;
            if (kind == FruitKind.Orange)
            {
                var side = preferLeft ? Vector2Int.left : Vector2Int.right;
                candidates = forward == Directions.Down
                    ? new[] { side, Directions.Down, -side, -Directions.Down }
                    : new[] { Directions.Down, side, -side, -Directions.Down };
            }
            else candidates = preferLeft ? new[] { left, right, forward, -forward } : new[] { right, left, forward, -forward };
            Vector2Int best = Vector2Int.zero;
            int bestCost = int.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                var direction = candidates[i];
                if (blocked(cell + direction)) continue;
                int cost = visits(cell + direction) * 5 + i;
                if (kind == FruitKind.Orange && direction.y < 0) cost += 30;
                if (cost < bestCost) { bestCost = cost; best = direction; }
            }
            return best;
        }
    }
}
