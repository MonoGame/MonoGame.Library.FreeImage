
using System.Diagnostics;
using Cake.Common.Tools.Command;
using Cake.Common.Build;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.VSWhere;
using Cake.Common.Tools.VSWhere.Latest;

namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
public sealed class BuildWindowsTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override async Task RunAsync(BuildContext context)
    {
        MSBuildSettings buildSettings = new()
        {
            Verbosity = Verbosity.Normal,
            Configuration = "Release",
            PlatformTarget = PlatformTarget.x64
        };

        buildSettings.WithProperty("WindowsTargetPlatformVersion", "10.0.17763.0");
        buildSettings.WithProperty("PlatformToolset", "v141");

        //  Disable openmp so there is no dependency on VCOMP140.dll
        context.ReplaceTextInFiles("freeimage/**/*LibRawLite.2017.vcxproj", "<OpenMPSupport>true</OpenMPSupport>", "<OpenMPSupport>false</OpenMPSupport>");

        context.MSBuild("freeimage/FreeImage.2017.sln", buildSettings);
        context.CopyFile("freeimage/Dist/x64/Freeimage.dll", $"{context.ArtifactsDir}/FreeImage.dll");

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            await context.BuildSystem().GitHubActions.Commands.UploadArtifact(DirectoryPath.FromString(context.ArtifactsDir), "artifacts-windows-x64");
        }
    }
}

[TaskName("Test Windows")]
[IsDependentOn(typeof(BuildWindowsTask))]
public sealed class TestWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        var vswhere = new VSWhereLatest(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        var settings = new VSWhereLatestSettings();
        var latest = vswhere.Latest(settings);

        string devcmdPath = vswhere.Latest(new VSWhereLatestSettings()).FullPath + @"\Common7\Tools\vsdevcmd.bat";

        Console.WriteLine(devcmdPath);

        context.StartProcess(devcmdPath, new ProcessSettings()
        {
            Arguments = "& dumpbin /dependents /nologo ./artifacts/FreeImage.dll",
            RedirectStandardOutput = true
        },
        out IEnumerable<string> processOutput);

        HashSet<string> validDlls = new HashSet<string>()
        {
            "WS2_32.dll",
            "KERNEL32.dll"
        };

        bool checkDll = false;
        foreach (string output in processOutput)
        {
            string line = output.Trim();

            //  Exit early when we get to the summary part
            if (line == "Summary") { break; }

            //  Check if we've hit the line where the actual dlls are listed
            if (line == "Image has the following dependencies:")
            {
                checkDll = true;
            }

            if (checkDll && line.Contains("dll") && !validDlls.Contains(line))
            {
                throw new Exception($"Found a dynamic library ref: {line}");
            }
        }
    }
}
