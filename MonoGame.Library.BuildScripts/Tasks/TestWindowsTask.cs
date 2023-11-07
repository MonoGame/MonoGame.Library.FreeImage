using Cake.Common.Tools.VSWhere.Latest;

namespace BuildScripts;

[TaskName("Test Windows")]
public sealed class TestWindowsTask : FrostingTask<BuildContext>
{
    private static readonly string[] ValidLibs = {
        "WS2_32.dll",
        "KERNEL32.dll"
    };

    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        var vswhere = new VSWhereLatest(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        var devcmdPath = vswhere.Latest(new VSWhereLatestSettings()).FullPath + @"\Common7\Tools\vsdevcmd.bat";

        foreach (var filePath in context.GetFiles(context.ArtifactsDir))
        {
            context.Information($"Checking: {filePath}");
            context.StartProcess(
                devcmdPath,
                new ProcessSettings()
                {
                    Arguments = $"& dumpbin /dependents /nologo {filePath}",
                    RedirectStandardOutput = true
                },
                out IEnumerable<string> processOutput
            );

            foreach (string output in processOutput)
            {
                var libPath = output.Trim();
                if (!libPath.EndsWith(".dll"))
                    continue;
                context.Information($"DEP: {libPath}");
                if (ValidLibs.Contains(libPath))
                    continue;

                throw new Exception($"Found a dynamic library ref: {libPath}");
            }
        }
    }
}
