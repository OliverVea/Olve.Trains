using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Events;
using Olve.Engine3D.GUI;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Time;
using Olve.Engine3D.TimeStepping;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GameUI;
using Olve.Trains.Scenes.MainMenu;
using Olve.Trains.Telemetry;

namespace Olve.Trains;

public static class GameServiceRegistration
{
    public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration, GameInstanceId instanceId, bool listen, bool manual)
    {
        // Logging & Metrics
        services.AddLogging(builder => builder.AddConfiguredLogging(configuration));
        services.AddConfiguredMetrics(configuration);

        // Engine modules
        services.AddCoreEngineServices();
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
        services.AddSingleton<AfterRenderEvent>();
        services.AddSingleton<GameClosingEvent>();
        if (manual)
        {
            services.AddSingleton<ManualStepper>();
            services.AddSingleton<ITimeStepper>(sp => sp.GetRequiredService<ManualStepper>());
        }
        else
        {
            services.AddSingleton<ITimeStepper, WindowStepper>();
        }
        services.AddSingleton<ScreenshotManager>();
        services.AddSingleton<CommandHandlerServiceCollection>();
        services.AddSingleton<EventQueueFactory>();

        // Command infrastructure
        services.AddSingleton(instanceId);
        services.AddSingleton<CommandRunner>();
        services.AddSingleton<CommandQueue>();
        services.AddSingleton<EchoCommandHandler>();
        services.AddSingleton<HelpCommandHandler>();
        services.AddSingleton<ExitCommandHandler>();
        services.AddSingleton<ScreenshotCommandHandler>();
        if (manual)
        {
            services.AddSingleton<StepCommandHandler>();
        }
        if (listen)
        {
            services.AddSingleton<CommandPipeServer>();
        }

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
