using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Scenes.GameUI.Indicators;
using Olve.Trains.Scenes.GameUI.Tools;
using Olve.Trains.Shared.GUI;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.GameUI;

public static class UISceneServiceRegistration
{
    public static IServiceCollection AddUISceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameUIScene;

        services.AddSceneService<RenderingManagerSceneService>(sceneId);
        services.AddSceneService<SharedRenderingService>(sceneId);

        services.AddGuiSceneServices(sceneId);

        services.AddSceneService<MouseRaycastService>(sceneId);

        services.AddSceneService<TrackArrowIndicatorService>(sceneId);
        services.AddSceneService<TrackPlacingToolService>(sceneId);
        services.TryAddScoped<BuildingPlacementToolService>();
        services.AddSceneService<StationPlacingToolService>(sceneId);
        services.AddSceneService<ResidencePlacingToolService>(sceneId);
        services.AddSceneService<ForestPlacingToolService>(sceneId);
        services.AddSceneService<SawmillPlacingToolService>(sceneId);
        services.AddSceneService<DepotPlacingToolService>(sceneId);
        services.AddSceneService<DeletionToolService>(sceneId);
        services.AddSceneService<SelectToolHandlerService>(sceneId);
        services.AddSceneService<ToolBarService>(sceneId);
        services.AddSceneService<InfoBarService>(sceneId);
        services.AddSceneService<DayTimeSliderService>(sceneId);
        services.AddSceneService<BurgerMenuService>(sceneId);
        services.AddSceneService<SignalRulesPanelService>(sceneId);
        services.AddSceneService<StationInfoPanelService>(sceneId);
        services.AddSceneService<IndustryInfoPanelService>(sceneId);
        services.AddSceneService<DepotPanelService>(sceneId);
        services.AddSceneService<TrainSpeedCycleService>(sceneId);
        services.AddSceneService<ActivateGuiHandlerService>(sceneId);
        services.AddSceneService<QueryGuiHandlerService>(sceneId);

        services.AddSceneService<ScreenshotService>(sceneId);

        services.TryAddScoped<ToolManagementService>();

        return services;
    }
}
