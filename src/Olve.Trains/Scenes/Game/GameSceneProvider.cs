using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game.CommandHandlers;
using Olve.Trains.Scenes.Game.Terrain;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Game;

[ServiceProvider]
[Singleton(typeof(SceneLightService))]
[Singleton(typeof(TrackArrowRenderingService))]
[Singleton(typeof(TerrainService))]
[Singleton(typeof(TerrainRaycastService))]
[Singleton(typeof(TerrainRenderingService))]
[Singleton(typeof(CameraSceneService))]
[Singleton(typeof(GLService))]
[Singleton(typeof(TrackPlacingService))]
[Singleton(typeof(TrackService))]
[Singleton(typeof(TrackSplineService))]
[Singleton(typeof(TrackRenderingService))]
[Singleton(typeof(TrackJunctionUpdatingService))]
[Singleton(typeof(TrackConnectionService))]
[Singleton(typeof(TrackJunctionService))]
[Singleton(typeof(SetTimeHandlerService))]
[Singleton(typeof(VehicleService))]
[Singleton(typeof(VehiclePositionService))]
[Singleton(typeof(PlaceVehicleHandlerService))]
[Singleton(typeof(VehicleRenderingService))]
[Singleton(typeof(VehicleMovementService))]
[Singleton(typeof(CommandHandlerServiceCollection), Factory= nameof(GetCommandHandlerServiceCollection))]
[Transient(typeof(MeshEntityManager), Factory = nameof(GetMeshEntityManager))]
[Transient(typeof(HeightmapEntityManager), Factory = nameof(GetHeightmapEntityManager))]
[Transient(typeof(ShaderEntityManager), Factory = nameof(GetShaderEntityManager))]
[Transient(typeof(TextureEntityManager), Factory = nameof(GetTextureEntityManager))]
[Transient(typeof(LineStripEntityManager), Factory = nameof(GetLineStripEntityManager))]
[Transient(typeof(OpenGLModelRenderingManager), Factory = nameof(GetOpenGLModelRenderingManager))]
[Transient(typeof(RenderingManager3D), Factory = nameof(GetRenderingManager))]
[Transient(typeof(DaylightManager), Factory = nameof(GetDaylightManager))]
[Transient(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
[Transient(typeof(KeyboardManager), Factory = nameof(GetKeyboardManager))]
[Transient(typeof(MouseManager), Factory = nameof(GetMouseManager))]
[Transient(typeof(Provider<IWindow>), Factory = nameof(GetWindowProvider))]
[Transient(typeof(Provider<GL>), Factory = nameof(GetGLProvider))]
[Transient(typeof(ILoggingManager), Factory = nameof(GetLoggingManager))]
[Singleton(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
public partial class GameSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private CommandHandlerServiceCollection GetCommandHandlerServiceCollection() => gameProvider.GetService<CommandHandlerServiceCollection>();
    private MeshEntityManager GetMeshEntityManager() => gameProvider.GetRequiredService<MeshEntityManager>();
    private HeightmapEntityManager GetHeightmapEntityManager() => gameProvider.GetRequiredService<HeightmapEntityManager>();
    private ShaderEntityManager GetShaderEntityManager() => gameProvider.GetRequiredService<ShaderEntityManager>();
    private TextureEntityManager GetTextureEntityManager() => gameProvider.GetRequiredService<TextureEntityManager>();
    private LineStripEntityManager GetLineStripEntityManager() => gameProvider.GetRequiredService<LineStripEntityManager>();
    private OpenGLModelRenderingManager GetOpenGLModelRenderingManager() => gameProvider.GetRequiredService<OpenGLModelRenderingManager>();
    private RenderingManager3D GetRenderingManager() => gameProvider.GetRequiredService<RenderingManager3D>();
    private DaylightManager GetDaylightManager() => gameProvider.GetRequiredService<DaylightManager>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetRequiredService<DayTimeManager>();
    private KeyboardManager GetKeyboardManager() => gameProvider.GetRequiredService<KeyboardManager>();
    private MouseManager GetMouseManager() => gameProvider.GetRequiredService<MouseManager>();
    private Provider<IWindow> GetWindowProvider() => gameProvider.GetRequiredService<Provider<IWindow>>();
    private Provider<GL> GetGLProvider() => gameProvider.GetRequiredService<Provider<GL>>();
    private ILoggingManager GetLoggingManager() => gameProvider.GetRequiredService<ILoggingManager>();
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    
    private IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<SceneLightService>(),
        provider.GetRequiredService<GLService>(),
        provider.GetRequiredService<TrackArrowRenderingService>(),
        provider.GetRequiredService<CameraSceneService>(),
        provider.GetRequiredService<TerrainRenderingService>(),
        provider.GetRequiredService<TerrainRaycastService>(),
        provider.GetRequiredService<TerrainService>(),
        provider.GetRequiredService<TrackPlacingService>(),
        provider.GetRequiredService<TrackSplineService>(),
        provider.GetRequiredService<TrackRenderingService>(),
        provider.GetRequiredService<TrackJunctionUpdatingService>(),
        provider.GetRequiredService<SetTimeHandlerService>(),
        provider.GetRequiredService<PlaceVehicleHandlerService>(),
        provider.GetRequiredService<VehicleRenderingService>(),
        provider.GetRequiredService<VehicleMovementService>(),
    ];

}