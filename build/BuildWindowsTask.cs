
using Cake.Common.Tools.MSBuild;

namespace BuildScripts;

[TaskName("Build Windows")]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        context.CleanDirectory(context.ArtifactsDir);

        MSBuildSettings buildSettings = new()
        {
            Verbosity = Verbosity.Normal,
            Configuration = "Release",
            PlatformTarget = PlatformTarget.x64
        };

        context.MSBuild(@"freeimage\FreeImage.2017.sln", buildSettings);
        context.CopyFile(@"freeimage\Dist\x64\Freeimage.dll", context.ArtifactsDir + @"\FreeImage.dll");
    }
}
