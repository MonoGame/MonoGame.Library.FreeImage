
namespace BuildScripts;

[TaskName("Package")]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var regex = @"PRODUCTVERSION (\d+),(\d+),(\d+),\d+";
        var major = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 1, System.Text.RegularExpressions.RegexOptions.Singleline);
        var minor = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        var patch = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        var version = $"{major}.{minor}.{patch}";
        var dnMsBuildSettings = new DotNetMSBuildSettings();
        dnMsBuildSettings.WithProperty("Version", version + "." + context.EnvironmentVariable("GITHUB_RUN_NUMBER"));
        dnMsBuildSettings.WithProperty("RepositoryUrl", "https://github.com/" + context.EnvironmentVariable("GITHUB_REPOSITORY"));

        var dnPackSettings = new DotNetPackSettings
        {
            MSBuildSettings = dnMsBuildSettings,
            Verbosity = DotNetVerbosity.Minimal,
            Configuration = "Release"
        };
        context.DotNetPack("src/MonoGame.Library.FreeImage.csproj", dnPackSettings);
    }
}
