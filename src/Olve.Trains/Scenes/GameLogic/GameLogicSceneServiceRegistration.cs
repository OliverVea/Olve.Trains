using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Commands.GameLogic;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Scenes.GameLogic;

public static class GameLogicSceneServiceRegistration
{
    public static IServiceCollection AddGameLogicSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameLogicScene;

        // Command processing
        services.AddSceneService<CommandProcessingService>(sceneId);

        // Scene services (participate in scene lifecycle)
        services.AddSceneService<AddJunctionRuleHandlerService>(sceneId);
        services.AddSceneService<AddWagonHandlerService>(sceneId);
        services.AddSceneService<BuildingBlueprintLibraryService>(sceneId);
        services.AddSceneService<CargoAndRecipeLibraryService>(sceneId);
        services.AddSceneService<CameraSceneService>(sceneId);
        services.AddSceneService<ClearJunctionSignalRulesHandlerService>(sceneId);
        services.AddSceneService<ClickHandlerService>(sceneId);

        services.AddSceneService<EnvironmentalObjectBlueprintLibraryService>(sceneId);

        services.AddSceneService<DayTimeSteppingService>(sceneId);
        services.AddSceneService<DeleteTrackHandlerService>(sceneId);
        services.AddSceneService<DeleteTrainHandlerService>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<ListJunctionsHandlerService>(sceneId);
        services.AddSceneService<ListTrainsHandlerService>(sceneId);
        services.AddSceneService<ListWagonsHandlerService>(sceneId);
        services.AddSceneService<PlaceBuildingHandlerService>(sceneId);
        services.AddSceneService<PlaceTrackHandlerService>(sceneId);
        services.AddSceneService<PlaceTrainHandlerService>(sceneId);
        services.AddSceneService<ProjectToScreenHandlerService>(sceneId);
        services.AddSceneService<QueryBuildingHandlerService>(sceneId);
        services.AddSceneService<QueryJunctionHandlerService>(sceneId);
        services.AddSceneService<QueryTrainHandlerService>(sceneId);
        services.AddSceneService<RaycastHandlerService>(sceneId);
        services.AddSceneService<RemoveWagonHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetCameraHandlerService>(sceneId);
        services.AddSceneService<SetMouseHandlerService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);

        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<TrainCollisionService>(sceneId);
        services.AddSceneService<TrainJunctionCrossingService>(sceneId);
        services.AddSceneService<IndustryProductionService>(sceneId);
        services.AddSceneService<StationCargoTransferService>(sceneId);
        services.AddSceneService<TrainMovementService>(sceneId);
        services.AddSceneService<WagonBlueprintLibraryService>(sceneId);
        services.AddSceneService<WagonInventoryService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.TryAddScoped<BuildingBlueprintService>();
        services.TryAddScoped<BuildingCollisionService>();
        services.TryAddScoped<BuildingMeshBlueprintService>();
        services.TryAddScoped<BuildingPositionService>();
        services.TryAddScoped<BuildingService>();
        services.TryAddScoped<BuildingValidationService>();
        services.TryAddScoped<CargoInventoryService>();
        services.TryAddScoped<CargoTransferService>();
        services.TryAddScoped<CargoTransferPolicyService>();
        services.TryAddScoped<CargoTypeService>();
        services.TryAddScoped<ColliderDebugSettings>();
        services.TryAddScoped<EnvironmentalObjectBlueprintService>();
        services.TryAddScoped<EnvironmentalObjectCollisionService>();
        services.TryAddScoped<EnvironmentalObjectService>();
        services.TryAddScoped<GridService>();
        services.TryAddScoped<IndustryBlueprintService>();
        services.TryAddScoped<IndustryRecipeService>();
        services.TryAddScoped<RecipeTransactionService>();
        services.TryAddScoped<IndustryService>();
        services.TryAddScoped<JunctionService>();
        services.TryAddScoped<JunctionSignalCollisionService>();
        services.TryAddScoped<JunctionSignalRuleEvaluationService>();
        services.TryAddScoped<JunctionSignalService>();
        services.TryAddScoped<ResidenceBlueprintService>();
        services.TryAddScoped<StationBlueprintService>();
        services.TryAddScoped<StationNameGenerator>();
        services.TryAddScoped<StationService>();
        services.TryAddScoped<TerrainHighlightSettings>();
        services.TryAddScoped<TrackCollisionService>();
        services.TryAddScoped<TrackConnectionService>();
        services.TryAddScoped<TrackLineStripDataService>();
        services.TryAddScoped<TrackPlacingService>();
        services.TryAddScoped<TrackService>();
        services.TryAddScoped<TrackValidationService>();
        services.TryAddScoped<TrainCollisionService>();
        services.TryAddScoped<TrainGroupService>();
        services.TryAddScoped<TrainTrackHistoryService>();
        services.TryAddScoped<TrainJunctionService>();
        services.TryAddScoped<TrainPositionService>();
        services.TryAddScoped<TrainService>();
        services.TryAddScoped<TrainWagonService>();
        services.TryAddScoped<WagonBlueprintService>();
        services.TryAddScoped<WagonPositioningService>();

        // Scene Events
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (JunctionService js, TrackService ts, Id<Track> trackId) => js.OnTrackAdded(trackId, ts),
            prefill: ts => ts.TrackIds);
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (JunctionService js, TrackService ts, Id<Track> trackId) => js.OnTrackRemoved(trackId, ts));
        services.AddEventSceneService(sceneId,
            (JunctionService js) => js.OnJunctionConnectionsUpdated,
            (JunctionSignalService jss, Id<Junction> id) => jss.EvaluateSignal(id));
        services.AddEventSceneService(sceneId,
            (JunctionSignalService jss) => jss.OnJunctionAdded,
            (JunctionSignalCollisionService jscs, Id<Junction> id) => jscs.Register(id),
            prefill: jss => jss.SignalJunctions);
        services.AddEventSceneService(sceneId,
            (JunctionSignalService jss) => jss.OnJunctionRemoved,
            (JunctionSignalCollisionService jscs, Id<Junction> id) => jscs.Unregister(id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (StationService ss, Id<Building> id) => ss.CreateStationForBuilding(id).ToEmptyResult(),
            prefill: bs => bs.BuildingIds);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (StationService ss, Id<Building> id) => ss.DeleteStationForBuilding(id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (IndustryService ist, Id<Building> id) => ist.CreateIndustryForBuilding(id).ToEmptyResult(),
            prefill: bs => bs.BuildingIds);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (IndustryService ist, Id<Building> id) => ist.RemoveIndustryForBuilding(id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (BuildingCollisionService bcs, Id<Building> id) => bcs.Register(id),
            prefill: bs => bs.BuildingIds);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (BuildingCollisionService bcs, Id<Building> id) => bcs.Unregister(id));
        services.AddEventSceneService(sceneId,
            (BuildingBlueprintService blueprintService) => blueprintService.OnBlueprintRemoved,
            (BuildingService buildingService, Id<BuildingBlueprint> id) => buildingService.DeleteBuildingsWithBlueprint(id));
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectBlueprintService ebs) => ebs.OnBlueprintRemoved,
            (EnvironmentalObjectService eos, Id<EnvironmentalObjectBlueprint> id) => eos.DeleteObjectsWithBlueprint(id));
        services.AddImmediateEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectAdded,
            (EnvironmentalObjectCollisionService eocs, Id<EnvironmentalObject> id) => eocs.Register(id),
            prefill: eos => eos.ObjectIds);
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectRemoved,
            (EnvironmentalObjectCollisionService eocs, Id<EnvironmentalObject> id) => eocs.Unregister(id));
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (TrackCollisionService tcs, Id<Track> id) => tcs.Register(id),
            prefill: ts => ts.TrackIds);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackCollisionService tcs, Id<Track> id) => tcs.Unregister(id));
        services.AddEventSceneService(sceneId,
            (TrainService vs) => vs.OnTrainAdded,
            (TrainCollisionService vcs, Id<Train> id) => vcs.Register(id),
            prefill: vs => vs.TrainIds);
        services.AddEventSceneService(sceneId,
            (TrainService vs) => vs.OnTrainRemoved,
            (TrainCollisionService vcs, Id<Train> id) => vcs.Unregister(id));
        services.AddEventSceneService(sceneId,
            (TrainMovementService vms) => vms.OnTrainReachedTrackEnd,
            (TrainJunctionCrossingService vjcs, Id<Train> id) => vjcs.OnTrainReachedEnd(id),
            after: [new SceneServiceType<TrainMovementService>()],
            before: [new SceneServiceType<TrainJunctionCrossingService>()]);

        // TODO: Remove auto-attach once train depot UI allows players to build trains manually
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainAdded,
            (TrainWagonService tws, Id<Train> trainId) =>
            {
                return Result.Concat(
                    tws.AddWagon(trainId, WagonBlueprintCatalog.GoodsWagon).ToEmptyResult(),
                    tws.AddWagon(trainId, WagonBlueprintCatalog.GoodsWagon).ToEmptyResult());
            },
            prefill: ts => ts.TrainIds);

        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainWagonService tws, Id<Train> trainId) => tws.RemoveAllWagons(trainId));
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainTrackHistoryService tths, Id<Train> trainId) => tths.RemoveHistory(trainId));

        return services;
    }
}
