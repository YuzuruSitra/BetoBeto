using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BetoBeto.Editor
{
    public static class WindowsBuild
    {
        public const string Output="Builds/Windows64/BetoBeto.exe";
        [Serializable] sealed class BuildSummary
        {
            public string result, target, output, error;
            public string[] scenes;
            public int errors,warnings;
            public long bytes;
            public double seconds;
        }

        [MenuItem("BetoBeto/Build/Windows 64-bit")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new BuildFailedException("Stop Play before building.");
            StageBuildScenes.Synchronize();
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(scenes.Length==0||!scenes[0].EndsWith("/Title.unity"))throw new BuildFailedException("Title must be the first enabled build scene.");
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var summary=new BuildSummary {result="Building",target="StandaloneWindows64",output=Path.GetFullPath(Output),scenes=scenes};
            string reportPath=Path.Combine(Path.GetDirectoryName(Output),"build-result.json");
            File.WriteAllText(reportPath,JsonUtility.ToJson(summary,true));
            try
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=scenes,locationPathName=Output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                summary.result=report.summary.result.ToString();summary.errors=(int)report.summary.totalErrors;
                summary.warnings=(int)report.summary.totalWarnings;summary.bytes=(long)report.summary.totalSize;
                summary.seconds=report.summary.totalTime.TotalSeconds;
                File.WriteAllText(reportPath,JsonUtility.ToJson(summary,true));
                if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Windows 64-bit build failed; see "+reportPath);
                Debug.Log("Windows 64-bit build ready: "+Output);
            }
            catch(Exception e)
            {
                summary.result="Failed";summary.error=e.Message;File.WriteAllText(reportPath,JsonUtility.ToJson(summary,true));
                throw;
            }
        }
    }
}
