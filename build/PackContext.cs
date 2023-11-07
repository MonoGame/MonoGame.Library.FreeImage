
namespace BuildScripts;

public class PackContext
{
    public string LibraryName { get; }

    public string LicensePath { get; }

    public string? RepositoryOwner { get; }

    public string? RepositoryUrl { get; }

    public string Version { get; }

    public PackContext(ICakeContext context)
    {
        LibraryName = context.Arguments("libraryname", "X").FirstOrDefault()!;
        LicensePath = context.Arguments("licensepath", "").FirstOrDefault()!;
        Version = "1.0.0";

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            RepositoryOwner = context.EnvironmentVariable("GITHUB_REPOSITORY_OWNER");
            RepositoryUrl = $"https://github.com/{context.EnvironmentVariable("GITHUB_REPOSITORY")}";
            Version = context.EnvironmentVariable("VERSION") + "." + context.EnvironmentVariable("GITHUB_RUN_NUMBER");
        }
    }
}
