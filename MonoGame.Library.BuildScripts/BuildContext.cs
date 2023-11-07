
namespace BuildScripts;

public class BuildContext : FrostingContext
{
    public string ArtifactsDir { get; }

    public PackContext PackContext { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        ArtifactsDir = context.Arguments("artifactsDir", "artifacts").FirstOrDefault()!;
        PackContext = new PackContext(context);

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            context.BuildSystem().GitHubActions.Commands.SetSecret(context.EnvironmentVariable("GITHUB_TOKEN"));
        }
    }
}
