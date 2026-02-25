using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Scenes.GameUI.Indicators;
using Olve.Trains.Scenes.GameUI.Tools;
using Olve.Trains.Scenes.GUI;

namespace Olve.Trains.Scenes.GameUI;

public static class UISceneServiceRegistration
{
    public static IServiceCollection AddUISceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameUIScene;

        // Shared GUI services (rendering, layout, text, input, etc.)
        services.AddGuiSceneServices(sceneId);

        // Tool / indicator services
        services.AddSceneService<TrackArrowIndicatorService>(sceneId);
        services.AddSceneService<TrackPlacingToolService>(sceneId);
        services.AddSceneService<TrainPlacingToolService>(sceneId);
        services.AddSceneService<StationPlacingToolService>(sceneId);
        services.AddSceneService<ResidencePlacingToolService>(sceneId);
        services.AddSceneService<DeletionToolService>(sceneId);
        services.AddSceneService<SelectToolHandlerService>(sceneId);
        services.AddSceneService<ToolBarService>(sceneId);
        services.AddSceneService<InfoBarService>(sceneId);

        services.AddSceneService<ScreenshotService>(sceneId);

        // Game-specific GUI services
        services.AddSceneService<GameStyleService>(sceneId);

        // Non-scene singletons
        services.TryAddScoped<ToolManagementService>();

        return services;
    }
}
