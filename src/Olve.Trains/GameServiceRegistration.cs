using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI;
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

public static class GameServiceRegistration
{
    public static IServiceCollection AddAllServices(this IServiceCollection services)
    {
        // Engine modules
        services.AddWindowingServices();
        services.AddOpenGLServices();
        services.AddGuiServices();

        // Core singletons
        services.AddSingleton<GameManager>();
        services.AddSingleton<SceneManager>();
        services.AddSingleton<KeyboardManager>();
        services.AddSingleton<MouseManager>();
        services.AddSingleton<DayTimeManager>();
        services.AddSingleton<DaylightManager>();
        services.AddSingleton<ScreenResizedEvent>();
        services.AddSingleton<TextureLoadingManager>();
        services.AddSingleton<CommandHandlerServiceCollection>();

        // ILoggingManager (temporary, replaced in follow-up)
        services.AddSingleton<InMemoryLoggingManager>();
        services.AddSingleton<ILoggingManager>(sp =>
            new ConsoleLoggingManager(sp.GetRequiredService<InMemoryLoggingManager>()));

        // Scene services
        services.AddGameSceneServices();
        services.AddRenderingSceneServices();
        services.AddUISceneServices();

        // Scenes
        services.AddSingleton<IEnumerable<IScene>>(sp =>
        {
            var logging = sp.GetRequiredService<ILoggingManager>();
            return
            [
                new Scene(logging, sp.GetKeyedServices<SceneService>(SceneIds.GameScene),
                    SceneIds.GameScene, "GameScene"),
                new Scene(logging, sp.GetKeyedServices<SceneService>(SceneIds.RenderingScene),
                    SceneIds.RenderingScene, "RenderingScene", 1),
                new Scene(logging, sp.GetKeyedServices<SceneService>(SceneIds.UIScene),
                    SceneIds.UIScene, "UIScene", 2),
            ];
        });

        return services;
    }
}
