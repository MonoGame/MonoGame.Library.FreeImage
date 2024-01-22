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
        // we need to patch the source to be compatible with the xcode 64bit compiler

        // ZLib
        context.ReplaceTextInFiles(
            "freeimage/Source/ZLib/gzlib.c",
            "/* Local functions */",
            "#ifdef __APPLE__\r\n    #define _LARGEFILE64_SOURCE     /* See feature_test_macros(7) */\r\n    #include <sys/types.h>\r\n    #include <unistd.h>\r\n#endif");
        context.ReplaceTextInFiles(
            "freeimage/Source/ZLib/gzguts.h",
            "#ifdef _LARGEFILE64_SOURCE",
            "#ifdef __APPLE__\r\n    #include <unistd.h>\r\n#endif\r\n\r\n#ifdef _LARGEFILE64_SOURCE");
        // LibJXR
        context.ReplaceTextInFiles(
            "freeimage/Source/LibJXR/image/decode/segdec.c",
            "#include \"strcodec.h\"",
            "#ifdef __APPLE__\r\n    #include <libkern/OSByteOrder.h>\r\n    #define _byteswap_ulong(x) _OSSwapInt32\r\n#endif\r\n\r\n#include \"strcodec.h\"");
        context.ReplaceTextInFiles(
            "freeimage/Source/LibJXR/jxrgluelib/JXRGlueJxr.c",
            "#include <limits.h>",
            "#ifdef __APPLE__\r\n    #include <wchar.h>\r\n#endif\r\n\r\n#include <limits.h>");

        // we need to modify the makefile to produce a dynamic library

        var makeFilePattern = "freeimage/Makefile.osx";
        // disable neon instructions in case we're building for arm64
        context.ReplaceTextInFiles(
            makeFilePattern,
            "COMPILERFLAGS = -Os -fexceptions -fvisibility=hidden -DNO_LCMS -D__ANSI__",
            "COMPILERFLAGS = -Os -fexceptions -fvisibility=hidden -DNO_LCMS -D__ANSI__ -DDISABLE_PERF_MEASUREMENT -DPNG_ARM_NEON_OPT=0");
        // rename output to libFreeImage.dylib
        context.ReplaceTextInFiles(
            makeFilePattern,
            "SHAREDLIB = lib$(TARGET)-$(VER_MAJOR).$(VER_MINOR).dylib",
            "SHAREDLIB = lib$(TARGET).dylib");
        // build only dynamic library
        context.ReplaceTextInFiles(
            makeFilePattern,
            "FreeImage: $(STATICLIB)",
            "FreeImage: $(SHAREDLIB)");
        context.ReplaceTextInFiles(
            makeFilePattern,
            "cp *.a Dist",
            "cp *.dylib Dist");

        // generate x86_64 and arm64 at once
        if (context.IsUniversalBinary)
        {
            context.ReplaceTextInFiles(
                makeFilePattern, 
                "COMPILERFLAGS_X86_64 = -arch x86_64",
                "COMPILERFLAGS_X86_64 = -arch x86_64 -arch arm64");
            
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
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "-f Makefile.osx"});
        context.CopyFile(@"freeimage/Dist/libFreeImage.dylib", $"{context.ArtifactsDir}/libfreeimage.dylib");
    }
}
