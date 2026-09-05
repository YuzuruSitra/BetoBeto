using UnityEngine;

namespace BetoBeto.Core
{
    [CreateAssetMenu(menuName = "BetoBeto/Game Assets")]
    public sealed class GameAssets : ScriptableObject
    {
        public GameObject tile;
        public GameObject wall;
        public GameObject pipe;
        public GameObject shredder;
        public GameObject jelly;
        public GameObject cookie;
        public GameObject movingShredder;
        public GameObject scone;
        public GameObject freezer;
        public GameObject iceWall;
        public Material jellyMaterial;
        public Material cookieMaterial;
        public Material frostMaterial;
        public GameObject exit;
        public GameObject playerStart;
        public GameObject ghost;
        public GameObject ice;
        public GameObject drool;
        public GameObject[] fruits = new GameObject[4];
        public Material[] fruitMaterials = new Material[4];
        public Material sparkleMaterial;
        public Material placementMaterial;
        public Material effectMaterial;
        public Material droolMaterial;
    }
}
