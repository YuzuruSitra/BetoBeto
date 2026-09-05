using System;
using UnityEngine;

namespace BetoBeto.Stage
{
    public static class GimmickRules
    {
        public const string Symbols = ".#PXEGJCHV1234F";
        public const float BladeRadius = .62f;
        public const int SconeMaxHits = 20;
        public static bool IsShredder(char c) => c == 'X' || c == 'H' || c == 'V';
        public static bool IsScone(char c) => c >= '1' && c <= '4';
        // Cookies and scones can be broken to open a route.
        public static bool BlocksConnectivity(char c) => c == '#' || c == 'J' || c == 'P';
        public static int QuarterTurn(Transform prop) => ((Mathf.RoundToInt(prop.eulerAngles.y / 90) % 4) + 4) % 4;
        public static Vector2Int Rotate(Vector2Int direction, int turns)
        {
            for (int i = 0; i < ((turns % 4) + 4) % 4; i++) direction = new Vector2Int(-direction.y, direction.x);
            return direction;
        }
        // The unrotated triangle fills the north-west half of its tile; the slope faces south-east.
        public static bool HitsSconeSide(Vector2Int incoming, int turns)
        {
            Vector2Int normal = Rotate(Vector2Int.one, turns);
            return incoming.x * normal.x + incoming.y * normal.y > 0;
        }
        public static Vector2Int SconeReflection(Vector2Int incoming, int turns)
        {
            Vector2Int normal = Rotate(Vector2Int.one, turns);
            int dot = incoming.x * normal.x + incoming.y * normal.y;
            return dot < 0 ? incoming - dot * normal : -incoming;
        }
        public static bool SweepCircle(Vector2 from, Vector2 to, Vector2 centre, float radius, out float fraction)
        {
            Vector2 delta = to - from, offset = from - centre;
            float c = offset.sqrMagnitude - radius * radius;
            fraction = 0;
            if (c <= 0) return true;
            float a = delta.sqrMagnitude;
            if (a < .000001f) return false;
            float b = Vector2.Dot(offset, delta), discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            fraction = (-b - Mathf.Sqrt(discriminant)) / a;
            return fraction >= 0 && fraction <= 1;
        }
    }

    public sealed class CookieState
    {
        public int MaxHits { get; }
        public int HitsLeft { get; private set; }
        public float RespawnSeconds { get; }
        public float Remaining { get; private set; }
        public bool Broken => HitsLeft == 0;
        public CookieState(int hits, float seconds) { MaxHits = HitsLeft = hits; RespawnSeconds = seconds; }
        public bool Hit()
        {
            if (Broken) return false;
            HitsLeft--;
            if (Broken) Remaining = RespawnSeconds;
            return Broken;
        }
        public bool Tick(float dt, bool occupied)
        {
            if (!Broken || dt <= 0) return false;
            Remaining = Mathf.Max(0, Remaining - dt);
            if (Remaining > 0 || occupied) return false;
            HitsLeft = MaxHits;
            return true;
        }
    }

    public sealed class MovingShredderState
    {
        public Vector2Int Start { get; }
        public Vector2Int Cell { get; private set; }
        public Vector2Int Target { get; private set; }
        public Vector2Int Direction { get; private set; }
        public Vector2 Position => Vector2.Lerp(Cell, Target, progress);
        public Vector2 Previous { get; private set; }
        public Vector2Int OccupiedCell => Vector2Int.RoundToInt(Position);
        float progress;
        public MovingShredderState(Vector2Int start, Vector2Int direction)
        {
            Start = Cell = Target = start; Direction = direction; Previous = start;
        }
        public bool Reserves(Vector2Int cell) => cell == Cell || cell == Target;
        public bool Tick(float dt, float speed, Func<Vector2Int, bool> blocked)
        {
            Previous = Position;
            if (dt <= 0) return false;
            bool reversed = false;
            float budget = dt * speed;
            for (int step = 0; step < 64 && budget > .00001f; step++)
            {
                if (Cell == Target)
                {
                    var next = Cell + Direction;
                    if (blocked(next))
                    {
                        Direction = -Direction; reversed = true;
                        next = Cell + Direction;
                        if (blocked(next)) break;
                    }
                    Target = next; progress = 0;
                }
                float travel = Mathf.Min(budget, 1 - progress);
                progress += travel; budget -= travel;
                if (progress >= .99999f) { Cell = Target; progress = 0; }
            }
            return reversed;
        }
    }
}
