using Jab;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI;

[ServiceProviderModule]
[Singleton(typeof(GuiAnchorService))]
[Singleton(typeof(GuiLayoutService))]
[Singleton(typeof(GuiElementService))]
[Singleton(typeof(GuiNodeService))]
public interface IGuiProvider;