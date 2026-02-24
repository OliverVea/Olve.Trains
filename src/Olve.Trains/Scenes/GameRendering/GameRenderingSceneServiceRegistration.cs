using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameRendering;

public static class GameRenderingSceneServiceRegistration
{
    private static readonly ISceneServiceType[] BeforeTrackRendering = [new SceneServiceType<TrackRenderingService>()];
    private static readonly ISceneServiceType[] BeforeBuildingRendering = [new SceneServiceType<BuildingRenderingService>()];

    public static IServiceCollection AddGameRenderingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameRenderingScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<CameraSceneService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<SetCameraHandlerService>(sceneId);
        services.AddSceneService<SetMouseHandlerService>(sceneId);
        services.AddSceneService<TerrainRenderingService>(sceneId);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (TrackRenderingService trs, Id<Track> trackId) =>
            {
                trs.Unregister(trackId);
                return trs.Register(trackId);
            },
            prefill: ts => ts.TrackIds,
            before: BeforeTrackRendering);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackRenderingService trs, Id<Track> trackId) => trs.Unregister(trackId),
            before: BeforeTrackRendering);
        services.AddSceneService<TrackRenderingService>(sceneId);
        services.AddSceneService<TrackGhostRenderingService>(sceneId);
        services.AddSceneService<VehicleRenderingService>(sceneId);
        services.AddSceneService<JunctionSignalRenderingService>(sceneId);
        services.AddSceneService<TerrainRaycastService>(sceneId);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (BuildingRenderingService brs, Id<Building> id) => brs.Register(id),
            prefill: bs => bs.BuildingIds,
            before: BeforeBuildingRendering);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (BuildingRenderingService brs, Id<Building> id) => brs.Unregister(id),
            before: BeforeBuildingRendering);
        services.AddSceneService<BuildingRenderingService>(sceneId);

        return services;
    }
}
