using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZiplaKac.Editor
{
    public static class BuildPipelineScript
    {
        public static void BuildIOS()
        {
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            string buildPath = "iOS_Build";

            Debug.Log($"[BuildPipelineScript] Starting iOS build to '{buildPath}'...");

            PlayerSettings.companyName = "YusufYildirim";
            PlayerSettings.productName = "ZiplaKac";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.yusufyldrm.ziplakac");
            PlayerSettings.iOS.appleDeveloperTeamID = "29367Z6APG";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildPipelineScript] Result: {summary.result}, Total Time: {summary.totalTime.TotalSeconds:F1}s, Errors: {summary.totalErrors}");

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[BuildPipelineScript] iOS Build succeeded!");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildPipelineScript] iOS Build failed with {summary.totalErrors} errors!");
                EditorApplication.Exit(1);
            }
        }
    }
}
