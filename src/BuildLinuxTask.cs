using System.Runtime.InteropServices;

namespace BuildScripts;

[TaskName("Build Linux")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildLinuxTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnLinux();

    public override void Run(BuildContext context)
    {
        var cflags = "-Wno-implicit-function-declaration";
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.Arm:
            case Architecture.Arm64:
                cflags += " -O3 -fPIC -fexceptions -fvisibility=hidden -DPNG_ARM_NEON_OPT=0";
                break;
        }

        var makeFilePattern = "freeimage/Makefile.*";
        context.ReplaceRegexInFiles(makeFilePattern, @"SHAREDLIB.+\=.+", "SHAREDLIB = lib$(TARGET).so");

        var buildWorkingDir = "freeimage/";
        var env = new Dictionary<string, string>
        {
            { "CFLAGS", cflags },
            { "CXXFLAGS", $"{cflags} -std=c++98" }
        };
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "-f Makefile.gnu", EnvironmentVariables = env });
        context.CopyFile(@"freeimage/Dist/libfreeimage.so", $"{context.ArtifactsDir}/libfreeimage.so");
    }
}
