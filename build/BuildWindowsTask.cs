using Cake.Common.Tools.MSBuild;

namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildLibraryTask))]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        //  Disable openmp so there is no dependency on VCOMP140.dll
        context.ReplaceTextInFiles("freeimage/**/*LibRawLite.2017.vcxproj", "<OpenMPSupport>true</OpenMPSupport>", "<OpenMPSupport>false</OpenMPSupport>");

        MSBuildSettings buildSettings = new()
        {
            Verbosity = Verbosity.Normal,
            Configuration = "Release",
            PlatformTarget = PlatformTarget.x64
        };
        buildSettings.WithProperty("WindowsTargetPlatformVersion", "10.0.17763.0");
        buildSettings.WithProperty("PlatformToolset", "v143");
        context.MSBuild("freeimage/FreeImage.2017.sln", buildSettings);
        context.CopyFile("freeimage/Dist/x64/Freeimage.dll", $"{context.ArtifactsDir}/FreeImage.dll");
    }
}
