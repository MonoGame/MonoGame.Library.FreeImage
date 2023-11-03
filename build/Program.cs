
namespace BuildScripts;

public static class Program
{
    public static int Main(string[] args)
        => new CakeHost()
            .UseWorkingDirectory("../")
            .UseContext<BuildContext>()
            .Run(args);
}

[TaskName("Default")]
[IsDependentOn(typeof(BuildWindowsTask))]
[IsDependentOn(typeof(BuildMacOSTask))]
[IsDependentOn(typeof(BuildLinuxTask))]
[IsDependentOn(typeof(TestMacOSTask))]
[IsDependentOn(typeof(TestLinuxTask))]
public class DefaultTask : FrostingTask
{
}
