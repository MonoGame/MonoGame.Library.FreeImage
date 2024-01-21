using System.Runtime.InteropServices;

namespace BuildScripts;

[TaskName("Build macOS")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildLibraryTask))]
public sealed class BuildMacOSTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

    public override void Run(BuildContext context)
    {
        // disable neon instructions in case we're building for arm64
        var cflags = "-Wno-implicit-function-declaration -O3 -fPIC -fexceptions -fvisibility=hidden -DPNG_ARM_NEON_OPT=0";

        var makeFilePattern = "freeimage/Makefile.*";
        context.ReplaceRegexInFiles(makeFilePattern, @"SHAREDLIB.+\=.+", "SHAREDLIB = lib$(TARGET).dylib");
        context.ReplaceTextInFiles(makeFilePattern, "cp *.so Dist/", "cp *.dylib Dist/");
        context.ReplaceTextInFiles(makeFilePattern, "$(CC) -s -shared -Wl,-soname,$(VERLIBNAME) $(LDFLAGS) -o $@ $(MODULES) $(LIBRARIES)", "$(CXX) -dynamiclib -install_name $(LIBNAME) -current_version $(VER_MAJOR).$(VER_MINOR) -compatibility_version $(VER_MAJOR) $(LDFLAGS) -o $@ $(MODULES)");

        if (context.IsUniversalBinary)
        {
            // generate x86_64 and arm64 at once
            context.ReplaceTextInFiles(makeFilePattern, "COMPILERFLAGS_X86_64 = -arch x86_64", "COMPILERFLAGS_X86_64 = -arch x86_64 -arch arm64");
            context.ReplaceTextInFiles(makeFilePattern, "$(LIBTOOL) -arch_only x86_64 -o $@ $(MODULES_X86_64)", "$(LIBTOOL) -o $@ $(MODULES_X86_64)");
            context.ReplaceTextInFiles(makeFilePattern, "$(CPP_X86_64) -arch x86_64 -dynamiclib $(LIBRARIES_X86_64) -o $@ $(MODULES_X86_64)", "$(CPP_X86_64) -arch x86_64 -arm arm64 -dynamiclib $(LIBRARIES_X86_64) -o $@ $(MODULES_X86_64)");
            // remove lipo
            context.ReplaceTextInFiles(makeFilePattern, "$(SHAREDLIB): $(SHAREDLIB)-i386 $(SHAREDLIB)-x86_64", "");
            context.ReplaceTextInFiles(makeFilePattern, "$(LIPO) -create $(SHAREDLIB)-i386 $(SHAREDLIB)-x86_64 -output $(SHAREDLIB)", "");
        }

        var buildWorkingDir = "freeimage/";
        var env = new Dictionary<string, string>
        {
            { "CFLAGS", cflags }
        };
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "-f Makefile.gnu", EnvironmentVariables = env });
        context.CopyFile(@"freeimage/Dist/libfreeimage.dylib", $"{context.ArtifactsDir}/libfreeimage.dylib");
    }
}
