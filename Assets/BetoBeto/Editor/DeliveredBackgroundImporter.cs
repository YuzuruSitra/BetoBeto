using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetoBeto.Editor
{
    public sealed class DeliveredBackgroundTextureSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(DeliveredBackgroundImporter.Root+"/Textures/",StringComparison.Ordinal)||!assetPath.Contains("_Delivered_"))return;
            var importer=(TextureImporter)assetImporter;
            bool normal=assetPath.EndsWith("_Normal.png",StringComparison.Ordinal);
            bool mask=assetPath.EndsWith("_Mask.png",StringComparison.Ordinal);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!mask;importer.mipmapEnabled=true;
            importer.maxTextureSize=mask?1024:2048;importer.anisoLevel=2;
            importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Compressed;
        }
    }

    public static class DeliveredBackgroundImporter
    {
        public const string Root="Assets/BetoBeto/Art/Environment";
        [Serializable] class Item { public string name,material,BaseColor,Normal,Mask; }
        [Serializable] class Manifest { public Item[] items; }

        [MenuItem("BetoBeto/Apply Delivered Background Models")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before importing background models.");
            var manifest=JsonUtility.FromJson<Manifest>("{\"items\":"+File.ReadAllText(Root+"/DeliveredBackground.json")+"}");
            foreach(var item in manifest.items)
            {
                Texture2D Texture(string name)
                {
                    if(string.IsNullOrEmpty(name))return null;
                    string path=Root+"/Textures/"+name;
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                var albedo=Texture(item.BaseColor);var normal=Texture(item.Normal);var mask=Texture(item.Mask);
                string materialPath=Root+"/Materials/"+item.material+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",albedo);
                material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",1);
                material.SetTexture("_MetallicGlossMap",mask);material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);
                material.SetFloat("_SmoothnessTextureChannel",0);material.SetFloat("_Surface",0);
                material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");
                EditorUtility.SetDirty(material);
                string modelPath=Root+"/Models/"+item.name+".fbx";
                AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
                importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;
                importer.importTangents=ModelImporterTangents.CalculateMikk;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),item.material),material);
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
