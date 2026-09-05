using System;
using UnityEditor;
using UnityEngine;

namespace BetoBeto.Editor
{
    public static class GhostDroolVfxSetup
    {
        public const string MaterialPath = "Assets/BetoBeto/Art/Characters/CuteGhost/Materials/DroolLiquid.mat";
        public static Material EnsureMaterial()
        {
            var shader = Shader.Find("BetoBeto/Drool Liquid");
            if (shader == null) throw new InvalidOperationException("Drool Liquid shader missing");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Clear refractive drool" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            return material;
        }
    }
}
