namespace Olve.Engine3D.Scenes;

public abstract class SceneService
{
    /// <summary>
    /// Priority of the service. Services with lower priority are executed first.
    /// </summary>
    public virtual int Priority => 0;
    public virtual Result Load() => Result.Success();
    public virtual Result Unload() => Result.Success();
    public virtual Result<Pass> Input(TimeSpan deltaTime) => Result<Pass>.Success(Pass.Pass);
    public virtual Result Update(TimeSpan deltaTime) => Result.Success();
    public virtual Result Render(TimeSpan deltaTime) => Result.Success();
    
    protected static int GetPriorityFromDependencies(IReadOnlyCollection<SceneService> dependencies)
    {
        return dependencies.Max(service => service.Priority) + 1;
    }
}