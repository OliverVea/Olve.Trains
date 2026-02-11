using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.Game;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI;
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
        services.AddSingleton<GameManager>();
        services.AddSingleton<SceneManager>();
        services.AddSingleton<KeyboardManager>();
        services.AddSingleton<MouseManager>();
        services.AddSingleton<DayTimeManager>();
        services.AddSingleton<DaylightManager>();
        services.AddSingleton<ScreenResizedEvent>();
        services.AddSingleton<TextureLoadingManager>();
        services.AddSingleton<CommandHandlerServiceCollection>();

        // Scene services
        services.AddGameSceneServices();
        services.AddRenderingSceneServices();
        services.AddUISceneServices();

        // Scenes
        services.AddSingleton<IEnumerable<IScene>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Scene>>();
            return
            [
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.GameScene),
                    SceneIds.GameScene, "GameScene"),
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.RenderingScene),
                    SceneIds.RenderingScene, "RenderingScene", 1),
                new Scene(logger, sp.GetKeyedServices<ISceneService>(SceneIds.UIScene),
                    SceneIds.UIScene, "UIScene", 2),
            ];
        });

        return services;
    }
}
