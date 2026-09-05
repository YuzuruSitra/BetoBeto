using System.Collections.Generic;
using UnityEngine;

namespace BetoBeto.Stage
{
    public sealed class StageBoard
    {
        public StageData Data { get; }
        public readonly List<Vector2Int> Pipes = new List<Vector2Int>();
        public readonly HashSet<Vector2Int> Walls = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> Shredders = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> Exits = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, float> Drool = new Dictionary<Vector2Int, float>();
        public readonly HashSet<Vector2Int> Jellies = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> Freezers = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, int> Scones = new Dictionary<Vector2Int, int>();
        public readonly Dictionary<Vector2Int, CookieState> Cookies = new Dictionary<Vector2Int, CookieState>();
        public readonly List<MovingShredderState> Movers = new List<MovingShredderState>();
        public readonly Dictionary<Vector2Int, StageObject> Objects = new Dictionary<Vector2Int, StageObject>();
        public Vector2Int PlayerStart { get; private set; }

        public StageBoard(StageLayout layout)
        {
            Data = layout.Read();
            PlayerStart = Data.Find('G')[0];
            // Use actual scene placement, so scene-level layout edits are respected at runtime.
            foreach (var item in layout.GetComponentsInChildren<StageObject>())
            {
                var cell = Data.Cell(item.transform.position);
                Objects[cell] = item;
                switch (item.kind)
                {
                    case StageObjectKind.Wall: Walls.Add(cell); break;
                    case StageObjectKind.Pipe: Pipes.Add(cell); break;
                    case StageObjectKind.Shredder: Shredders.Add(cell); break;
                    case StageObjectKind.Exit: Exits.Add(cell); break;
                    case StageObjectKind.PlayerStart: PlayerStart = cell; break;
                    case StageObjectKind.Jelly: Jellies.Add(cell); break;
                    case StageObjectKind.Cookie: Cookies[cell] = new CookieState(Data.cookieHits, Data.cookieRespawnSeconds); break;
                    case StageObjectKind.Freezer: Freezers.Add(cell); break;
                    case StageObjectKind.Scone: Scones[cell] = GimmickRules.QuarterTurn(item.transform); break;
                    case StageObjectKind.MovingShredder:
                        Movers.Add(new MovingShredderState(cell, GimmickRules.Rotate(Vector2Int.right, GimmickRules.QuarterTurn(item.transform)))); break;
                }
            }
        }
        public bool Blocked(Vector2Int cell) => Walls.Contains(cell) || Jellies.Contains(cell) || Scones.ContainsKey(cell)
            || (Cookies.TryGetValue(cell, out var cookie) && !cookie.Broken);
        public bool BlocksSliding(Vector2Int cell) => Blocked(cell) && !Scones.ContainsKey(cell);
        public bool HasShredder(Vector2Int cell)
        {
            if (Shredders.Contains(cell)) return true;
            foreach (var mover in Movers) if (mover.OccupiedCell == cell) return true;
            return false;
        }
        public bool MoverReserves(Vector2Int cell, MovingShredderState except = null)
        {
            foreach (var mover in Movers) if (mover != except && mover.Reserves(cell)) return true;
            return false;
        }
        public Vector3 MoverWorld(Vector2 position) => Data.World(Vector2Int.zero) + new Vector3(position.x, 0, -position.y);
        public bool TouchesShredder(Vector2Int cell, Vector3 point)
        {
            if (Shredders.Contains(cell)) return true;
            var p = new Vector2(point.x, point.z);
            foreach (var mover in Movers)
            {
                Vector3 a = MoverWorld(mover.Previous), b = MoverWorld(mover.Position);
                if (GimmickRules.SweepCircle(new Vector2(a.x, a.z), new Vector2(b.x, b.z), p, GimmickRules.BladeRadius + .001f, out _)) return true;
            }
            return false;
        }
        // Fruits recognise the blades while walking; a slide cannot steer around them.
        public bool BlocksWalking(Vector2Int cell) => Blocked(cell) || Shredders.Contains(cell) || MoverReserves(cell);
        public bool CanPlace(Vector2Int cell) => Data.Contains(cell) && !Blocked(cell) && !HasShredder(cell) && !Exits.Contains(cell) && !Pipes.Contains(cell);
    }
}
