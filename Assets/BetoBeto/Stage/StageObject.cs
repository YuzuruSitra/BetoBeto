using UnityEngine;

namespace BetoBeto.Stage
{
    public enum StageObjectKind { Wall, Pipe, Shredder, Exit, PlayerStart, Jelly, Cookie, MovingShredder, Scone, Freezer }
    public sealed class StageObject : MonoBehaviour
    {
        public StageObjectKind kind;
    }
}
