using System.Runtime.InteropServices;
using Cake.Common.Build;
using Cake.Common.Diagnostics;

namespace BuildScripts;

[TaskName("Build Linux")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildLinuxTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnLinux();

    public override async Task RunAsync(BuildContext context)
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

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            await context.BuildSystem().GitHubActions.Commands.UploadArtifact(DirectoryPath.FromString(context.ArtifactsDir), "FreeImage-ubuntu-20.04");
        }
    }
}

[TaskName("Test Linux")]
[IsDependentOn(typeof(BuildLinuxTask))]
public sealed class TestLinuxTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnLinux();

    public override void Run(BuildContext context)
    {
        context.StartProcess(
            "ldd",
            new ProcessSettings
            {
                Arguments = $"{context.ArtifactsDir}/libfreeimage.so",
                RedirectStandardOutput = true
            },
            out IEnumerable<string> processOutput);

        var validLibs = new List<string>
        {
            "linux-vdso.so",
            "libstdc++.so",
            "libgcc_s.so",
            "libc.so",
            "libm.so",
            "/lib/ld-linux-",
            "/lib64/ld-linux-"
        };
        foreach (var line in processOutput)
        {
            var libPath = line.Trim().Split(' ')[0];
            context.Information($"DEP: {libPath}");

            var isValidLib = false;
            foreach (var validLib in validLibs)
            {
                if (libPath.StartsWith(validLib))
                {
                    isValidLib = true;
                    break;
                }
            }

            if (!isValidLib)
                throw new Exception($"Found a dynamic library ref: {libPath}");
        }
    }
}
