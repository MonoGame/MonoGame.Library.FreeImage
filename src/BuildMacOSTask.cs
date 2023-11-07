using System.Runtime.InteropServices;

namespace BuildScripts;

[TaskName("Build macOS")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildMacOSTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

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
    }
}
