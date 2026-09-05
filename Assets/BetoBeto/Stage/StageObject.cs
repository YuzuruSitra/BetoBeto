using UnityEngine;

namespace BetoBeto.Stage
{
    public enum StageObjectKind { Wall, Pipe, Shredder, Exit, PlayerStart }
    public sealed class StageObject : MonoBehaviour
    {
        public StageObjectKind kind;
    }
}
