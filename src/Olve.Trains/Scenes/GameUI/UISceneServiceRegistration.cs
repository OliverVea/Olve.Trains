using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Scenes.GameUI.Indicators;
using Olve.Trains.Scenes.GameUI.Tools;
using Olve.Trains.Shared.GUI;

namespace Olve.Trains.Scenes.GameUI;

public static class UISceneServiceRegistration
{
    public static IServiceCollection AddUISceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameUIScene;

        // Unified rendering manager (calls RenderAll for this scene's groups)
        services.AddSceneService<RenderingManagerSceneService>(sceneId);

        // Shared GUI services (rendering, layout, text, input, etc.)
        services.AddGuiSceneServices(sceneId);

        // Mouse raycasting (per-frame hit cache)
        services.AddSceneService<MouseRaycastService>(sceneId);

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
        services.AddSceneService<BurgerMenuService>(sceneId);
        services.AddSceneService<SignalRulesPanelService>(sceneId);
        services.AddSceneService<ActivateGuiHandlerService>(sceneId);

        services.AddSceneService<ScreenshotService>(sceneId);

        // Game-specific GUI services
        services.AddSceneService<GameStyleService>(sceneId);

        // Non-scene singletons
        services.TryAddScoped<ToolManagementService>();

        return services;
    }
}
