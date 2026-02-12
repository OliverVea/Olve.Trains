using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.UI.GUI;

public class GameStyleService(
    GuiStyleApplierService guiStyleApplierService,
    GuiStyleRegistry styleRegistry) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependents([guiStyleApplierService]);

    public Result Load()
    {
        styleRegistry.Register(Styles.MenuButtonStyle);
        styleRegistry.Register(Styles.MenuBarBackground);
        styleRegistry.Register(Styles.ToolIconStyle);
        styleRegistry.Register(Styles.InfoBarBackground);
        styleRegistry.Register(Styles.InfoBarSection);
        styleRegistry.Register(Styles.InfoBarClockText);

        return Result.Success();
    }
}
