using Cake.Common.Build;

namespace BuildScripts;

public class BuildContext : FrostingContext
{
    public string ArtifactsDir { get; }

    public string Version { get; }

    public string RepositoryUrl { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        var versionRegex = @"PRODUCTVERSION (\d+),(\d+),(\d+),\d+";
        var major = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 1, System.Text.RegularExpressions.RegexOptions.Singleline);
        var minor = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        var patch = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        ArtifactsDir = context.Arguments("artifactsDir", "artifacts").FirstOrDefault();
        Version = $"{major}.{minor}.{patch}";
        RepositoryUrl = "https://github.com/MonoGame/MonoGame.Library.FreeImage";
        
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            Version += "." + context.EnvironmentVariable("GITHUB_RUN_NUMBER");
            RepositoryUrl = $"https://github.com/{context.EnvironmentVariable("GITHUB_REPOSITORY")}";

            context.BuildSystem().GitHubActions.Commands.SetSecret(context.EnvironmentVariable("GITHUB_TOKEN"));
        }
    }
}

[TaskName("Prepare Build")]
public sealed class PrepTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectory(context.ArtifactsDir);
        context.CreateDirectory(context.ArtifactsDir);
    }
}
