using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameLogic.Industries;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Stations;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Scenes.GameLogic;

public static class GameLogicSceneServiceRegistration
{
    public static IServiceCollection AddGameLogicSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameLogicScene;

        // Scene services (participate in scene lifecycle)
        services.AddSceneService<AddJunctionRuleHandlerService>(sceneId);
        services.AddSceneService<ClearJunctionSignalRules>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<PlaceVehicleHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);
        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<VehicleJunctionCrossingService>(sceneId);
        services.AddSceneService<VehicleMovementService>(sceneId);
        services.AddSceneService<DayTimeSteppingService>(sceneId);
        services.AddSceneService<BuildingBlueprintLibraryService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.TryAddScoped<JunctionService>();
        services.TryAddScoped<JunctionSignalRuleEvaluationService>();
        services.TryAddScoped<JunctionSignalService>();
        services.TryAddScoped<BuildingService>();
        services.TryAddScoped<BuildingBlueprintService>();
        services.TryAddScoped<StationPlatformService>();
        services.TryAddScoped<StationNameGenerator>();
        services.TryAddScoped<StationService>();
        services.TryAddScoped<StationPlatformAreaService>();
        services.TryAddScoped<TrackConnectionService>();
        services.TryAddScoped<TrackService>();
        services.TryAddScoped<TrackPlacingService>();
        services.TryAddScoped<TrackLineStripDataService>();
        services.TryAddScoped<BuildingValidationService>();
        services.TryAddScoped<TrackValidationService>();
        services.TryAddScoped<VehicleJunctionService>();
        services.TryAddScoped<VehiclePositionService>();
        services.TryAddScoped<VehicleService>();

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
            });
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
            (TrackService ts) => ts.OnTrackRemoved,
            (StationPlatformService sps, Id<Track> trackId) => sps.RemoveForTrack(trackId).MapToResult());
        services.AddEventSceneService(sceneId,
            (JunctionService js) => js.OnJunctionConnectionsUpdated,
            (JunctionSignalService jss, Id<Junction> id) => jss.EvaluateSignal(id));
        services.AddEventSceneService(sceneId,
            (StationPlatformService sps) => sps.OnPlatformAdded,
            (StationPlatformAreaService spas, Id<StationPlatform> id) => spas.RegisterPlatform(id));
        services.AddEventSceneService(sceneId,
            (StationPlatformService sps) => sps.OnPlatformRemoved,
            (StationPlatformAreaService spas, Id<StationPlatform> id) => spas.DeregisterPlatform(id));
        services.AddEventSceneService(sceneId,
            (BuildingBlueprintService blueprintService) => blueprintService.OnBlueprintRemoved,
            (BuildingService buildingService, Id<BuildingBlueprint> id) => buildingService.DeleteBuildingsWithBlueprint(id));
        services.AddEventSceneService(sceneId,
            (VehicleMovementService vms) => vms.OnVehicleReachedTrackEnd,
            (VehicleJunctionCrossingService vjcs, Id<Vehicle> id) => vjcs.OnVehicleReachedEnd(id),
            after: [new SceneServiceType<VehicleMovementService>()],
            before: [new SceneServiceType<VehicleJunctionCrossingService>()]);

        return services;
    }
}
