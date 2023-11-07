
namespace BuildScripts;

[TaskName("BuildLibrary")]
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
