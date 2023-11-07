
namespace BuildScripts;

[TaskName("BuildLibrary")]
[IsDependentOn(typeof(BuildWindowsTask))]
[IsDependentOn(typeof(BuildMacOSTask))]
[IsDependentOn(typeof(BuildLinuxTask))]
public class BuildLibraryTask : FrostingTask { }

[TaskName("TestLibrary")]
[IsDependentOn(typeof(TestWindowsTask))]
[IsDependentOn(typeof(TestMacOSTask))]
[IsDependentOn(typeof(TestLinuxTask))]
public class TestLibraryTask : FrostingTask { }

[TaskName("Default")]
[IsDependentOn(typeof(BuildLibraryTask))]
[IsDependentOn(typeof(PublishLibraryTask))]
[IsDependentOn(typeof(TestLibraryTask))]
public class DefaultTask : FrostingTask { }
