using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GUI;

public static class GuiSceneServiceRegistration
{
    private static readonly ISceneServiceType[] BeforeAnimation = [new SceneServiceType<GuiAnimationService>()];
    private static readonly ISceneServiceType[] AfterMouseInput = [new SceneServiceType<GuiMouseInputService>()];
    private static readonly ISceneServiceType[] AfterAnimation = [new SceneServiceType<GuiAnimationService>()];

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

        // GuiStateListener events — all run before GuiAnimationService (which reads state)
        services.AddEventSceneService(sceneId,
            (GuiElementService es) => es.OnAdded,
            (GuiNodeStateService ss, GuiElementArgs a) =>
                ss.UpdateState(a.NodeId, s => s | GuiNodeState.Show | GuiNodeState.Enabled),
            before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiElementService es) => es.OnRemoved,
            (GuiNodeStateService ss, GuiElementArgs a) =>
                ss.UpdateState(a.NodeId, s => s & ~GuiNodeState.All),
            before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiNodeService ns) => ns.OnEnabled,
            (GuiNodeStateService ss, Id<GuiNode> id) =>
                ss.UpdateState(id, s => s | GuiNodeState.Enabled),
            before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiNodeService ns) => ns.OnDisabled,
            (GuiNodeStateService ss, Id<GuiNode> id) =>
                ss.UpdateState(id, s => s & ~GuiNodeState.Enabled),
            before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiFocusService fs) => fs.OnFocusChanged,
            (GuiNodeStateService ss, GuiFocusChanged c) =>
            {
                if (c.Previous.HasValue) ss.UpdateState(c.Previous.Value, s => s & ~GuiNodeState.Focused);
                if (c.Current.HasValue) ss.UpdateState(c.Current.Value, s => s | GuiNodeState.Focused);
            }, before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiMouseInputService mis) => mis.OnPressedNode,
            (GuiNodeStateService ss, Id<GuiNode> id) =>
                ss.UpdateState(id, s => s | GuiNodeState.Pressed),
            after: AfterMouseInput, before: BeforeAnimation);
        services.AddEventSceneService(sceneId,
            (GuiMouseInputService mis) => mis.OnReleasedNode,
            (GuiNodeStateService ss, Id<GuiNode> id) =>
                ss.UpdateState(id, s => s & ~GuiNodeState.Pressed),
            after: AfterMouseInput, before: BeforeAnimation);

        // GuiStyleApplier event — must run after GuiAnimationService which produces weight changes
        services.AddEventSceneService(sceneId,
            (GuiAnimationService gas) => gas.GuiStateWeightsChanged,
            (GuiStyleApplierService sas, GuiAnimationService.GuiStateWeightsChangedMessage msg) =>
                sas.ApplyStateWeights(msg), after: AfterAnimation);

        AddGuiSceneService<GuiAnimationService>(services, sceneId);
        AddGuiSceneService<GuiMouseInputService>(services, sceneId);

        // Non-scene singletons needed by GUI services
        services.TryAddSingleton<Provider<LayoutContext>>();
        services.TryAddScoped<GuiStyleApplierService>();

        return services;
    }

    private static void AddGuiSceneService<T>(IServiceCollection services, Id<IScene> sceneId)
        where T : class, ISceneService
    {
        services.TryAddScoped<T>();
        services.AddKeyedTransient<ISceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
    }
}
