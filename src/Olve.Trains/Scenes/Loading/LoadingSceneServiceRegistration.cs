using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Shared.GUI;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.Loading;

public static class LoadingSceneServiceRegistration
{
    public static IServiceCollection AddLoadingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.LoadingScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<SharedRenderingService>(sceneId);

        services.AddGuiSceneServices(sceneId);

        services.AddSceneService<CommandProcessingService>(sceneId);

        services.AddSceneParameterService<LoadingSceneParameterService>(sceneId);

        services.AddScoped<AssetPrewarmService>();
        services.AddScoped<GameLoadService>();
        services.AddSceneService<LoadingService>(sceneId);

        return services;
    }
}
