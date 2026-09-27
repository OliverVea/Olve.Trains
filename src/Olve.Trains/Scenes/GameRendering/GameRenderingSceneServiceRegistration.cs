using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Commands.GameRendering;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Environment;
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
    private static readonly ISceneServiceType[] BeforeEnvironmentalObjectRendering = [new SceneServiceType<EnvironmentalObjectRenderingService>()];

    public static IServiceCollection AddGameRenderingSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameRenderingScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<SharedRenderingService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<ShadowMapService>(sceneId);
        services.AddSceneService<TerrainRenderingService>(sceneId);
        services.AddSceneService<MeshRenderingService>(sceneId);
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (TrackRenderingService trs, EntityAdded<Track, Id<Track>> added) =>
            {
                trs.Unregister(added.Id);
                return trs.Register(added.Id);
            },
            prefill: ts => ts.Tracks.AsAdded(),
            before: BeforeTrackRendering);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackRenderingService trs, EntityDeleted<Track, Id<Track>> deleted) => trs.Unregister(deleted.Id),
            before: BeforeTrackRendering);
        services.AddSceneService<TrackRenderingService>(sceneId);
        services.AddSceneService<TrackGhostRenderingService>(sceneId);
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainAdded,
            (TrainRenderingService trs, EntityAdded<Train, Id<Train>> added) => trs.AddTrain(added.Id),
            before: BeforeTrainRendering);
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainRenderingService trs, EntityDeleted<Train, Id<Train>> deleted) => trs.RemoveTrain(deleted.Id),
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
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (BuildingRenderingService brs, EntityAdded<Building, Id<Building>> added) => brs.Register(added.Id),
            prefill: bs => bs.Buildings.AsAdded(),
            before: BeforeBuildingRendering);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (BuildingRenderingService brs, EntityDeleted<Building, Id<Building>> deleted) => brs.Unregister(deleted.Id),
            before: BeforeBuildingRendering);
        services.AddSceneService<FootprintRenderingService>(sceneId);
        services.AddSceneService<BuildingRenderingService>(sceneId);
        services.AddSceneService<BuildingGhostPreviewService>(sceneId);
        services.AddImmediateEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectAdded,
            (EnvironmentalObjectRenderingService eors, EntityAdded<EnvironmentalObject, Id<EnvironmentalObject>> added) => eors.Register(added.Id),
            prefill: eos => eos.Objects.AsAdded(),
            before: BeforeEnvironmentalObjectRendering);
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectRemoved,
            (EnvironmentalObjectRenderingService eors, EntityDeleted<EnvironmentalObject, Id<EnvironmentalObject>> deleted) => eors.Unregister(deleted.Id),
            before: BeforeEnvironmentalObjectRendering);
        services.AddSceneService<EnvironmentalObjectRenderingService>(sceneId);
        services.AddSceneService<ColliderDebugRenderingService>(sceneId);
        services.AddSceneService<ToggleColliderDebugHandlerService>(sceneId);

        services.TryAddScoped<ClearancePreviewService>();

        return services;
    }
}
