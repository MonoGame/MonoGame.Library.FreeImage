
namespace BuildScripts;

[TaskName("Test macOS")]
public sealed class TestMacOSTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

    public override void Run(BuildContext context)
    {
        foreach (var filePath in context.GetFiles(context.ArtifactsDir))
        {
            context.Information($"Checking: {filePath}");
            context.StartProcess(
                "dyld_info",
                new ProcessSettings
                {
                    Arguments = $"-dependents {filePath}",
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

            context.Information("");
        }
    }
}
