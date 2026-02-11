using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.Game.Junctions;
using Olve.Trains.Scenes.Game.Light;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Terrain;
using Olve.Trains.Scenes.Game.Time;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;

namespace Olve.Trains.Scenes.Game;

public static class GameSceneServiceRegistration
{
    public static IServiceCollection AddGameSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameScene;

        // Scene services (participate in scene lifecycle)
        services.AddSceneService<AddJunctionRuleHandlerService>(sceneId);
        services.AddSceneService<ClearJunctionSignalRules>(sceneId);
        services.AddSceneService<JunctionSignalRuleService>(sceneId);
        services.AddSceneService<JunctionSignalService>(sceneId);
        services.AddSceneService<JunctionUpdatingService>(sceneId);
        services.AddSceneService<PlaceVehicleHandlerService>(sceneId);
        services.AddSceneService<SceneLightService>(sceneId);
        services.AddSceneService<SetTimeHandlerService>(sceneId);
        services.AddSceneService<StationPlatformAreaService>(sceneId);
        services.AddSceneService<StationPlatformTrackDeletionService>(sceneId);
        services.AddSceneService<TerrainService>(sceneId);
        services.AddSceneService<TrackSplineService>(sceneId);
        services.AddSceneService<VehicleJunctionCrossingService>(sceneId);
        services.AddSceneService<VehicleMovementService>(sceneId);
        services.AddSceneService<DayTimeSteppingService>(sceneId);

        // Non-scene singletons (dependencies only, not in scene lifecycle)
        services.AddSingleton<JunctionService>();
        services.AddSingleton<JunctionSignalRuleEvaluationService>();
        services.AddSingleton<StationPlatformService>();
        services.AddSingleton<StationNameGenerator>();
        services.AddSingleton<StationService>();
        services.AddSingleton<TrackConnectionService>();
        services.AddSingleton<TrackService>();
        services.AddSingleton<TrackPlacingService>();
        services.AddSingleton<TrackLineStripDataService>();
        services.AddSingleton<TrackValidationService>();
        services.AddSingleton<VehicleJunctionService>();
        services.AddSingleton<VehiclePositionService>();
        services.AddSingleton<VehicleService>();

        return services;
    }
}
