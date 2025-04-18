using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Light;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;

namespace Olve.Trains.Scenes.Game;

[ServiceProvider(RootServices = [typeof(ISceneService)])]
[Singleton(typeof(SceneLightService))]
[Singleton(typeof(ISceneService), Factory = nameof(GetSceneLightService))]
[Singleton(typeof(TrackArrowRenderingService))]
[Singleton(typeof(TrackService))]
[Singleton(typeof(ISceneService), Factory = nameof(GetTrackService))]
[Singleton(typeof(CameraSceneService))]
[Singleton(typeof(ISceneService), Factory = nameof(GetCameraSceneService))]
[Transient(typeof(MeshEntityManager), Factory = nameof(GetMeshEntityManager))]
[Transient(typeof(HeightmapEntityManager), Factory = nameof(GetHeightmapEntityManager))]
[Transient(typeof(ShaderEntityManager), Factory = nameof(GetShaderEntityManager))]
[Transient(typeof(TextureEntityManager), Factory = nameof(GetTextureEntityManager))]
[Transient(typeof(RenderingManager), Factory = nameof(GetRenderingManager))]
[Transient(typeof(DaylightManager), Factory = nameof(GetDaylightManager))]
[Transient(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
public partial class GameSceneProvider(Trains.GameProvider gameProvider)
{
    private static SceneLightService GetSceneLightService(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<SceneLightService>();

    private static TrackService GetTrackService(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<TrackService>();

    private static CameraSceneService GetCameraSceneService(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<CameraSceneService>();

    private MeshEntityManager GetMeshEntityManager() => gameProvider.GetRequiredService<MeshEntityManager>();
    private HeightmapEntityManager GetHeightmapEntityManager() => gameProvider.GetRequiredService<HeightmapEntityManager>();
    private ShaderEntityManager GetShaderEntityManager() => gameProvider.GetRequiredService<ShaderEntityManager>();
    private TextureEntityManager GetTextureEntityManager() => gameProvider.GetRequiredService<TextureEntityManager>();
    private RenderingManager GetRenderingManager() => gameProvider.GetRequiredService<RenderingManager>();
    private DaylightManager GetDaylightManager() => gameProvider.GetRequiredService<DaylightManager>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetRequiredService<DayTimeManager>();
}