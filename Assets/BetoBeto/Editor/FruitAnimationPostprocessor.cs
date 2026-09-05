using UnityEditor;

namespace BetoBeto.Editor
{
    public sealed class FruitAnimationPostprocessor : AssetPostprocessor
    {
        bool IsScaredRun => assetPath.StartsWith(FruitModelImporter.Source + "/Animations/")
            && assetPath.EndsWith("_ScaredRun.fbx");

        void OnPreprocessModel()
        {
            if (!IsScaredRun) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName = "Root";
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            // Animation-only FBX must retain the rig node used by the model's mesh hierarchy.
            importer.preserveHierarchy = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
        }

        void OnPreprocessAnimation()
        {
            if (!IsScaredRun) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = "ScaredRun";
                clip.loopTime = true;
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionY = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
