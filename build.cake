#addin nuget:?package=Cake.FileHelpers&version=5.0.0

var target = Argument("target", "Build");
var artifactsDir = "artifacts";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("BuildWindows")
    .WithCriteria(() => IsRunningOnWindows())
    .Does(() =>
{
    CleanDirectory(artifactsDir);

    MSBuildSettings buildSettings = new MSBuildSettings()
    {
        Verbosity = Verbosity.Normal,
        Configuration = "Release",
        PlatformTarget = PlatformTarget.x64
    };

    MSBuild(@"freeimage\FreeImage.2017.sln", buildSettings);
    CopyFile(@"freeimage\Dist\x64\Freeimage.dll", artifactsDir + @"\FreeImage.dll");

});

Task("BuildMacOS")
    .WithCriteria(() => IsRunningOnMacOs())
    .Does(() =>
{

});

Task("BuildLinux")
    .WithCriteria(() => IsRunningOnLinux())
    .Does(() =>
{

});

Task("Package")
    .Does(() =>
{
    var regex = @"PRODUCTVERSION (\d+),(\d+),(\d+),\d+";
    var major = FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 1, System.Text.RegularExpressions.RegexOptions.Singleline);
    var minor = FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
    var patch = FindRegexMatchGroupInFile("freeimage/FreeImage.rc", regex, 2, System.Text.RegularExpressions.RegexOptions.Singleline);
    var version = $"{major}.{minor}.{patch}";
    
    var dnMsBuildSettings = new DotNetMSBuildSettings();
    dnMsBuildSettings.WithProperty("Version", version + "." + EnvironmentVariable("GITHUB_RUN_NUMBER"));
    dnMsBuildSettings.WithProperty("RepositoryUrl", "https://github.com/" + EnvironmentVariable("GITHUB_REPOSITORY"));

    var dnPackSettings = new DotNetPackSettings();
    dnPackSettings.MSBuildSettings = dnMsBuildSettings;
    dnPackSettings.Verbosity = DotNetVerbosity.Minimal;
    dnPackSettings.Configuration = "Release";   

    DotNetPack("MonoGame.Library.FreeImage.csproj", dnPackSettings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("BuildWindows")
    .IsDependentOn("BuildMacOS")
    .IsDependentOn("BuildLinux");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);