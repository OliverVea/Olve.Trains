namespace Olve.Engine3D.Scenes;

public static class SceneServicePriority
{
    private const int Step = 1024;

    public static int FromDependencies(IReadOnlyCollection<ISceneService> dependencies)
    {
        return dependencies.Max(service => service.Priority) + Step;
    }

    public static int FromDependents(IReadOnlyCollection<ISceneService> dependents)
    {
        return dependents.Min(service => service.Priority) - Step;
    }
}
