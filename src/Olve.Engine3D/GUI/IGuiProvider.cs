using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI.Collision;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.GUI;

[ServiceProviderModule]
[Singleton(typeof(GuiAnchorService))]
[Singleton(typeof(GuiLayoutService))]
[Singleton(typeof(GuiNodeStateService))]
[Singleton(typeof(GuiActivationService))]
[Singleton(typeof(GuiStateListenerService))]
[Singleton(typeof(GuiStyleApplierService))]
[Singleton(typeof(GuiAnimationService))]
[Singleton(typeof(GuiStyleRegistry))]
[Singleton(typeof(GuiElementService))]
[Singleton(typeof(GuiNodeService))]
[Singleton(typeof(GuiCollisionService))]
[Singleton(typeof(GuiFocusService))]
[Singleton(typeof(GuiMouseInputService))]
public interface IGuiProvider
{
    public static IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider)
    {
        return [
            provider.GetRequiredService<GuiDepthService>(),
            provider.GetRequiredService<GuiLayoutService>(),
            provider.GetRequiredService<GuiStateListenerService>(),
            provider.GetRequiredService<GuiStyleApplierService>(),
            provider.GetRequiredService<GuiAnimationService>(),
            provider.GetRequiredService<GuiMouseInputService>(),
        ];
    }
}