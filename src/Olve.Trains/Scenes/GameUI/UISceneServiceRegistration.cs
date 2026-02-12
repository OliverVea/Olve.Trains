using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameUI.GUI;
using Olve.Trains.Scenes.GameUI.Indicators;
using Olve.Trains.Scenes.GameUI.Tools;

namespace Olve.Trains.Scenes.GameUI;

public static class UISceneServiceRegistration
{
    public static IServiceCollection AddUISceneServices(this IServiceCollection services)
    {
        var sceneId = SceneIds.GameUIScene;

        // Tool / indicator services
        services.AddSceneService<TrackArrowIndicatorService>(sceneId);
        services.AddSceneService<TrackPlacingToolService>(sceneId);
        services.AddSceneService<TrainPlacingToolService>(sceneId);
        services.AddSceneService<StationPlacingToolService>(sceneId);
        services.AddSceneService<ToolBarService>(sceneId);
        services.AddSceneService<InfoBarService>(sceneId);

        // GUI services
        services.AddSceneService<GuiLayoutContextUpdater>(sceneId);
        services.AddSceneService<GuiLayoutUpdateService>(sceneId);
        services.AddSceneService<GuiRectangleRenderingService>(sceneId);
        services.AddSceneService<GuiRectangleUpdateService>(sceneId);
        services.AddSceneService<GuiTextRenderingService>(sceneId);
        services.AddSceneService<GuiTextUpdateService>(sceneId);
        services.AddSceneService<GameStyleService>(sceneId);

        // GUI engine scene services (from engine's GuiServiceRegistration)
        services.AddSceneService<GuiDepthService>(sceneId);
        services.AddSceneService<GuiLayoutService>(sceneId);
        services.AddSceneService<GuiStateListenerService>(sceneId);
        services.AddSceneService<GuiStyleApplierService>(sceneId);
        services.AddSceneService<GuiAnimationService>(sceneId);
        services.AddSceneService<GuiMouseInputService>(sceneId);

        // Non-scene singletons
        services.AddSingleton<ToolManagementService>();
        services.AddSingleton<Provider<LayoutContext>>();

        return services;
    }
}
