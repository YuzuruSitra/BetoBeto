using System;
using BetoBeto.Core;
using BetoBeto.Stage;
using UnityEngine;

namespace BetoBeto.Enemies
{
    public static class FruitNavigation
    {
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
