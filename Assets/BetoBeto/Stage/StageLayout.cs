using UnityEngine;

namespace BetoBeto.Stage
{
    /// <summary>Serialized layout remains editable in Unity after importing JSON.</summary>
    public sealed class StageLayout : MonoBehaviour
    {
        [TextArea(8, 20)] public string sourceJson;
        public Transform tiles;
        public Transform placements;
        public StageData Read() => StageData.Parse(sourceJson);
    }
}
