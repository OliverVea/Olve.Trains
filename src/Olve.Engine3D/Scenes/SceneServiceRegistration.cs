using Microsoft.Extensions.DependencyInjection;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public static class SceneServiceRegistration
{
    public static IServiceCollection AddSceneService<T>(this IServiceCollection services, Id<IScene> sceneId)
        where T : SceneService
    {
        services.AddSingleton<T>();
        services.AddKeyedSingleton<SceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
        return services;
    }
}
