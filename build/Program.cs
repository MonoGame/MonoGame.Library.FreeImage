
namespace BuildScripts;

public static class Program
{
    public static int Main(string[] args)
        => new CakeHost()
            .UseWorkingDirectory("../")
            .UseContext<BuildContext>()
            .Run(args);
}
