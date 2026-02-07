using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Logging;
using Olve.Trains.Scenes.Game;
using Olve.Trains.Scenes.Game.Stations;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI.GUI;
using Olve.Trains.Scenes.UI.Indicators;
using Olve.Trains.Scenes.UI.Tools;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.UI;

[ServiceProvider]
[Singleton(typeof(ToolManagementService))]
[Singleton(typeof(TrackPlacingToolService))]
[Singleton(typeof(TrainPlacingToolService))]
[Singleton(typeof(StationPlacingToolService))]
[Singleton(typeof(TrackArrowIndicatorService))]
[Singleton(typeof(ToolBarService))]
[Singleton(typeof(GuiLayoutUpdateService))]
[Singleton(typeof(GuiLayoutContextUpdater))]
[Singleton(typeof(GuiDepthService))]
[Singleton(typeof(GuiRectangleRenderingService))]
[Singleton(typeof(GuiRectangleUpdateService))]
[Singleton(typeof(GuiTextRenderingService))]
[Singleton(typeof(GameStyleService))]
[Singleton(typeof(GuiTextUpdateService))]
[Singleton(typeof(InfoBarService))]
[Singleton(typeof(Provider<LayoutContext>))]
[Singleton(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
[Singleton(typeof(RenderingManager2D), Factory = nameof(GetRenderingManager2D))]
[Transient(typeof(ILoggingManager), Factory = nameof(GetLoggingManager))]
[Transient(typeof(TextureLoadingService), Factory = nameof(GetTextureLoadingService))]
[Transient(typeof(TerrainRaycastService), Factory = nameof(GetTerrainRaycastService))]
[Transient(typeof(MouseManager), Factory = nameof(GetMouseManager))]
[Transient(typeof(KeyboardManager), Factory = nameof(GetKeyboardManager))]
[Transient(typeof(MeshEntityManager), Factory = nameof(GetMeshEntityManager))]
[Transient(typeof(ShaderEntityManager), Factory = nameof(GetShaderEntityManager))]
[Transient(typeof(TextureEntityManager), Factory = nameof(GetTextureEntityManager))]
[Transient(typeof(CameraSceneService), Factory = nameof(GetCameraSceneService))]
[Transient(typeof(RenderingManager3D), Factory = nameof(GetRenderingManager3D))]
[Transient(typeof(TrackService), Factory = nameof(GetTrackService))]
[Transient(typeof(ScreenResizedEvent), Factory = nameof(GetScreenResizedEvent))]
[Transient(typeof(Provider<IWindow>), Factory = nameof(GetWindowProvider))]
[Transient(typeof(TrackPlacingService), Factory = nameof(GetTrackPlacingService))]
[Transient(typeof(TrackSplineService), Factory = nameof(GetTrackSplineService))]
[Transient(typeof(VehicleService), Factory = nameof(GetVehicleService))]
[Transient(typeof(VehiclePositionService), Factory = nameof(GetVehiclePositionService))]
[Transient(typeof(StationService), Factory = nameof(GetStationService))]
[Transient(typeof(StationPlatformService), Factory = nameof(GetStationPlatformService))]
[Transient(typeof(StationNameGenerator), Factory = nameof(GetStationNameGenerator))]
[Transient(typeof(AssetLoader), Factory = nameof(GetAssetLoader))]
[Transient(typeof(RenderingServiceHelper), Factory = nameof(GetRenderingServiceHelper))]
[Transient(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
[Import(typeof(IGuiProvider))]
public partial class UISceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private TerrainRaycastService GetTerrainRaycastService() => gameProvider.GetRequiredService<RenderingSceneProvider>().GetRequiredService<TerrainRaycastService>();
    private CameraSceneService GetCameraSceneService() => gameProvider.GetRequiredService<RenderingSceneProvider>().GetRequiredService<CameraSceneService>();
    private TrackService GetTrackService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TrackService>();
    private TrackSplineService GetTrackSplineService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TrackSplineService>();
    private TrackPlacingService GetTrackPlacingService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<TrackPlacingService>();
    private VehicleService GetVehicleService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<VehicleService>();
    private VehiclePositionService GetVehiclePositionService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<VehiclePositionService>();
    private StationService GetStationService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<StationService>();
    private StationPlatformService GetStationPlatformService() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<StationPlatformService>();
    private StationNameGenerator GetStationNameGenerator() => gameProvider.GetRequiredService<GameSceneProvider>().GetRequiredService<StationNameGenerator>();
    private Provider<IWindow> GetWindowProvider() => gameProvider.GetRequiredService<Provider<IWindow>>();
    private ScreenResizedEvent GetScreenResizedEvent() => gameProvider.GetRequiredService<ScreenResizedEvent>();
    private MouseManager GetMouseManager() => gameProvider.GetRequiredService<MouseManager>();
    private KeyboardManager GetKeyboardManager() => gameProvider.GetRequiredService<KeyboardManager>();
    private MeshEntityManager GetMeshEntityManager() => gameProvider.GetRequiredService<MeshEntityManager>();
    private ShaderEntityManager GetShaderEntityManager() => gameProvider.GetRequiredService<ShaderEntityManager>();
    private TextureEntityManager GetTextureEntityManager() => gameProvider.GetRequiredService<TextureEntityManager>();
    private RenderingManager3D GetRenderingManager3D() => gameProvider.GetRequiredService<RenderingManager3D>();
    private RenderingManager2D GetRenderingManager2D() => gameProvider.GetRequiredService<RenderingManager2D>();
    private ILoggingManager GetLoggingManager() => gameProvider.GetRequiredService<ILoggingManager>();
    private TextureLoadingService GetTextureLoadingService() => gameProvider.GetRequiredService<TextureLoadingService>();
    private AssetLoader GetAssetLoader() => gameProvider.GetRequiredService<AssetLoader>();
    private RenderingServiceHelper GetRenderingServiceHelper() => gameProvider.GetRequiredService<RenderingServiceHelper>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetRequiredService<DayTimeManager>();

    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    private IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<TrackArrowIndicatorService>(),
        provider.GetRequiredService<TrackPlacingToolService>(),
        provider.GetRequiredService<TrainPlacingToolService>(),
        provider.GetRequiredService<StationPlacingToolService>(),
        provider.GetRequiredService<ToolBarService>(),
        provider.GetRequiredService<InfoBarService>(),
        provider.GetRequiredService<GuiLayoutContextUpdater>(),
        provider.GetRequiredService<GuiLayoutUpdateService>(),
        provider.GetRequiredService<GuiRectangleRenderingService>(),
        provider.GetRequiredService<GuiRectangleUpdateService>(),
        provider.GetRequiredService<GuiTextRenderingService>(),
        provider.GetRequiredService<GuiTextUpdateService>(),
        provider.GetRequiredService<TextureLoadingService>(),
        provider.GetRequiredService<GameStyleService>(),

        ..IGuiProvider.GetAllSceneServices(provider)
    ];
}