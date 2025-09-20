using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game;
using Olve.Trains.Scenes.Game.Junctions;
using Olve.Trains.Scenes.Game.Light;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Terrain;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Rendering;

[ServiceProvider]
[Singleton(typeof(GLService))]
[Singleton(typeof(CameraSceneService))]
[Singleton(typeof(TrackRenderingService))]
[Singleton(typeof(VehicleRenderingService))]
[Singleton(typeof(JunctionSignalRenderingService))]
[Singleton(typeof(TerrainRenderingService))]
[Singleton(typeof(TerrainRaycastService))]
[Singleton(typeof(IEnumerable<SceneService>), Factory=nameof(GetAllSceneServices))]
[Transient(typeof(ILoggingManager), Factory=nameof(GetLoggingManager))]
[Transient(typeof(OpenGLShaderManager), Factory=nameof(GetOpenGLShaderManager))]
[Transient(typeof(OpenGLModelRenderingManager), Factory=nameof(GetOpenGLModelRenderingManager))]
[Transient(typeof(TextureEntityManager), Factory=nameof(GetTextureEntityManager))]
[Transient(typeof(ShaderEntityManager), Factory=nameof(GetShaderEntityManager))]
[Transient(typeof(MeshEntityManager), Factory=nameof(GetMeshEntityManager))]
[Transient(typeof(LineStripEntityManager), Factory=nameof(GetLineStripEntityManager))]
[Transient(typeof(RenderingManager3D), Factory=nameof(GetRenderingManager3D))]
[Transient(typeof(TerrainService), Factory=nameof(GetTerrainService))]
[Transient(typeof(KeyboardManager), Factory=nameof(GetKeyboardManager))]
[Transient(typeof(MouseManager), Factory=nameof(GetMouseManager))]
[Transient(typeof(HeightmapEntityManager), Factory=nameof(GetHeightmapEntityManager))]
[Transient(typeof(SceneLightService), Factory=nameof(GetSceneLightService))]
[Transient(typeof(TrackSplineService), Factory=nameof(GetTrackSplineService))]
[Transient(typeof(TrackService), Factory=nameof(GetTrackService))]
[Transient(typeof(StationPlatformService), Factory=nameof(GetStationPlatformService))]
[Transient(typeof(JunctionService), Factory=nameof(GetJunctionService))]
[Transient(typeof(JunctionSignalService), Factory=nameof(GetJunctionSignalService))]
[Transient(typeof(VehicleService), Factory=nameof(GetVehicleService))]
[Transient(typeof(VehiclePositionService), Factory=nameof(GetVehiclePositionService))]
[Transient(typeof(ScreenResizedEvent), Factory=nameof(GetScreenResizedEvent))]
[Transient(typeof(Provider<GL>), Factory=nameof(GetGLProvider))]
[Transient(typeof(Provider<IWindow>), Factory=nameof(GetWindowProvider))]
public partial class RenderingSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private ILoggingManager GetLoggingManager() => gameProvider.GetRequiredService<ILoggingManager>();
    private OpenGLModelRenderingManager GetOpenGLModelRenderingManager() => gameProvider.GetRequiredService<OpenGLModelRenderingManager>();
    private OpenGLShaderManager GetOpenGLShaderManager() => gameProvider.GetRequiredService<OpenGLShaderManager>();
    private TextureEntityManager GetTextureEntityManager() => gameProvider.GetRequiredService<TextureEntityManager>();
    private ShaderEntityManager GetShaderEntityManager() => gameProvider.GetRequiredService<ShaderEntityManager>();
    private MeshEntityManager GetMeshEntityManager() => gameProvider.GetRequiredService<MeshEntityManager>();
    private LineStripEntityManager GetLineStripEntityManager() => gameProvider.GetRequiredService<LineStripEntityManager>();
    private RenderingManager3D GetRenderingManager3D() => gameProvider.GetRequiredService<RenderingManager3D>();
    private KeyboardManager GetKeyboardManager() => gameProvider.GetRequiredService<KeyboardManager>();
    private MouseManager GetMouseManager() => gameProvider.GetRequiredService<MouseManager>();
    private HeightmapEntityManager GetHeightmapEntityManager() => gameProvider.GetRequiredService<HeightmapEntityManager>();
    private SceneLightService GetSceneLightService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<SceneLightService>();
    private TerrainService GetTerrainService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TerrainService>();
    private TrackSplineService GetTrackSplineService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TrackSplineService>();
    private TrackService GetTrackService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TrackService>();
    private StationPlatformService GetStationPlatformService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<StationPlatformService>();
    private JunctionService GetJunctionService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<JunctionService>();
    private JunctionSignalService GetJunctionSignalService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<JunctionSignalService>();
    private VehicleService GetVehicleService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<VehicleService>();
    private VehiclePositionService GetVehiclePositionService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<VehiclePositionService>();
    private ScreenResizedEvent GetScreenResizedEvent() => gameProvider.GetRequiredService<ScreenResizedEvent>();
    private Provider<GL> GetGLProvider() => gameProvider.GetRequiredService<Provider<GL>>();
    private Provider<IWindow> GetWindowProvider() => gameProvider.GetRequiredService<Provider<IWindow>>();
    
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    
    private IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<GLService>(),
        provider.GetRequiredService<CameraSceneService>(),
        provider.GetRequiredService<TerrainRenderingService>(),
        provider.GetRequiredService<TrackRenderingService>(),
        provider.GetRequiredService<VehicleRenderingService>(),
        provider.GetRequiredService<JunctionSignalRenderingService>(),
        provider.GetRequiredService<TerrainRaycastService>(),
    ];
}