using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
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
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnAdded,
            (TrackService ts, JunctionService js, Id<Track> trackId) =>
            {
                return ResultExtensions.Chain(
                    () => ts.Get(trackId),
                    track =>
                        Result.Concat(
                            js.AddJunctionConnection(track.Id, track.Start).ToEmptyResult(),
                            js.AddJunctionConnection(track.Id, track.End).ToEmptyResult()));
            });
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnRemoved,
            (TrackService ts, JunctionService js, Id<Track> trackId) =>
            {
                return ResultExtensions.Chain(
                    () => ts.Get(trackId),
                    track =>
                        Result.Concat(
                            js.RemoveJunctionConnection(track.Id, track.Start).MapToResult(),
                            js.RemoveJunctionConnection(track.Id, track.End).MapToResult()));
            });
        services.AddEventSceneService(sceneId,
            (TrackService ts) => ts.OnRemoved,
            (StationPlatformService sps, Id<Track> trackId) => sps.RemoveForTrack(trackId).MapToResult());
        services.AddSceneService<AddJunctionRuleHandlerService>(sceneId);
        services.AddSceneService<ClearJunctionSignalRules>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<JunctionSignalService>(sceneId);
        services.AddSceneService<PlaceVehicleHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);
        services.AddSceneService<StationPlatformAreaService>(sceneId);
        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<VehicleJunctionCrossingService>(sceneId);
        services.AddSceneService<VehicleMovementService>(sceneId);
        services.AddSceneService<DayTimeSteppingService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.TryAddScoped<JunctionService>();
        services.TryAddScoped<JunctionSignalRuleEvaluationService>();
        services.TryAddScoped<StationPlatformService>();
        services.TryAddScoped<StationNameGenerator>();
        services.TryAddScoped<StationService>();
        services.TryAddScoped<TrackConnectionService>();
        services.TryAddScoped<TrackService>();
        services.TryAddScoped<TrackPlacingService>();
        services.TryAddScoped<TrackLineStripDataService>();
        services.TryAddScoped<TrackValidationService>();
        services.TryAddScoped<VehicleJunctionService>();
        services.TryAddScoped<VehiclePositionService>();
        services.TryAddScoped<VehicleService>();

        return services;
    }
}
