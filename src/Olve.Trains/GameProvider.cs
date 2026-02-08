using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Logging;
using Olve.Trains.Scenes.Game;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI;

namespace Olve.Trains;

[ServiceProvider]
[Singleton(typeof(GameManager))]
[Singleton(typeof(SceneManager))]
[Singleton(typeof(KeyboardManager))]
[Singleton(typeof(MouseManager))]
[Singleton(typeof(DayTimeManager))]
[Singleton(typeof(DaylightManager))]
[Singleton(typeof(ScreenResizedEvent))]
[Singleton(typeof(TextureLoadingService))]
[Singleton(typeof(CommandHandlerServiceCollection))]
[Singleton(typeof(GameSceneProvider))]
[Singleton(typeof(UISceneProvider))]
[Singleton(typeof(RenderingSceneProvider))]
[Singleton(typeof(IEnumerable<IScene>), Factory = nameof(GetAllScenes))]
[Singleton(typeof(GameProvider), Factory = nameof(GetGameProvider))]
[Singleton(typeof(InMemoryLoggingManager))]
[Singleton(typeof(ILoggingManager), Factory = nameof(GetLoggingManager))]
[Import(typeof(IWindowingProvider))]
[Import(typeof(IOpenGLProvider))]
public partial class GameProvider
{
    public GameProvider GetGameProvider() => this;

    private ILoggingManager GetLoggingManager(IServiceProvider provider)
    {
        var inMemoryLogger = provider.GetRequiredService<InMemoryLoggingManager>();
        return new ConsoleLoggingManager(inMemoryLogger);
    }

    private IEnumerable<IScene> GetAllScenes(IServiceProvider provider)
    {
        var loggingManager = provider.GetRequiredService<ILoggingManager>();

        return
        [
            new Scene<GameSceneProvider>(loggingManager, provider.GetRequiredService<GameSceneProvider>(), SceneIds.GameScene),
            new Scene<RenderingSceneProvider>(loggingManager, provider.GetRequiredService<RenderingSceneProvider>(), SceneIds.RenderingScene, 1),
            new Scene<UISceneProvider>(loggingManager, provider.GetRequiredService<UISceneProvider>(), SceneIds.UIScene, 2),
        ];
    }
}
