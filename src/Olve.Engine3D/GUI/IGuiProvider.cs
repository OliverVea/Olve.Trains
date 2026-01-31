using Jab;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI.Collision;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.GUI;

[ServiceProviderModule]
[Singleton(typeof(GuiAnchorService))]
[Singleton(typeof(GuiLayoutService))]
[Singleton(typeof(GuiElementService))]
[Singleton(typeof(GuiNodeService))]
[Singleton(typeof(GuiCollisionService))]
public interface IGuiProvider
{
    public static IEnumerable<SceneService> GetAllSceneServices(IServiceProvider provider)
    {
        return [
            provider.GetRequiredService<GuiDepthService>(),
            provider.GetRequiredService<GuiLayoutService>(),
        ];
    }
}