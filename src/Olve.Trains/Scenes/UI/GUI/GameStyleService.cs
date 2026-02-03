using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GameStyleService(
    ILoggingManager loggingManager,
    GuiStyleApplierService guiStyleApplierService,
    GuiStyleRegistry styleRegistry) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependents([guiStyleApplierService]);

    protected override Result OnLoad()
    {
        styleRegistry.Register(Styles.MenuButtonStyle);
        styleRegistry.Register(Styles.MenuBarBackground);
        styleRegistry.Register(Styles.ToolIconStyle);
        styleRegistry.Register(Styles.InfoBarBackground);
        styleRegistry.Register(Styles.InfoBarSection);

        return Result.Success();
    }
}
