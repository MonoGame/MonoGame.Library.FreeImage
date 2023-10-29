
namespace BuildScripts;

[TaskName("Prepare Build")]
public sealed class PrepTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.CleanDirectory(context.ArtifactsDir);
        context.CreateDirectory(context.ArtifactsDir);
    }
}
