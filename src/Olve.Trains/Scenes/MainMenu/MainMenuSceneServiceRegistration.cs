using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GUI;

namespace Olve.Trains.Scenes.MainMenu;

public static class MainMenuSceneServiceRegistration
{
    public static IServiceCollection AddMainMenuSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.MainMenuScene;

        // GL setup and clear
        services.AddSceneService<GLService>(sceneId);

        // Shared GUI services (rendering, layout, text, input, etc.)
        services.AddGuiSceneServices(sceneId);

        // Main menu-specific services
        services.AddSceneService<MainMenuService>(sceneId);
        services.AddSceneService<MainMenuStyleService>(sceneId);

        return services;
    }
}
