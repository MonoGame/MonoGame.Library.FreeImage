
using Cake.Common.Build;

namespace BuildScripts;

[TaskName("Package")]
public sealed class PackageTask : AsyncFrostingTask<BuildContext>
{
    public override async Task RunAsync(BuildContext context)
    {
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            Directory.CreateDirectory("artifacts-windows-x64");
            Directory.CreateDirectory("artifacts-macos");
            Directory.CreateDirectory("artifacts-linux-x64");

            await context.BuildSystem().GitHubActions.Commands.DownloadArtifact("FreeImage-windows-latest", "artifacts-windows-x64");
            await context.BuildSystem().GitHubActions.Commands.DownloadArtifact("FreeImage-macos-latest", "artifacts-macos");
            await context.BuildSystem().GitHubActions.Commands.DownloadArtifact("FreeImage-ubuntu-20.04", "artifacts-linux-x64");
        }

        var dnMsBuildSettings = new DotNetMSBuildSettings();
        dnMsBuildSettings.WithProperty("Version", context.Version);
        dnMsBuildSettings.WithProperty("RepositoryUrl", context.RepositoryUrl);
        
        context.DotNetPack("src/MonoGame.Library.FreeImage.csproj", new DotNetPackSettings
        {
            MSBuildSettings = dnMsBuildSettings,
            Verbosity = DotNetVerbosity.Minimal,
            Configuration = "Release"
        });
    }
}
