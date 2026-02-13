using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // Engine infrastructure (singletons - shared across all scopes)
        services.AddSingleton<GameManager>();
        services.AddSingleton<SceneManager>();
        services.AddSingleton<KeyboardManager>();
        services.AddSingleton<MouseManager>();
        services.AddSingleton<DayTimeManager>();
        services.AddSingleton<DaylightManager>();
        services.AddSingleton<ScreenResizedEvent>();
        services.AddSingleton<CommandHandlerServiceCollection>();
        services.AddSingleton<EventQueueFactory>();

        // Scene services
        services.AddMainMenuSceneServices();
        services.AddGameLogicSceneServices();
        services.AddGameRenderingSceneServices();
        services.AddUISceneServices();

        // Scene definitions
        services.AddSingleton<IEnumerable<SceneDefinition>>(_ =>
        [
            new(SceneIds.MainMenuScene, "MainMenuScene", LayerOrder: 0),
            new(SceneIds.GameLogicScene, "GameScene"),
            new(SceneIds.GameRenderingScene, "RenderingScene", LayerOrder: 1,
                ParentId: SceneIds.GameLogicScene),
            new(SceneIds.GameUIScene, "UIScene", LayerOrder: 2,
                ParentId: SceneIds.GameRenderingScene),
        ]);

        return services;
    }
}
