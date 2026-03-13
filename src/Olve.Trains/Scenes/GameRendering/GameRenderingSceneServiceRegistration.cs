using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Commands.GameRendering;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.GameRendering;

public static class GameRenderingSceneServiceRegistration
{
    private static readonly ISceneServiceType[] BeforeTrackRendering = [new SceneServiceType<TrackRenderingService>()];
    private static readonly ISceneServiceType[] BeforeTrainRendering = [new SceneServiceType<TrainRenderingService>()];
    private static readonly ISceneServiceType[] BeforeWagonRendering = [new SceneServiceType<WagonRenderingService>()];
    private static readonly ISceneServiceType[] BeforeBuildingRendering = [new SceneServiceType<BuildingRenderingService>()];

    public static IServiceCollection AddGameRenderingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameRenderingScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<SharedRenderingService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<ShadowMapService>(sceneId);
        services.AddSceneService<TerrainRenderingService>(sceneId);
        services.AddSceneService<MeshRenderingService>(sceneId);
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
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainAdded,
            (TrainRenderingService trs, Id<Train> id) => trs.AddTrain(id),
            before: BeforeTrainRendering);
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainRenderingService trs, Id<Train> id) => trs.RemoveTrain(id),
            before: BeforeTrainRendering);
        services.AddSceneService<TrainRenderingService>(sceneId);
        services.AddEventSceneService(sceneId,
            (TrainWagonService tws) => tws.OnWagonAdded,
            (WagonRenderingService wrs, (Id<Train> TrainId, Wagon Wagon) e) => wrs.AddWagon(e.Wagon),
            before: BeforeWagonRendering);
        services.AddEventSceneService(sceneId,
            (TrainWagonService tws) => tws.OnWagonRemoved,
            (WagonRenderingService wrs, (Id<Train> TrainId, Wagon Wagon) e) => wrs.RemoveWagon(e.Wagon),
            before: BeforeWagonRendering);
        services.AddSceneService<WagonRenderingService>(sceneId);
        services.AddSceneService<JunctionSignalRenderingService>(sceneId);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (BuildingRenderingService brs, Id<Building> id) => brs.Register(id),
            prefill: bs => bs.BuildingIds,
            before: BeforeBuildingRendering);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (BuildingRenderingService brs, Id<Building> id) => brs.Unregister(id),
            before: BeforeBuildingRendering);
        services.AddSceneService<FootprintRenderingService>(sceneId);
        services.AddSceneService<BuildingRenderingService>(sceneId);
        services.AddSceneService<ColliderDebugRenderingService>(sceneId);
        services.AddSceneService<ToggleColliderDebugHandlerService>(sceneId);

        return services;
    }
}
