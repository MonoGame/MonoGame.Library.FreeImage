using System.Runtime.InteropServices;

namespace BuildScripts;

[TaskName("Publish Library")]
[IsDependentOn(typeof(PrepTask))]
public sealed class PublishLibraryTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.BuildSystem().IsRunningOnGitHubActions;

    public override async Task RunAsync(BuildContext context)
    {
        var rid = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            rid = "windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            rid = "osx";
        else
            rid = "linux";
        rid += RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm or Architecture.Arm64 => "-arm64",
            _ => "-x64",
        };

        await context.BuildSystem().GitHubActions.Commands.UploadArtifact(DirectoryPath.FromString(context.ArtifactsDir), $"artifacts-{rid}");
    }
}
