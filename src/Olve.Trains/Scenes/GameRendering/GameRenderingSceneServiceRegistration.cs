using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameRendering;

public static class GameRenderingSceneServiceRegistration
{
    public static IServiceCollection AddGameRenderingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameRenderingScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<CameraSceneService>(sceneId);
        services.AddSceneService<TerrainRenderingService>(sceneId);
        services.AddSceneService<TrackRenderingUpdaterService>(sceneId);
        services.AddSceneService<TrackRenderingService>(sceneId);
        services.AddSceneService<VehicleRenderingService>(sceneId);
        services.AddSceneService<JunctionSignalRenderingService>(sceneId);
        services.AddSceneService<TerrainRaycastService>(sceneId);

        return services;
    }
}
