
using Cake.Common.Tools.MSBuild;

namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        MSBuildSettings buildSettings = new()
        {
            Verbosity = Verbosity.Normal,
            Configuration = "Release",
            PlatformTarget = PlatformTarget.x64
        };

        context.MSBuild("freeimage/FreeImage.2017.sln", buildSettings);
        context.CopyFile("freeimage/Dist/x64/Freeimage.dll", $"{context.ArtifactsDir}/FreeImage.dll");
    }
}
