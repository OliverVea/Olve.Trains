namespace Olve.Engine3D.Scenes;

public static class SceneServicePriority
{
    public static int FromDependencies(IReadOnlyCollection<ISceneService> dependencies)
    {
        return dependencies.Max(service => service.Priority) + 1;
    }

    public static int FromDependents(IReadOnlyCollection<ISceneService> dependents)
    {
        return dependents.Min(service => service.Priority) - 1;
    }
}
