using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Scenes;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Game;

[ServiceProvider]
[Singleton(typeof(CoreGameService))]
[Singleton(typeof(SceneLightService))]
[Singleton(typeof(TrackService))]
[Singleton(typeof(CameraSceneService))]
[Singleton(typeof(GLService))]
[Singleton(typeof(TrackArrowRenderingService))]
[Transient(typeof(MeshEntityManager), Factory = nameof(GetMeshEntityManager))]
[Transient(typeof(HeightmapEntityManager), Factory = nameof(GetHeightmapEntityManager))]
[Transient(typeof(ShaderEntityManager), Factory = nameof(GetShaderEntityManager))]
[Transient(typeof(TextureEntityManager), Factory = nameof(GetTextureEntityManager))]
[Transient(typeof(RenderingManager), Factory = nameof(GetRenderingManager))]
[Transient(typeof(DaylightManager), Factory = nameof(GetDaylightManager))]
[Transient(typeof(DayTimeManager), Factory = nameof(GetDayTimeManager))]
[Transient(typeof(KeyboardManager), Factory = nameof(GetKeyboardManager))]
[Transient(typeof(MouseManager), Factory = nameof(GetMouseManager))]
[Transient(typeof(Provider<IWindow>), Factory = nameof(GetWindowProvider))]
[Transient(typeof(Provider<GL>), Factory = nameof(GetGLProvider))]
[Singleton(typeof(IEnumerable<SceneService>), Factory = nameof(GetAllSceneServices))]
    
public partial class GameSceneProvider(GameProvider gameProvider) : ISceneServicesProvider
{
    private MeshEntityManager GetMeshEntityManager() => gameProvider.GetRequiredService<MeshEntityManager>();
    private HeightmapEntityManager GetHeightmapEntityManager() => gameProvider.GetRequiredService<HeightmapEntityManager>();
    private ShaderEntityManager GetShaderEntityManager() => gameProvider.GetRequiredService<ShaderEntityManager>();
    private TextureEntityManager GetTextureEntityManager() => gameProvider.GetRequiredService<TextureEntityManager>();
    private RenderingManager GetRenderingManager() => gameProvider.GetRequiredService<RenderingManager>();
    private DaylightManager GetDaylightManager() => gameProvider.GetRequiredService<DaylightManager>();
    private DayTimeManager GetDayTimeManager() => gameProvider.GetRequiredService<DayTimeManager>();
    private KeyboardManager GetKeyboardManager() => gameProvider.GetRequiredService<KeyboardManager>();
    private MouseManager GetMouseManager() => gameProvider.GetRequiredService<MouseManager>();
    private Provider<IWindow> GetWindowProvider() => gameProvider.GetRequiredService<Provider<IWindow>>();
    private Provider<GL> GetGLProvider() => gameProvider.GetRequiredService<Provider<GL>>();
    public IEnumerable<SceneService> GetSceneServices() => this.GetServices<SceneService>();
    
    private IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider) =>
    [
        provider.GetRequiredService<CoreGameService>(),
        provider.GetRequiredService<SceneLightService>(),
        provider.GetRequiredService<GLService>(),
        provider.GetRequiredService<TrackService>(),
        provider.GetRequiredService<CameraSceneService>()
    ];

}