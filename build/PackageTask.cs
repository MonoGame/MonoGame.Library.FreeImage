
using Cake.Common.Build;

namespace BuildScripts;

[TaskName("Package")]
public sealed class PackageTask : AsyncFrostingTask<BuildContext>
{
    private static async Task<string> ReadEmbeddedResourceAsync(string resourceName)
    {
        await using var stream = typeof(PackageTask).Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task SaveEmbeddedResourceAsync(string resourceName, string outPath)
    {
        if (File.Exists(outPath))
            File.Delete(outPath);

        await using var stream = typeof(PackageTask).Assembly.GetManifestResourceStream(resourceName)!;
        await using var writer = File.Create(outPath);
        await stream.CopyToAsync(writer);
        writer.Close();
    }

    public override async Task RunAsync(BuildContext context)
    {
        var requiredRids = new[] {
            "windows-x64",
            "osx-x64",
            "osx-arm64",
            "linux-x64"
        };

        // Download built artifacts
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            foreach (var rid in requiredRids)
            {
                var directoryPath = $"runtimes/{rid}/native";
                if (context.DirectoryExists(directoryPath))
                    continue;

                context.CreateDirectory(directoryPath);
                await context.BuildSystem().GitHubActions.Commands.DownloadArtifact($"artifacts-{rid}", directoryPath);
            }
        }

        // Generate Project
        var projectData = await ReadEmbeddedResourceAsync("MonoGame.Library.X.txt");
        projectData = projectData.Replace("{X}", "FreeImage");
        projectData = projectData.Replace("{LicencePath}", @"freeimage\license-fi.txt");
        projectData = projectData.Replace("{LicenceName}", "LICENSE.txt");

        var librariesToInclude = from rid in requiredRids from filePath in Directory.GetFiles($"runtimes/{rid}/native") select $"<Content Include=\"{filePath}\" />";
        projectData = projectData.Replace("{LibrariesToInclude}", string.Join(Environment.NewLine, librariesToInclude));

        await File.WriteAllTextAsync("MonoGame.Library.FreeImage.csproj", projectData);
        await SaveEmbeddedResourceAsync("Icon.png", "Icon.png");

        // Build
        var dnMsBuildSettings = new DotNetMSBuildSettings();
        dnMsBuildSettings.WithProperty("Version", context.Version);
        dnMsBuildSettings.WithProperty("RepositoryUrl", context.RepositoryUrl);
        
        context.DotNetPack("MonoGame.Library.FreeImage.csproj", new DotNetPackSettings
        {
            MSBuildSettings = dnMsBuildSettings,
            Verbosity = DotNetVerbosity.Minimal,
            Configuration = "Release"
        });

        // Upload Artifacts
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            foreach (var nugetPath in context.GetFiles("bin/Release/*.nupkg"))
            {
                await context.BuildSystem().GitHubActions.Commands.UploadArtifact(nugetPath, nugetPath.GetFilename().ToString());
                context.DotNetNuGetPush(nugetPath, new()
                {
                    ApiKey = context.EnvironmentVariable("GITHUB_TOKEN"),
                    Source = $"https://nuget.pkg.github.com/{context.RepositoryOwner}/index.json"
                });
            }
        }
    }
}
