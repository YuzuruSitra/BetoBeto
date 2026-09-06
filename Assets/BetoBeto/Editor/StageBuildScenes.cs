using System.Collections.Generic;
using System.IO;
using BetoBeto.Core;
using UnityEditor;
using UnityEngine;

namespace BetoBeto.Editor
{
    [InitializeOnLoad]
    public static class StageBuildScenes
    {
        static StageBuildScenes()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if(state==PlayModeStateChange.ExitingEditMode)Synchronize();
            };
        }

        [MenuItem("BetoBeto/Synchronize Stage Build Scenes")]
        public static void Synchronize()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<StageCatalog>("Assets/BetoBeto/Resources/StageCatalog.asset");
            if(catalog==null||catalog.stages==null)return;
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed=false;
            foreach(var entry in catalog.stages)
            {
                if(entry==null||string.IsNullOrWhiteSpace(entry.sceneName))continue;
                string path=entry.sceneName.StartsWith("Assets/")?entry.sceneName:"Assets/BetoBeto/Scenes/"+entry.sceneName;
                if(!path.EndsWith(".unity"))path+=".unity";
                if(!File.Exists(path))
                {
                    Debug.LogError("ステージ一覧に対応するシーンが見つかりません: "+path,catalog);
                    continue;
                }
                int index=scenes.FindIndex(s=>s.path==path);
                if(index<0){scenes.Add(new EditorBuildSettingsScene(path,true));changed=true;}
                else if(!scenes[index].enabled){scenes[index]=new EditorBuildSettingsScene(path,true);changed=true;}
            }
            // Preserve the title scene's position and all unrelated build entries.
            if(changed)EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
