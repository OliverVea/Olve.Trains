using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameRendering;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Shared.GUI;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.MainMenu;

public static class MainMenuSceneServiceRegistration
{
    public static IServiceCollection AddMainMenuSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.MainMenuScene;

        services.AddSceneService<GLService>(sceneId);
        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<SharedRenderingService>(sceneId);

        services.AddGuiSceneServices(sceneId);

        services.AddSceneService<CommandProcessingService>(sceneId);

        services.AddSceneService<ActivateGuiHandlerService>(sceneId);

        services.AddSceneService<MainMenuService>(sceneId);

        return services;
    }
}
