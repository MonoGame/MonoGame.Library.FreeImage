using System.Collections;
using System.Runtime.InteropServices;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Microsoft.VisualBasic;

namespace BuildScripts;

[TaskName("Build macOS")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildMacOSTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

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
        context.ReplaceRegexInFiles(makeFilePattern, @"SHAREDLIB.+\=.+", "SHAREDLIB = lib$(TARGET).dylib");
        context.ReplaceTextInFiles(makeFilePattern, "cp *.so Dist/", "cp *.dylib Dist/");
        context.ReplaceTextInFiles(makeFilePattern, "$(CC) -s -shared -Wl,-soname,$(VERLIBNAME) $(LDFLAGS) -o $@ $(MODULES) $(LIBRARIES)", "$(CXX) -dynamiclib -install_name $(LIBNAME) -current_version $(VER_MAJOR).$(VER_MINOR) -compatibility_version $(VER_MAJOR) $(LDFLAGS) -o $@ $(MODULES)");

        var buildWorkingDir = "freeimage/";
        var env = new Dictionary<string, string>
        {
            { "CFLAGS", cflags }
        };
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "-f Makefile.gnu", EnvironmentVariables = env });

        context.CopyFile(@"freeimage/Dist/libfreeimage.dylib", $"{context.ArtifactsDir}/libfreeimage.dylib");

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            await context.BuildSystem().GitHubActions.Commands.UploadArtifact(DirectoryPath.FromString("artifcats"), "FreeImage-macos-latest");
        }
    }
}

[TaskName("Test macOS")]
[IsDependentOn(typeof(BuildMacOSTask))]
public sealed class TestMacOSTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

    public override void Run(BuildContext context)
    {
        context.StartProcess(
            "dyld_info",
            new ProcessSettings
            {
                Arguments = $"-dependents {context.ArtifactsDir}/libfreeimage.dylib",
                RedirectStandardOutput = true
            },
            out IEnumerable<string> processOutput);

        var processOutputList = processOutput.ToList();
        for (int i = 3; i < processOutputList.Count; i++)
        {
            var libPath = processOutputList[i].Trim();
            context.Information($"DEP: {libPath}");
            if (libPath.StartsWith("/usr/lib/"))
                continue;

            throw new Exception($"Found a dynamic library ref: {libPath}");
        }
    }
}
