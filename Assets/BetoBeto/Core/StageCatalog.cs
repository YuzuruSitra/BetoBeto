using System;
using UnityEngine;

namespace BetoBeto.Core
{
    [Serializable]
    public sealed class StageEntry
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public string sceneName;
        public TextAsset layoutJson;
    }
    [CreateAssetMenu(menuName = "BetoBeto/Stage Catalog")]
    public sealed class StageCatalog : ScriptableObject
    {
        public StageEntry[] stages;
        public static StageCatalog Load() => Resources.Load<StageCatalog>("StageCatalog");
    }
}
