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
        var cflags = "-Os -fexceptions -fvisibility=hidden -DNO_LCMS -D__ANSI__ -DDISABLE_PERF_MEASUREMENT -DPNG_ARM_NEON_OPT=0";

        var makeFilePattern = "freeimage/Makefile.osx";
        // rename output to libFreeImage.dylib
        context.ReplaceTextInFiles(
            makeFilePattern,
            "SHAREDLIB = lib$(TARGET)-$(VER_MAJOR).$(VER_MINOR).dylib",
            "SHAREDLIB = lib$(TARGET).dylib");
        context.ReplaceTextInFiles(
            makeFilePattern, 
            "cp *.so Dist/", 
            "cp *.dylib Dist/");

        if (context.IsUniversalBinary)
        {
            // generate x86_64 and arm64 at once
            context.ReplaceTextInFiles(
                makeFilePattern, 
                "COMPILERFLAGS_X86_64 = -arch x86_64",
                "COMPILERFLAGS_X86_64 = -arch x86_64 -arch arm64");
            // build only dynamic library
            context.ReplaceTextInFiles(
                makeFilePattern,
                "FreeImage: $(STATICLIB)",
                "FreeImage: $(SHAREDLIB)");
            // remove arch specification to leave it to compiler flags
            context.ReplaceTextInFiles(
                makeFilePattern, 
                "$(LIBTOOL) -arch_only x86_64 -o $@ $(MODULES_X86_64)",
                "$(LIBTOOL) -o $@ $(MODULES_X86_64)");
            context.ReplaceTextInFiles(
                makeFilePattern,
                "$(CPP_X86_64) -arch x86_64 -dynamiclib $(LIBRARIES_X86_64) -o $@ $(MODULES_X86_64)",
                "$(CPP_X86_64) -dynamiclib $(LIBRARIES_X86_64) -o $@ $(MODULES_X86_64)");
            // remove Intel 32bit
            context.ReplaceTextInFiles(
                makeFilePattern,
                "$(SHAREDLIB): $(SHAREDLIB)-i386 $(SHAREDLIB)-x86_64",
                "$(SHAREDLIB): $(SHAREDLIB)-x86_64");
            context.ReplaceTextInFiles(
                makeFilePattern,
                "$(LIPO) -create $(SHAREDLIB)-i386 $(SHAREDLIB)-x86_64 -output $(SHAREDLIB)",
                "$(LIPO) -create $(SHAREDLIB)-x86_64 -output $(SHAREDLIB)");
        }

        var buildWorkingDir = "freeimage/";
        var env = new Dictionary<string, string>
        {
            { "CFLAGS", cflags }
        };
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "-f Makefile.osx", EnvironmentVariables = env });
        context.CopyFile(@"freeimage/Dist/libFreeImage.dylib", $"{context.ArtifactsDir}/libfreeimage.dylib");
    }
}
