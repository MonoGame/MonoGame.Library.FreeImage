
namespace BuildScripts;

[TaskName("Test Linux")]
public sealed class TestLinuxTask : FrostingTask<BuildContext>
{
    private static readonly string[] ValidLibs = {
        "linux-vdso.so",
        "libstdc++.so",
        "libgcc_s.so",
        "libc.so",
        "libm.so",
        "/lib/ld-linux-",
        "/lib64/ld-linux-"
    };

    public override bool ShouldRun(BuildContext context) => context.IsRunningOnLinux();

    public override void Run(BuildContext context)
    {
        foreach (var filePath in context.GetFiles(context.ArtifactsDir))
        {
            context.Information($"Checking: {filePath}");
            context.StartProcess(
                "ldd",
                new ProcessSettings
                {
                    Arguments = $"{filePath}",
                    RedirectStandardOutput = true
                },
                out IEnumerable<string> processOutput);

            foreach (var line in processOutput)
            {
                var libPath = line.Trim().Split(' ')[0];
                context.Information($"DEP: {libPath}");

                var isValidLib = false;
                foreach (var validLib in ValidLibs)
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

            context.Information("");
        }
    }
}
