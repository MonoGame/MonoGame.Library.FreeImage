
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
        
        buildSettings.WithProperty("WindowsTargetPlatformVersion", "10.0.17763.0");
        buildSettings.WithProperty("PlatformToolset", "v141");

        context.MSBuild("freeimage/FreeImage.2017.sln", buildSettings);
        context.CopyFile("freeimage/Dist/x64/Freeimage.dll", $"{context.ArtifactsDir}/FreeImage.dll");
    }
}
