using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Junctions;
using Olve.Trains.Scenes.Game.Light;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Terrain;
using Olve.Trains.Scenes.Game.Time;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;

namespace Olve.Trains.Scenes.Game;

[ServiceProvider]
[Singleton(typeof(AddJunctionRuleHandlerService))]
[Singleton(typeof(ClearJunctionSignalRules))]
[Singleton(typeof(JunctionService))]
[Singleton(typeof(JunctionSignalRuleEvaluationService))]
[Singleton(typeof(JunctionSignalRuleService))]
[Singleton(typeof(JunctionSignalService))]
[Singleton(typeof(JunctionUpdatingService))]
[Singleton(typeof(PlaceVehicleHandlerService))]
[Singleton(typeof(SceneLightService))]
[Singleton(typeof(SetTimeHandlerService))]
[Singleton(typeof(StationPlatformAreaService))]
[Singleton(typeof(StationPlatformService))]
[Singleton(typeof(StationPlatformTrackDeletionService))]
[Singleton(typeof(StationService))]
[Singleton(typeof(TerrainService))]
[Singleton(typeof(TrackConnectionService))]
[Singleton(typeof(TrackService))]
[Singleton(typeof(TrackSplineService))]
[Singleton(typeof(VehicleJunctionCrossingService))]
[Singleton(typeof(VehicleJunctionService))]
[Singleton(typeof(VehicleMovementService))]
[Singleton(typeof(VehiclePositionService))]
[Singleton(typeof(VehicleService))]
[Singleton(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
[Transient(typeof(CommandHandlerServiceCollection), Factory= nameof(GetCommandHandlerServiceCollection))]
[Transient(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
[Transient(typeof(DaylightManager), Factory = nameof(GetDaylightManager))]
[Transient(typeof(ILoggingManager), Factory = nameof(GetLoggingManager))]
public partial class GameSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private CommandHandlerServiceCollection GetCommandHandlerServiceCollection() => gameProvider.GetService<CommandHandlerServiceCollection>();
    private DaylightManager GetDaylightManager() => gameProvider.GetRequiredService<DaylightManager>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetRequiredService<DayTimeManager>();
    private ILoggingManager GetLoggingManager() => gameProvider.GetRequiredService<ILoggingManager>();
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    
    private IEnumerable<SceneService> GetAllSceneServices() =>
    [
        this.GetRequiredService<AddJunctionRuleHandlerService>(),
        this.GetRequiredService<ClearJunctionSignalRules>(),
        this.GetRequiredService<JunctionSignalRuleService>(),
        this.GetRequiredService<JunctionSignalService>(),
        this.GetRequiredService<JunctionUpdatingService>(),
        this.GetRequiredService<PlaceVehicleHandlerService>(),
        this.GetRequiredService<SceneLightService>(),
        this.GetRequiredService<SetTimeHandlerService>(),
        this.GetRequiredService<StationPlatformAreaService>(),
        this.GetRequiredService<StationPlatformTrackDeletionService>(),
        this.GetRequiredService<TerrainService>(),
        this.GetRequiredService<TrackSplineService>(),
        this.GetRequiredService<VehicleJunctionCrossingService>(),
        this.GetRequiredService<VehicleMovementService>(),
    ];

}