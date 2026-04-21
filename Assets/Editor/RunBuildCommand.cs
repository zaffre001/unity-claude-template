#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.Editor
{
    // Invoked from the CLI via:
    //   Unity -batchmode -quit -executeMethod Project.Editor.RunBuildCommand.Build
    //         -buildTarget <Target> -outputPath <dir>
    //
    // Called by scripts/run.sh (the /run skill). Not meant for interactive use.
    public static class RunBuildCommand
    {
        public static void Build()
        {
            string outputPath = GetArg("-outputPath");
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[RunBuild] -outputPath is required");
                EditorApplication.Exit(1);
                return;
            }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError(
                    "[RunBuild] No enabled scenes in EditorBuildSettings. " +
                    "Add at least one scene via File > Build Settings, or via " +
                    "EditorBuildSettings.scenes at edit time.");
                EditorApplication.Exit(1);
                return;
            }

            string fileName = GetDefaultFileName(target);
            string locationPath = string.IsNullOrEmpty(fileName)
                ? outputPath
                : Path.Combine(outputPath, fileName);

            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = target,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(
                    "[RunBuild] SUCCESS: " + locationPath +
                    " | size=" + summary.totalSize + " bytes" +
                    ", duration=" + summary.totalTime +
                    ", errors=" + summary.totalErrors);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError(
                    "[RunBuild] FAILED: " + summary.result +
                    ", errors=" + summary.totalErrors);
                EditorApplication.Exit(1);
            }
        }

        static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }

        static string[] GetEnabledScenes()
        {
            var list = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.enabled) list.Add(s.path);
            }
            return list.ToArray();
        }

        static string GetDefaultFileName(BuildTarget target)
        {
            string productName = PlayerSettings.productName.Replace(" ", "");
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return productName + ".exe";
                case BuildTarget.StandaloneOSX:
                    return productName + ".app";
                case BuildTarget.StandaloneLinux64:
                    return productName;
                case BuildTarget.Android:
                    return productName + ".apk";
                case BuildTarget.WebGL:
                case BuildTarget.iOS:
                    // These targets expect a directory, not a file
                    return string.Empty;
                default:
                    return productName;
            }
        }
    }
}
#endif
