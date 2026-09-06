using UnityEditor;
using UnityEngine;

namespace BetoBeto.Editor
{
    public sealed class KitchenUiAssetImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/BetoBeto/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = assetPath.Contains("/Hud") ? 1024 : 2048;
        }
    }
}
