using Cake.Common.Build;

namespace BuildScripts;

public class BuildContext : FrostingContext
{
    public string ArtifactsDir { get; }

    public string RepositoryOwner { get; }

    public string RepositoryUrl { get; }

    public string Version { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        var versionRegex = @"PRODUCTVERSION (\d+),(\d+),(\d+),\d+";
        var major = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 1, System.Text.RegularExpressions.RegexOptions.Singleline);
        var minor = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        var patch = context.FindRegexMatchGroupInFile("freeimage/FreeImage.rc", versionRegex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        ArtifactsDir = context.Arguments("artifactsDir", "artifacts").FirstOrDefault();
        RepositoryOwner = "MonoGame";
        RepositoryUrl = "https://github.com/MonoGame/MonoGame.Library.FreeImage";
        Version = $"{major}.{minor}.{patch}";
        
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            RepositoryOwner = context.EnvironmentVariable("GITHUB_REPOSITORY_OWNER");
            RepositoryUrl = $"https://github.com/{context.EnvironmentVariable("GITHUB_REPOSITORY")}";
            Version += "." + context.EnvironmentVariable("GITHUB_RUN_NUMBER");

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
