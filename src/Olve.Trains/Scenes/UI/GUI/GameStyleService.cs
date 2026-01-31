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

        return Result.Success();
    }
}
