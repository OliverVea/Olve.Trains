using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Scenes.GUI;

namespace Olve.Trains.Scenes.MainMenu;

public static class MainMenuSceneServiceRegistration
{
    public static IServiceCollection AddMainMenuSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.MainMenuScene;

        // GL setup and clear
        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);

        // Shared GUI services (rendering, layout, text, input, etc.)
        services.AddGuiSceneServices(sceneId);

        // Command processing
        services.AddSceneService<CommandProcessingService>(sceneId);

        // Main menu command handlers
        services.AddSceneService<StartGameCommandHandler>(sceneId);
        services.AddSceneService<ActivateGuiHandlerService>(sceneId);

        // Main menu-specific services
        services.AddSceneService<MainMenuService>(sceneId);
        services.AddSceneService<MainMenuStyleService>(sceneId);

        return services;
    }
}
