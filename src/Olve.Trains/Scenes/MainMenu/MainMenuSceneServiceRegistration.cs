using Microsoft.Extensions.DependencyInjection;

namespace Olve.Trains.Scenes.MainMenu;

public static class MainMenuSceneServiceRegistration
{

    public static IServiceCollection AddMainMenuSceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.MainMenuScene;

        return services;
    }
}