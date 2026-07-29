using System;
using System.IO;
using System.Linq;
using UnityEditor;

public static class CodexWebGLBuildRunner
{
    public static void Build()
    {
        var outputPath = GetOutputPath();
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in Build Settings.");
        }

        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
        }

        Console.WriteLine($"CODEx_WEBGL_BUILD_OUTPUT={outputPath}");
        Console.WriteLine($"CODEx_WEBGL_BUILD_SIZE={report.summary.totalSize}");
    }

    private static string GetOutputPath()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "-codexBuildOutput", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(args[i + 1]))
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        return Path.GetFullPath("Builds/CodexWebGLVerify");
    }
}
