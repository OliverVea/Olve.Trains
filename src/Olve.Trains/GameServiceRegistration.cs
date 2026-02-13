using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GameUI;
using Olve.Trains.Scenes.MainMenu;
using Olve.Trains.Telemetry;

namespace Olve.Trains;

public static class GameServiceRegistration
{
    public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Logging
        services.AddLogging(builder => builder.AddConfiguredLogging(configuration));

        // Engine modules
        services.AddWindowingServices();
        services.AddOpenGLServices();
        services.AddGuiServices();

        // Core singletons
        services.TryAddScoped<GameManager>();
        services.TryAddScoped<SceneManager>();
        services.TryAddScoped<KeyboardManager>();
        services.TryAddScoped<MouseManager>();
        services.TryAddScoped<DayTimeManager>();
        services.TryAddScoped<DaylightManager>();
        services.TryAddScoped<ScreenResizedEvent>();
        services.TryAddScoped<TextureLoadingManager>();
        services.TryAddScoped<CommandHandlerServiceCollection>();
        services.TryAddScoped<EventQueueFactory>();

        // Scene services
        services.AddMainMenuSceneServices();
        services.AddGameLogicSceneServices();
        services.AddGameRenderingSceneServices();
        services.AddUISceneServices();

        // Scenes
        services.TryAddScoped<IEnumerable<IScene>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Scene>>();
            return
            [
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.MainMenuScene),
                    SceneIds.MainMenuScene, "MainMenuScene", 0),
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.GameLogicScene),
                    SceneIds.GameLogicScene, "GameScene"),
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.GameRenderingScene),
                    SceneIds.GameRenderingScene, "RenderingScene", 1),
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.GameUIScene),
                    SceneIds.GameUIScene, "UIScene", 2),
            ];
        });

        return services;
    }
}
