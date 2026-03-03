using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Commands.GameLogic;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;
using Olve.Trains.Scenes.GameRendering;

namespace Olve.Trains.Scenes.GameLogic;

public static class GameLogicSceneServiceRegistration
{
    public static IServiceCollection AddGameLogicSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameLogicScene;

        // Command processing
        services.AddSceneService<CommandProcessingService>(sceneId);

        // Scene services (participate in scene lifecycle)
        services.AddSceneService<CameraSceneService>(sceneId);
        services.AddSceneService<SetCameraHandlerService>(sceneId);
        services.AddSceneService<SetMouseHandlerService>(sceneId);
        services.AddSceneService<ClickHandlerService>(sceneId);
        services.AddSceneService<TerrainRaycastService>(sceneId);
        services.AddSceneService<CollisionRaycastService>(sceneId);
        services.AddSceneService<AddJunctionRuleHandlerService>(sceneId);
        services.AddSceneService<ClearJunctionSignalRulesHandlerService>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<PlaceTrackHandlerService>(sceneId);
        services.AddSceneService<DeleteTrackHandlerService>(sceneId);
        services.AddSceneService<PlaceVehicleHandlerService>(sceneId);
        services.AddSceneService<PlaceBuildingHandlerService>(sceneId);
        services.AddSceneService<DeleteVehicleHandlerService>(sceneId);
        services.AddSceneService<QueryVehicleHandlerService>(sceneId);
        services.AddSceneService<ListVehiclesHandlerService>(sceneId);
        services.AddSceneService<ListJunctionsHandlerService>(sceneId);
        services.AddSceneService<QueryJunctionHandlerService>(sceneId);
        services.AddSceneService<RaycastHandlerService>(sceneId);
        services.AddSceneService<ProjectToScreenHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);
        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<VehicleJunctionCrossingService>(sceneId);
        services.AddSceneService<VehicleMovementService>(sceneId);
        services.AddSceneService<DayTimeSteppingService>(sceneId);
        services.AddSceneService<BuildingBlueprintLibraryService>(sceneId);
        services.AddSceneService<VehicleCollisionService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.TryAddScoped<JunctionService>();
        services.TryAddScoped<JunctionSignalRuleEvaluationService>();
        services.TryAddScoped<JunctionSignalService>();
        services.TryAddScoped<JunctionSignalCollisionService>();
        services.TryAddScoped<BuildingService>();
        services.TryAddScoped<BuildingBlueprintService>();
        services.TryAddScoped<BuildingMeshBlueprintService>();
        services.TryAddScoped<BuildingPositionService>();
        services.TryAddScoped<BuildingCollisionService>();
        services.TryAddScoped<StationNameGenerator>();
        services.TryAddScoped<StationService>();
        services.TryAddScoped<StationBlueprintService>();
        services.TryAddScoped<ResidenceBlueprintService>();
        services.TryAddScoped<IndustryBlueprintService>();
        services.TryAddScoped<TrackConnectionService>();
        services.TryAddScoped<GridService>();
        services.TryAddScoped<TrackService>();
        services.TryAddScoped<TrackPlacingService>();
        services.TryAddScoped<TrackLineStripDataService>();
        services.TryAddScoped<BuildingValidationService>();
        services.TryAddScoped<TrackValidationService>();
        services.TryAddScoped<VehicleGroupService>();
        services.TryAddScoped<VehicleJunctionService>();
        services.TryAddScoped<VehiclePositionService>();
        services.TryAddScoped<VehicleService>();
        services.TryAddScoped<TrackCollisionService>();
        services.TryAddScoped<VehicleCollisionService>();

        // Scene Events
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackAdded,
            (TrackService ts, JunctionService js, Id<Track> trackId) =>
            {
                if (!ts.TryGetTrack(trackId, out var track))
                {
                    return new ResultProblem("Track not found: '{0}'", trackId);
                }

                return Result.Concat(
                    js.AddJunctionConnection(track.Id, track.Start).ToEmptyResult(),
                    js.AddJunctionConnection(track.Id, track.End).ToEmptyResult());
            },
            prefill: ts => ts.TrackIds);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackService ts, JunctionService js, Id<Track> trackId) =>
            {
                if (!ts.TryGetTrack(trackId, out var track))
                {
                    return new ResultProblem("Track not found: '{0}'", trackId);
                }

                return Result.Concat(
                    js.RemoveJunctionConnection(track.Id, track.Start).MapToResult(),
                    js.RemoveJunctionConnection(track.Id, track.End).MapToResult());
            });
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
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingAdded,
            (StationService ss, Id<Building> id) => ss.CreateStationForBuilding(id),
            prefill: bs => bs.BuildingIds);
        services.AddEventSceneService(sceneId,
            (BuildingService bs) => bs.OnBuildingRemoved,
            (StationService ss, Id<Building> id) => ss.DeleteStationForBuilding(id));
        services.AddEventSceneService(sceneId,
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
            (TrackService ts) => ts.OnTrackAdded,
            (TrackCollisionService tcs, Id<Track> id) => tcs.Register(id),
            prefill: ts => ts.TrackIds);
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnTrackRemoved,
            (TrackCollisionService tcs, Id<Track> id) => tcs.Unregister(id));
        services.AddEventSceneService(sceneId,
            (VehicleService vs) => vs.OnVehicleAdded,
            (VehicleCollisionService vcs, Id<Vehicle> id) => vcs.Register(id),
            prefill: vs => vs.VehicleIds);
        services.AddEventSceneService(sceneId,
            (VehicleService vs) => vs.OnVehicleRemoved,
            (VehicleCollisionService vcs, Id<Vehicle> id) => vcs.Unregister(id));
        services.AddEventSceneService(sceneId,
            (VehicleMovementService vms) => vms.OnVehicleReachedTrackEnd,
            (VehicleJunctionCrossingService vjcs, Id<Vehicle> id) => vjcs.OnVehicleReachedEnd(id),
            after: [new SceneServiceType<VehicleMovementService>()],
            before: [new SceneServiceType<VehicleJunctionCrossingService>()]);

        return services;
    }
}
