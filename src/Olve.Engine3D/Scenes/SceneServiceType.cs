using Microsoft.Extensions.DependencyInjection;

namespace Olve.Engine3D.Scenes;

public interface ISceneServiceType
{
    int ResolvePriority(IServiceProvider sp);
}

public readonly struct SceneServiceType<T> : ISceneServiceType where T : class, ISceneService
{
    public int ResolvePriority(IServiceProvider sp) => sp.GetRequiredService<T>().Priority;
}
