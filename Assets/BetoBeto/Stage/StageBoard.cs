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
        public Vector2Int PlayerStart { get; private set; }

        public StageBoard(StageLayout layout)
        {
            Data = layout.Read();
            PlayerStart = Data.Find('G')[0];
            // Use actual scene placement, so scene-level layout edits are respected at runtime.
            foreach (var item in layout.GetComponentsInChildren<StageObject>())
            {
                var cell = Data.Cell(item.transform.position);
                switch (item.kind)
                {
                    case StageObjectKind.Wall: Walls.Add(cell); break;
                    case StageObjectKind.Pipe: Pipes.Add(cell); break;
                    case StageObjectKind.Shredder: Shredders.Add(cell); break;
                    case StageObjectKind.Exit: Exits.Add(cell); break;
                    case StageObjectKind.PlayerStart: PlayerStart = cell; break;
                }
            }
        }
        public bool Blocked(Vector2Int cell) => Walls.Contains(cell);
        // Fruits recognise the blades while walking; a slide cannot steer around them.
        public bool BlocksWalking(Vector2Int cell) => Blocked(cell) || Shredders.Contains(cell);
        public bool CanPlace(Vector2Int cell) => Data.Contains(cell) && !Blocked(cell) && !Shredders.Contains(cell) && !Exits.Contains(cell) && !Pipes.Contains(cell);
    }
}
