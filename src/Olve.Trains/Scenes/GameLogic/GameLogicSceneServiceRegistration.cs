using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Cities;
using Olve.Trains.Scenes.GameLogic.Buildings.Depots;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Commands.GameLogic;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Ordering;
using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Scenes.GameLogic.Saves;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Resources;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Scenes.GameLogic;

public static class GameLogicSceneServiceRegistration
{
    public static IServiceCollection AddGameLogicSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameLogicScene.Id;

        // Scene parameter services
        services.AddSceneParameters(SceneIds.GameLogicScene, () => new GameSceneArguments());

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
        services.AddSceneService<DeleteEnvironmentalObjectHandlerService>(sceneId);
        services.AddSceneService<DeleteTrackHandlerService>(sceneId);
        services.AddSceneService<DeleteTrainHandlerService>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<ListBuildingsHandlerService>(sceneId);
        services.AddSceneService<ListCitiesHandlerService>(sceneId);
        services.AddSceneService<ListEntitiesHandlerService>(sceneId);
        services.AddSceneService<ListJunctionsHandlerService>(sceneId);
        services.AddSceneService<ListTracksHandlerService>(sceneId);
        services.AddSceneService<ListTrainsHandlerService>(sceneId);
        services.AddSceneService<ListWagonsHandlerService>(sceneId);
        services.AddSceneService<LoadGameHandlerService>(sceneId);
        services.AddSceneService<PlaceBuildingHandlerService>(sceneId);
        services.AddSceneService<PlaceTrackHandlerService>(sceneId);
        services.AddSceneService<PlaceTrainHandlerService>(sceneId);
        services.AddSceneService<ProjectToScreenHandlerService>(sceneId);
        services.AddSceneService<QueryBuildingHandlerService>(sceneId);
        services.AddSceneService<QueryCityHandlerService>(sceneId);
        services.AddSceneService<QueryMoneyHandlerService>(sceneId);
        services.AddSceneService<QueryJunctionHandlerService>(sceneId);
        services.AddSceneService<QueryTimeHandlerService>(sceneId);
        services.AddSceneService<QueryTrackHandlerService>(sceneId);
        services.AddSceneService<QueryTrainHandlerService>(sceneId);
        services.AddSceneService<RaycastHandlerService>(sceneId);
        services.AddSceneService<RemoveWagonHandlerService>(sceneId);
        services.AddSceneService<SaveGameHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetCameraHandlerService>(sceneId);
        services.AddSceneService<SetMouseHandlerService>(sceneId);
        services.AddSceneService<SetSpeedHandlerService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);
        services.AddSceneService<SetTrainSpeedHandlerService>(sceneId);
        services.AddSceneService<SetWagonFilterHandlerService>(sceneId);

        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<TrainCollisionService>(sceneId);
        services.AddSceneService<TrainJunctionCrossingService>(sceneId);
        services.AddSceneService<ResourceOwnershipService>(sceneId);
        services.AddSceneService<IndustryProductionService>(sceneId);
        services.AddSceneService<StationCargoTransferService>(sceneId);
        services.AddSceneService<TrainMovementService>(sceneId);
        services.AddSceneService<TrainPositionService>(sceneId);
        services.AddSceneService<WagonBlueprintLibraryService>(sceneId);
        services.AddSceneService<WagonInventoryService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.TryAddScoped<SequenceService>();
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
        services.TryAddScoped<PlacementClearanceService>();
        services.TryAddScoped<EnvironmentalObjectBlueprintService>();
        services.TryAddScoped<EnvironmentalObjectCollisionService>();
        services.TryAddScoped<EnvironmentalObjectService>();
        services.TryAddScoped<GameSaveService>();
        services.TryAddScoped<GridService>();
        services.TryAddScoped<IndustryBlueprintService>();
        services.TryAddScoped<IndustryRecipeService>();
        services.TryAddScoped<RecipeTransactionService>();
        services.TryAddScoped<IndustryService>();
        services.TryAddScoped<JunctionService>();
        services.TryAddScoped<JunctionSignalCollisionService>();
        services.TryAddScoped<JunctionSignalRuleEvaluationService>();
        services.TryAddScoped<JunctionSignalService>();
        services.TryAddScoped<MoneyService>();
        services.TryAddScoped<ResourceService>();
        services.TryAddScoped<ResidenceBlueprintService>();
        services.TryAddScoped<CityService>();
        services.TryAddScoped<StationBlueprintService>();
        services.TryAddScoped<StationNameGenerator>();
        services.TryAddScoped<DepotBlueprintService>();
        services.TryAddScoped<DepotService>();
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
        services.TryAddScoped<TrainService>();
        services.TryAddScoped<TrainWagonService>();
        services.TryAddScoped<WagonBlueprintService>();
        services.TryAddScoped<WagonPositioningService>();

        // Scene Events
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (JunctionService js, EntityAdded<Track, Id<Track>> added) => js.OnTrackAdded(added.Entity),
            prefill: ts => ts.Tracks.AsAdded());
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (JunctionService js, EntityDeleted<Track, Id<Track>> deleted) => js.OnTrackRemoved(deleted.Entity));
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
            (StationService ss, EntityAdded<Building, Id<Building>> added) => ss.CreateStationForBuilding(added.Id).ToEmptyResult(),
            prefill: bs => bs.Buildings.AsAdded());
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (StationService ss, EntityDeleted<Building, Id<Building>> deleted) => ss.DeleteStationForBuilding(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (DepotService ds, EntityAdded<Building, Id<Building>> added) => ds.CreateDepotForBuilding(added.Id).ToEmptyResult(),
            prefill: bs => bs.Buildings.AsAdded());
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (DepotService ds, EntityDeleted<Building, Id<Building>> deleted) => ds.DeleteDepotForBuilding(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (IndustryService ist, EntityAdded<Building, Id<Building>> added) => ist.CreateIndustryForBuilding(added.Id).ToEmptyResult(),
            prefill: bs => bs.Buildings.AsAdded());
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (IndustryService ist, EntityDeleted<Building, Id<Building>> deleted) => ist.RemoveIndustryForBuilding(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (CityService cs, EntityAdded<Building, Id<Building>> added) => cs.CreateResidenceForBuilding(added.Id).ToEmptyResult(),
            prefill: bs => bs.Buildings.AsAdded());
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (CityService cs, EntityDeleted<Building, Id<Building>> deleted) => cs.RemoveResidenceForBuilding(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (BuildingCollisionService bcs, EntityAdded<Building, Id<Building>> added) => bcs.Register(added.Id),
            prefill: bs => bs.Buildings.AsAdded());
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (BuildingCollisionService bcs, EntityDeleted<Building, Id<Building>> deleted) => bcs.Unregister(deleted.Id));
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (PlacementClearanceService pcs, EntityAdded<Building, Id<Building>> added) => pcs.ClearForBuilding(added.Id));
        services.AddEventSceneService(sceneId,
            (BuildingBlueprintService blueprintService) => blueprintService.OnBlueprintRemoved,
            (BuildingService buildingService, EntityDeleted<BuildingBlueprint, Id<BuildingBlueprint>> deleted) => buildingService.DeleteBuildingsWithBlueprint(deleted.Id));
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectBlueprintService ebs) => ebs.OnBlueprintRemoved,
            (EnvironmentalObjectService eos, EntityDeleted<EnvironmentalObjectBlueprint, Id<EnvironmentalObjectBlueprint>> deleted) => eos.DeleteObjectsWithBlueprint(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectAdded,
            (EnvironmentalObjectCollisionService eocs, EntityAdded<EnvironmentalObject, Id<EnvironmentalObject>> added) => eocs.Register(added.Id),
            prefill: eos => eos.Objects.AsAdded());
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectRemoved,
            (EnvironmentalObjectCollisionService eocs, EntityDeleted<EnvironmentalObject, Id<EnvironmentalObject>> deleted) => eocs.Unregister(deleted.Id));
        services.AddImmediateEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectAdded,
            (ResourceService rs, EntityAdded<EnvironmentalObject, Id<EnvironmentalObject>> added) => rs.CreateResourceForEnvironmentalObject(added.Id),
            prefill: eos => eos.Objects.AsAdded());
        services.AddEventSceneService(sceneId,
            (EnvironmentalObjectService eos) => eos.OnObjectRemoved,
            (ResourceService rs, EntityDeleted<EnvironmentalObject, Id<EnvironmentalObject>> deleted) => rs.RemoveResourceForEnvironmentalObject(deleted.Id));

        // Mark resource ownership dirty when buildings or resources change
        // TODO: Optimize — only mark dirty when the entity is relevant (extractive building, matching resource type/position)
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (ResourceOwnershipService ros, EntityAdded<Building, Id<Building>> _) => { ros.MarkDirty(); return Result.Success(); });
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (ResourceOwnershipService ros, EntityDeleted<Building, Id<Building>> _) => { ros.MarkDirty(); return Result.Success(); });
        services.AddEventSceneService(sceneId,
            (ResourceService rs) => rs.OnResourceAdded,
            (ResourceOwnershipService ros, EntityAdded<Resource, Id<Resource>> _) => { ros.MarkDirty(); return Result.Success(); });
        services.AddEventSceneService(sceneId,
            (ResourceService rs) => rs.OnResourceRemoved,
            (ResourceOwnershipService ros, EntityDeleted<Resource, Id<Resource>> _) => { ros.MarkDirty(); return Result.Success(); });
        services.AddImmediateEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (TrackCollisionService tcs, EntityAdded<Track, Id<Track>> added) => tcs.Register(added.Id),
            prefill: ts => ts.Tracks.AsAdded());
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackCollisionService tcs, EntityDeleted<Track, Id<Track>> deleted) => tcs.Unregister(deleted.Id));
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (PlacementClearanceService pcs, EntityAdded<Track, Id<Track>> added) => pcs.ClearForTrack(added.Id));
        services.AddEventSceneService(sceneId,
            (TrainService vs) => vs.OnTrainAdded,
            (TrainCollisionService vcs, EntityAdded<Train, Id<Train>> added) => vcs.Register(added.Id),
            prefill: vs => vs.Trains.AsAdded());
        services.AddEventSceneService(sceneId,
            (TrainService vs) => vs.OnTrainRemoved,
            (TrainCollisionService vcs, EntityDeleted<Train, Id<Train>> deleted) => vcs.Unregister(deleted.Id));
        services.AddEventSceneService(sceneId,
            (TrainMovementService vms) => vms.OnTrainReachedTrackEnd,
            (TrainJunctionCrossingService vjcs, Id<Train> id) => vjcs.OnTrainReachedEnd(id),
            after: [new SceneServiceType<TrainMovementService>()],
            before: [new SceneServiceType<TrainJunctionCrossingService>()]);

        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainWagonService tws, EntityDeleted<Train, Id<Train>> deleted) => tws.RemoveAllWagons(deleted.Id));
        services.AddEventSceneService(sceneId,
            (TrainService ts) => ts.OnTrainRemoved,
            (TrainTrackHistoryService tths, EntityDeleted<Train, Id<Train>> deleted) => tths.RemoveHistory(deleted.Id));

        return services;
    }
}
