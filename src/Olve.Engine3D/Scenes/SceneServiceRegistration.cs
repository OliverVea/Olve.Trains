using Microsoft.Extensions.DependencyInjection;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public static class SceneServiceRegistration
{
    public static IServiceCollection AddSceneService<T>(this IServiceCollection services, Id<IScene> sceneId)
        where T : class, ISceneService
    {
        services.AddSingleton<T>();
        services.AddKeyedSingleton<ISceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
        return services;
    }
}
