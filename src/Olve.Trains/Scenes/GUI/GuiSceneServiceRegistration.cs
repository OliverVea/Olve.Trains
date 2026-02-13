using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GUI;

public static class GuiSceneServiceRegistration
{
    public static IServiceCollection AddGuiSceneServices(this IServiceCollection services, Id<IScene> sceneId)
    {
        // Olve.Trains GUI services
        AddGuiSceneService<GuiLayoutContextUpdater>(services, sceneId);
        AddGuiSceneService<GuiRectangleRenderingService>(services, sceneId);
        AddGuiSceneService<GuiRectangleUpdateService>(services, sceneId);
        AddGuiSceneService<GuiTextRenderingService>(services, sceneId);
        AddGuiSceneService<GuiTextUpdateService>(services, sceneId);

        // Engine GUI scene services
        AddGuiSceneService<GuiLayoutUpdateService>(services, sceneId);
        AddGuiSceneService<GuiDepthService>(services, sceneId);
        AddGuiSceneService<GuiLayoutService>(services, sceneId);
        AddGuiSceneService<GuiStateListenerService>(services, sceneId);
        AddGuiSceneService<GuiStyleApplierService>(services, sceneId);
        AddGuiSceneService<GuiAnimationService>(services, sceneId);
        AddGuiSceneService<GuiMouseInputService>(services, sceneId);

        // Non-scene singletons needed by GUI services
        services.TryAddSingleton<Provider<LayoutContext>>();

        return services;
    }

    private static void AddGuiSceneService<T>(IServiceCollection services, Id<IScene> sceneId)
        where T : class, ISceneService
    {
        services.TryAddScoped<T>();
        services.AddKeyedTransient<ISceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
    }
}
