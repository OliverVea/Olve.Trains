using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.Rendering;

public static class RenderingSceneServiceRegistration
{
    public static IServiceCollection AddRenderingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.RenderingScene;

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
